import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { finalize } from 'rxjs';
import { AccountSessionService, AuthenticatedUser } from '../../../../core/auth/account-session.service';
import { ROLE_LABELS } from '../../../user-management/models/user';

/** Self-service profile: name and job title are editable; email and role are read-only. */
@Component({
  selector: 'app-profile-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <main class="profile-page">
      <section class="profile-card" aria-labelledby="profile-title">
        <p class="eyebrow">S|2 PORTAL</p>
        <h1 id="profile-title">My profile</h1>

        @if (user) {
          <dl class="readonly">
            <div><dt>Email</dt><dd>{{ user.email }}</dd></div>
            <div><dt>Role</dt><dd>{{ roleLabel }}</dd></div>
            @if (user.agencyName) {
              <div><dt>Agency</dt><dd>{{ user.agencyName }}</dd></div>
            }
          </dl>
          <p class="note">Your email and role can only be changed by an administrator.</p>

          <form [formGroup]="form" (ngSubmit)="save()">
            <mat-form-field appearance="outline">
              <mat-label>First name</mat-label>
              <input matInput formControlName="firstName" maxlength="100" autocomplete="given-name" />
              @if (form.controls.firstName.touched && form.controls.firstName.invalid) {
                <mat-error>First name is required.</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Last name</mat-label>
              <input matInput formControlName="lastName" maxlength="100" autocomplete="family-name" />
              @if (form.controls.lastName.touched && form.controls.lastName.invalid) {
                <mat-error>Last name is required.</mat-error>
              }
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>Job title (optional)</mat-label>
              <input matInput formControlName="jobTitle" maxlength="150" />
            </mat-form-field>

            @if (message) {
              <div class="feedback" [class.error]="isError" role="status">{{ message }}</div>
            }
            <button mat-flat-button color="primary" type="submit" [disabled]="saving || form.pristine">
              {{ saving ? 'Saving...' : 'Save changes' }}
            </button>
          </form>
        }
      </section>
    </main>
  `,
  styles: `
    .profile-page { display: grid; min-height: calc(100vh - 60px); place-items: start center; padding: 40px 24px; background: #f7f8fc; }
    .profile-card { width: min(100%, 560px); padding: 36px; border: 1px solid #e8ebf2; border-radius: 12px; background: #fff; }
    .eyebrow { color: #64748b; font-size: 11px; font-weight: 700; letter-spacing: 1.2px; }
    h1 { color: #172033; margin-top: 4px; }
    form { display: grid; gap: 4px; }
    .readonly { display: grid; gap: 8px; margin: 16px 0 4px; }
    .readonly div { display: flex; gap: 12px; }
    dt { width: 72px; color: #68758a; font-size: 13px; }
    dd { margin: 0; color: #172033; font-weight: 600; }
    .note { color: #68758a; font-size: 13px; margin-bottom: 20px; }
    .feedback { padding: 10px 12px; border-radius: 8px; background: #ecfdf3; color: #067647; margin-bottom: 12px; }
    .feedback.error { background: #fef3f2; color: #b42318; }
  `
})
export class ProfilePage implements OnInit {
  private readonly session = inject(AccountSessionService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  user: AuthenticatedUser | null = null;
  saving = false;
  message = '';
  isError = false;

  readonly form = this.formBuilder.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    jobTitle: ['', [Validators.maxLength(150)]]
  });

  get roleLabel(): string {
    return this.user ? ROLE_LABELS[this.user.role] ?? this.user.role : '';
  }

  ngOnInit(): void {
    this.session.checkSession().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(user => {
      this.user = user;
      if (user) {
        this.resetForm(user);
      }
    });
  }

  save(): void {
    if (this.saving) {
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.message = '';
    const { firstName, lastName, jobTitle } = this.form.getRawValue();
    this.session.updateProfile({
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      jobTitle: jobTitle.trim() || null
    }).pipe(
      finalize(() => this.saving = false),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: user => {
        this.user = user;
        this.resetForm(user);
        this.isError = false;
        this.message = 'Your profile was updated.';
      },
      error: error => {
        this.isError = true;
        this.message = error instanceof HttpErrorResponse && typeof error.error?.message === 'string'
          ? error.error.message
          : 'Your profile could not be saved. Please try again.';
      }
    });
  }

  private resetForm(user: AuthenticatedUser): void {
    this.form.reset({
      firstName: user.firstName,
      lastName: user.lastName,
      jobTitle: user.jobTitle ?? ''
    });
  }
}
