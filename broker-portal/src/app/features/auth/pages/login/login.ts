import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { AccountSessionService } from '../../../../core/auth/account-session.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, FormsModule, MatButtonModule],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginPage {
  private readonly auth = inject(AccountSessionService);
  private readonly router = inject(Router);

  email = '';
  password = '';
  isLoading = false;
  submitted = false;
  errorMessage = '';
  signIn(form: NgForm): void {
    this.submitted = true;
    this.errorMessage = '';

    if (form.invalid || this.isLoading) {
      return;
    }

    this.isLoading = true;
    this.auth.login(this.email, this.password).subscribe({
      next: () => {
        this.isLoading = false;
        void this.router.navigateByUrl('/users');
      },
      error: error => {
        this.isLoading = false;
        this.errorMessage = error?.status === 401
          ? 'The email or password is incorrect.'
          : 'Unable to sign in. Please try again.';
      }
    });
  }
}
