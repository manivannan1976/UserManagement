import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, FormsModule, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { timer } from 'rxjs';
import { InvitationService } from '../../invitation.service';

function passwordConfirmation(control: AbstractControl): ValidationErrors | null {
  const confirmation = control.get('confirmPassword')?.value;
  return !confirmation || control.get('password')?.value === confirmation
    ? null
    : { passwordMismatch: true };
}

@Component({
  selector: 'app-activate-invitation-page',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, MatButtonModule, MatInputModule],
  templateUrl: './activate-invitation.html',
  styleUrl: './activate-invitation.scss'
})
export class ActivateInvitationPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(InvitationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(FormBuilder);

  readonly form = this.formBuilder.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(100)]],
    lastName: ['', [Validators.required, Validators.maxLength(100)]],
    password: ['', [
      Validators.required,
      Validators.minLength(12),
      Validators.maxLength(256),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$/)
    ]],
    confirmPassword: ['', [Validators.required, Validators.maxLength(256)]]
  }, { validators: passwordConfirmation });

  email: string | null = null;
  token: string | null = null;
  state = 'loading';
  loading = true;
  submitting = false;
  success = false;
  showPassword = false;
  errorMessage = '';

  goToLogin(): void {
    void this.router.navigateByUrl('/login');
  }

  ngOnInit(): void {
    this.route.queryParamMap.pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(params => {
      const token = params.get('token');
      if (!token) {
        if (!this.token && this.state === 'loading') {
          this.loading = false;
          this.state = 'invalid';
        }
        return;
      }
      if (this.token) {
        return;
      }

      this.token = token;
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { token: null },
        queryParamsHandling: 'merge',
        replaceUrl: true
      });
      this.service.validate(token).pipe(
        takeUntilDestroyed(this.destroyRef)
      ).subscribe({
        next: result => {
          this.loading = false;
          this.state = result.isValid ? 'valid' : result.state ?? 'invalid';
          this.email = result.email;
          this.form.patchValue({
            firstName: result.firstName ?? '',
            lastName: result.lastName ?? ''
          });
          if (!result.isValid) {
            this.token = null;
          }
        },
        error: () => {
          this.loading = false;
          this.state = 'unavailable';
          this.token = null;
        }
      });
    });
  }

  activate(): void {
    if (this.submitting || this.form.invalid || !this.token) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.errorMessage = '';
    const token = this.token;
    const { firstName, lastName, password, confirmPassword } = this.form.getRawValue();
    this.service.activate(token, firstName.trim(), lastName.trim(), password, confirmPassword).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: () => {
        this.token = null;
        this.form.reset();
        this.success = true;
        this.submitting = false;
        timer(2500).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
          void this.router.navigateByUrl('/login');
        });
      },
      error: error => {
        this.submitting = false;
        if (error instanceof HttpErrorResponse && error.status === 409) {
          this.state = error.error?.code ?? 'unavailable';
          this.token = null;
        } else if (error instanceof HttpErrorResponse && error.status === 400) {
          this.errorMessage = error.error?.errors
            ? Object.values(error.error.errors).flat().join(' ')
            : error.error?.message ?? 'Check the password requirements and try again.';
        } else {
          this.errorMessage = 'Account activation could not be completed. Please try again.';
        }
      }
    });
  }

  get stateMessage(): string {
    const messages: Record<string, string> = {
      invalid: 'This invitation link is invalid.',
      expired: 'This invitation has expired. Ask your portal administrator to send a new invitation.',
      used: 'This invitation has already been used.',
      revoked: 'This invitation has been revoked.',
      unavailable: 'This account is not available for activation.',
      loading: 'Checking your invitation...'
    };
    return messages[this.state] ?? 'This invitation could not be verified.';
  }
}
