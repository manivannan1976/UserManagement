import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { Router, RouterLink } from '@angular/router';
import { AccountSessionService } from '../../../../core/auth/account-session.service';

@Component({
  selector: 'app-unauthorized-page',
  standalone: true,
  imports: [CommonModule, MatButtonModule, RouterLink],
  templateUrl: './unauthorized.html',
  styleUrl: './unauthorized.scss'
})
export class UnauthorizedPage {
  private readonly auth = inject(AccountSessionService);
  private readonly router = inject(Router);

  logout(): void {
    this.auth.logout().subscribe({
      next: () => void this.router.navigateByUrl('/login')
    });
  }
}
