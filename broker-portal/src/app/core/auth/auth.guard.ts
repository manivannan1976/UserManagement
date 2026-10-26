import { AuthService } from '@auth0/auth0-angular';
import { filter, firstValueFrom, switchMap, take } from 'rxjs';
import { environment } from '../../../environments/environment';

export function getAuth0ConfigurationError(): string | null {
  const { domain, clientId } = environment.auth0;
  const validDomain =
    Boolean(domain) &&
    !/^YOUR_/i.test(domain) &&
    !/^https?:\/\//i.test(domain) &&
    !domain.includes('/') &&
    domain.includes('.');
  const validClientId = Boolean(clientId) && !/^YOUR_/i.test(clientId);

  if (!validDomain || !validClientId) {
    return 'Set your Auth0 tenant domain and SPA client ID in src/environments/environment.development.ts before signing in.';
  }

  return null;
}

export async function waitForAuthentication(auth: AuthService): Promise<boolean> {
  return firstValueFrom(
    auth.isLoading$.pipe(
      filter(isLoading => !isLoading),
      take(1),
      switchMap(() => auth.isAuthenticated$.pipe(take(1)))
    )
  );
}

export function safeReturnUrl(value: string | null | undefined): string {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : '/users';
}
