import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AccountSessionService } from './core/auth/account-session.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, MatButtonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly auth = inject(AccountSessionService);
  private readonly router = inject(Router);
  readonly isAuthenticated$ = this.auth.isAuthenticated$;
  readonly email$ = this.auth.email$;
  readonly canAdminister$ = this.auth.canAdminister$;
  logoutError = '';

  logout(): void {
    this.logoutError = '';
    this.auth.logout().subscribe({
      next: () => void this.router.navigateByUrl('/login'),
      error: () => this.logoutError = 'Sign out failed. Please try again.'
    });
  }
}