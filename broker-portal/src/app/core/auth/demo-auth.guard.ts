import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AccountSessionService, AuthenticatedUser, canAdministerUsers } from './account-session.service';

/** Where a signed-in user lands: admins manage users, everyone else goes to the portal home. */
function landingUrl(user: AuthenticatedUser): string {
  return canAdministerUsers(user) ? '/users' : '/home';
}

/** Any signed-in user. */
export const sessionAuthGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AccountSessionService);
  const router = inject(Router);

  return auth.checkSession().pipe(map(user => user
    ? true
    : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })));
};

/** Portal super admins and broker admins only. Data scoping is enforced by the API as well. */
export const userAdminGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AccountSessionService);
  const router = inject(Router);

  return auth.checkSession().pipe(map(user => {
    if (!user) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }
    return canAdministerUsers(user) ? true : router.createUrlTree(['/unauthorized']);
  }));
};

export const sessionLoginGuard: CanActivateFn = () => {
  const auth = inject(AccountSessionService);
  const router = inject(Router);

  return auth.checkSession().pipe(map(user => user
    ? router.createUrlTree([landingUrl(user)])
    : true));
};

export const sessionRootGuard: CanActivateFn = () => {
  const auth = inject(AccountSessionService);
  const router = inject(Router);

  return auth.checkSession().pipe(map(user => router.createUrlTree([
    user ? landingUrl(user) : '/login'
  ])));
};
