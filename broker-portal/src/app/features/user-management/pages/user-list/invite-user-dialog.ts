import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { BrokerRole, InvitationCreatedResponse } from '../../models/user';
import { UserManagementService } from '../../services/user-managemen';

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
export class InviteUserDialog {
  private readonly formBuilder = inject(FormBuilder);
  private readonly userService = inject(UserManagementService);
  private readonly dialogRef = inject(MatDialogRef<InviteUserDialog, InvitationCreatedResponse>);

  readonly roles: { value: BrokerRole; label: string }[] = [
    { value: 'BROKER_ADMIN', label: 'Broker Admin' },
    { value: 'BROKER_USER', label: 'Broker User' }
  ];

  readonly form = this.formBuilder.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
    jobTitle: ['', [Validators.maxLength(150)]],
    role: this.formBuilder.nonNullable.control<BrokerRole>('BROKER_USER', Validators.required)
  });

  submitting = false;
  errorMessage = '';

  submit(): void {
    if (this.submitting) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = '';
    const value = this.form.getRawValue();
    this.userService.inviteUser({
      ...value,
      firstName: value.firstName.trim(),
      lastName: value.lastName.trim(),
      email: value.email.trim(),
      jobTitle: value.jobTitle.trim() || null
    }).pipe(
      finalize(() => this.submitting = false)
    ).subscribe({
      next: response => this.dialogRef.close(response),
      error: error => this.errorMessage = this.getErrorMessage(error)
    });
  }

  private getErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'The invitation could not be saved. Please try again.';
    }

    const body = error.error as { detail?: unknown; title?: unknown; errors?: unknown } | null;
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
