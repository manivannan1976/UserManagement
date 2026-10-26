import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '@auth0/auth0-angular';
import { firstValueFrom, take } from 'rxjs';
import { environment } from '../../../environments/environment';
import { waitForAuthentication } from './auth.guard';

const requiredRole = 'PORTAL_SUPER_ADMIN';

export const adminRoleGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!(await waitForAuthentication(auth))) {
    return false;
  }

  const [user, claims] = await Promise.all([
    firstValueFrom(auth.user$.pipe(take(1))),
    firstValueFrom(auth.idTokenClaims$.pipe(take(1)))
  ]);
  const claimFound = Object.prototype.hasOwnProperty.call(
    claims ?? {},
    environment.auth0.rolesClaim
  );
  const roleClaim = claimFound ? claims?.[environment.auth0.rolesClaim] : undefined;
  const claimFormat = Array.isArray(roleClaim)
    ? 'array'
    : typeof roleClaim === 'string'
      ? 'string'
      : roleClaim === undefined
        ? 'missing'
        : typeof roleClaim;
  const roles =
    typeof roleClaim === 'string'
      ? [roleClaim]
      : Array.isArray(roleClaim)
        ? roleClaim.filter((role): role is string => typeof role === 'string')
        : [];

  if (!environment.production) {
    console.info('[Auth0 role diagnostics]', {
      authenticatedUserAvailable: Boolean(user),
      claimSource: 'ID token (AuthService.idTokenClaims$)',
      claimName: environment.auth0.rolesClaim,
      claimFound,
      claimFormat,
      roles
    });
  }

  if (roles.includes(requiredRole)) {
    return true;
  }

  return router.createUrlTree(['/unauthorized']);
};
