import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AccountSessionService } from './account-session.service';

export const sessionAuthGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AccountSessionService);
  const router = inject(Router);

  return auth.checkSession().pipe(map(user => {
    if (!user) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }
    return user.role === 'PORTAL_SUPER_ADMIN'
      ? true
      : router.createUrlTree(['/unauthorized']);
  }));
};

export const sessionLoginGuard: CanActivateFn = () => {
  const auth = inject(AccountSessionService);
  const router = inject(Router);

  return auth.checkSession().pipe(map(user => user
    ? router.createUrlTree([user.role === 'PORTAL_SUPER_ADMIN' ? '/users' : '/unauthorized'])
    : true));
};

export const sessionRootGuard: CanActivateFn = () => {
  const auth = inject(AccountSessionService);
  const router = inject(Router);

  return auth.checkSession().pipe(map(user => router.createUrlTree([
    !user ? '/login' : user.role === 'PORTAL_SUPER_ADMIN' ? '/users' : '/unauthorized'
  ])));
};
