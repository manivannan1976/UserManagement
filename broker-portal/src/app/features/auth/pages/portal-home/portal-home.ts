import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { AccountSessionService, canAdministerUsers } from '../../../../core/auth/account-session.service';

/**
 * Landing page. The user never picks a space: it is derived from their role and agency
 * (requirements 4.B). Q6 is still open, so this page offers the matching Duck Creek workspace
 * rather than redirecting straight into it.
 */
@Component({
  selector: 'app-portal-home-page',
  standalone: true,
  imports: [AsyncPipe, RouterLink],
  template: `
    @if (vm$ | async; as vm) {
      <main class="home-page">
        <section>
          <p class="eyebrow">S|2 PORTAL</p>
          <h1>Welcome, {{ vm.name }}</h1>
          @if (vm.agency) {
            <p class="agency">{{ vm.agency }}</p>
          }

          <h2>Your workspace</h2>
          @if (vm.workspace.url) {
            <a class="primary-link" [href]="vm.workspace.url">Open {{ vm.workspace.label }}</a>
          } @else if (vm.workspace.key !== 'PORTAL_ADMIN') {
            <p>{{ vm.workspace.label }} &mdash; not yet configured for this environment.</p>
          }

          @if (vm.canAdminister) {
            <a class="secondary-link" routerLink="/users">User management</a>
          }
          <a class="secondary-link" routerLink="/profile">My profile</a>
        </section>
      </main>
    }
  `,
  styles: `
    .home-page { display: grid; min-height: calc(100vh - 60px); place-items: center; padding: 24px; background: #f7f8fc; }
    section { display: grid; gap: 8px; width: min(100%, 560px); padding: 36px; border: 1px solid #e8ebf2; border-radius: 12px; background: #fff; }
    .eyebrow { color: #64748b; font-size: 11px; font-weight: 700; letter-spacing: 1.2px; }
    h1 { color: #172033; margin: 0; }
    h2 { color: #172033; font-size: 16px; margin: 16px 0 0; }
    p { color: #68758a; margin: 0; }
    .primary-link { padding: 10px 16px; border-radius: 8px; background: #3f51b5; color: #fff; text-decoration: none; justify-self: start; }
    .secondary-link { color: #3f51b5; }
  `
})
export class PortalHomePage {
  readonly vm$ = inject(AccountSessionService).checkSession().pipe(
    map(user => user && {
      name: `${user.firstName} ${user.lastName}`.trim() || user.email,
      agency: user.agencyName,
      workspace: user.workspace,
      canAdminister: canAdministerUsers(user)
    })
  );
}
