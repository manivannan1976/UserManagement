import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { AccountSessionService } from '../../../../core/auth/account-session.service';
import { Agency, BulkInviteResult, InviteRole, ROLE_LABELS } from '../../models/user';
import { UserManagementService } from '../../services/user-managemen';

export interface InviteDialogResult {
  message: string;
  activationUrl: string | null;
  emailPreviewHtml: string | null;
}

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

@Component({
  selector: 'app-invite-user-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule
  ],
  templateUrl: './invite-user-dialog.html',
  styleUrl: './invite-user-dialog.scss'
})
export class InviteUserDialog implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly userService = inject(UserManagementService);
  private readonly session = inject(AccountSessionService);
  private readonly dialogRef = inject(MatDialogRef<InviteUserDialog, InviteDialogResult>);

  private invitedBeforeFailure = 0;

  isSuperAdmin = false;
  agencies: Agency[] = [];
  roles: { value: InviteRole; label: string }[] = [];

  readonly form = this.formBuilder.nonNullable.group({
    emails: ['', [Validators.required, Validators.maxLength(60000)]],
    role: this.formBuilder.nonNullable.control<InviteRole>('BROKER_USER', Validators.required),
    agencyId: this.formBuilder.control<number | null>(null)
  });

  submitting = false;
  loadingAgencies = true;
  errorMessage = '';
  failures: BulkInviteResult[] = [];

  ngOnInit(): void {
    this.session.checkSession().subscribe(user => {
      this.isSuperAdmin = user?.role === 'PORTAL_SUPER_ADMIN';
      // Broker admins can only add Broker users; internal roles are BHSI-only.
      const allowed: InviteRole[] = this.isSuperAdmin
        ? ['BROKER_ADMIN', 'BROKER_USER', 'UNDERWRITER', 'UNDERWRITER_MANAGER']
        : ['BROKER_USER'];
      this.roles = allowed.map(value => ({ value, label: ROLE_LABELS[value] }));
      if (!this.isSuperAdmin) {
        this.form.controls.role.disable();
      }

      this.userService.getAgencies().subscribe({
        next: agencies => {
          this.agencies = agencies;
          // Broker admins inherit their own agency, which is the only one returned.
          if (!this.isSuperAdmin && agencies.length === 1) {
            this.form.controls.agencyId.setValue(agencies[0].id);
          }
          this.loadingAgencies = false;
        },
        error: () => {
          this.loadingAgencies = false;
          this.errorMessage = 'Agencies could not be loaded. Please try again.';
        }
      });
    });
  }

  get needsAgency(): boolean {
    const role = this.form.controls.role.getRawValue();
    return role === 'BROKER_ADMIN' || role === 'BROKER_USER';
  }

  get selectedAgency(): Agency | undefined {
    const id = this.form.controls.agencyId.value;
    return this.agencies.find(agency => agency.id === id);
  }

  get parsedEmails(): string[] {
    const seen = new Set<string>();
    return this.form.controls.emails.value
      .split(/[\s,;]+/)
      .map(email => email.trim().toLowerCase())
      .filter(email => email.length > 0 && !seen.has(email) && !!seen.add(email));
  }

  get invalidEmails(): string[] {
    return this.parsedEmails.filter(email => !EMAIL_PATTERN.test(email));
  }

  submit(): void {
    if (this.submitting) {
      return;
    }

    const emails = this.parsedEmails;
    if (emails.length === 0 || this.invalidEmails.length > 0 ||
        (this.needsAgency && this.form.controls.agencyId.value === null)) {
      this.form.markAllAsTouched();
      this.errorMessage = emails.length === 0
        ? 'Enter at least one email address.'
        : this.invalidEmails.length > 0
          ? `Not a valid email: ${this.invalidEmails.slice(0, 3).join(', ')}`
          : 'Select an agency.';
      return;
    }

    this.submitting = true;
    this.errorMessage = '';
    this.failures = [];
    const role = this.form.controls.role.getRawValue();
    const agencyId = this.needsAgency ? this.form.controls.agencyId.value : null;
    const requests = emails.map(email => ({ email, role, agencyId }));

    if (requests.length === 1) {
      this.userService.inviteUser(requests[0]).pipe(
        finalize(() => this.submitting = false)
      ).subscribe({
        next: response => this.dialogRef.close({
          message: response.user.status === 'Setup Incomplete'
            ? `${response.user.email} was created. ${response.message ?? 'Duck Creek setup failed.'}`
            : response.deliveryStatus === 'Sent'
              ? `${response.user.email} was invited.`
              : `${response.user.email} was saved as Invited. ${response.message ?? 'Email delivery did not complete.'}`,
          activationUrl: response.developmentActivationUrl,
          emailPreviewHtml: response.developmentEmailPreviewHtml
        } satisfies InviteDialogResult),
        error: error => this.errorMessage = this.getErrorMessage(error)
      });
      return;
    }

    this.userService.inviteUsers(requests).pipe(
      finalize(() => this.submitting = false)
    ).subscribe({
      next: results => {
        this.failures = results.filter(result => !result.succeeded);
        const sent = results.length - this.failures.length;
        if (this.failures.length === 0) {
          this.dialogRef.close(this.summarize(sent, results));
          return;
        }

        // Keep the dialog open so the admin can fix just the rejected rows and retry them.
        this.form.controls.emails.setValue(this.failures.map(failure => failure.email).join('\n'));
        this.errorMessage = `${sent} invited, ${this.failures.length} could not be invited. ` +
          'The rejected addresses are listed below.';
        if (sent > 0) {
          this.invitedBeforeFailure += sent;
        }
      },
      error: error => this.errorMessage = this.getErrorMessage(error)
    });
  }

  done(): void {
    this.dialogRef.close(this.invitedBeforeFailure > 0
      ? {
        message: `${this.invitedBeforeFailure} users were invited.`,
        activationUrl: null,
        emailPreviewHtml: null
      } satisfies InviteDialogResult
      : undefined);
  }

  private summarize(sent: number, results: BulkInviteResult[]): InviteDialogResult {
    const first = results.find(result => result.invitation)?.invitation;
    return {
      message: `${sent + this.invitedBeforeFailure} users were invited.`,
      activationUrl: first?.developmentActivationUrl ?? null,
      emailPreviewHtml: null
    };
  }

  private getErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'The invitation could not be saved. Please try again.';
    }

    const body = error.error as
      { message?: unknown; detail?: unknown; title?: unknown; errors?: unknown } | null;
    if (typeof body?.message === 'string') {
      return body.message;
    }
    if (typeof body?.detail === 'string') {
      return body.detail;
    }
    if (typeof body?.title === 'string') {
      return body.title;
    }
    if (body?.errors && typeof body.errors === 'object') {
      const messages = Object.values(body.errors as Record<string, unknown>)
        .flatMap(value => Array.isArray(value) ? value : [])
        .filter((value): value is string => typeof value === 'string');
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    return error.status === 0
      ? 'Unable to reach the Users API. Check that the API is running.'
      : 'The invitation could not be saved. Please try again.';
  }
}
