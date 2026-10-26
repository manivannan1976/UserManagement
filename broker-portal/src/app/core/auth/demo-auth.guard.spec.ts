import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, GuardResult, MaybeAsync, provideRouter, Router, RouterStateSnapshot } from '@angular/router';
import { firstValueFrom, isObservable, of } from 'rxjs';
import { AccountSessionService, AuthenticatedUser } from './account-session.service';
import { sessionAuthGuard, sessionLoginGuard, sessionRootGuard } from './demo-auth.guard';

describe('session authentication guards', () => {
  let sessionUser: AuthenticatedUser | null;
  let router: Router;

  beforeEach(() => {
    sessionUser = null;
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AccountSessionService,
          useValue: { checkSession: () => of(sessionUser) }
        }
      ]
    });
    router = TestBed.inject(Router);
  });

  const route = {} as ActivatedRouteSnapshot;
  const state = { url: '/users' } as RouterStateSnapshot;
  const resolveGuard = async (result: MaybeAsync<GuardResult>): Promise<GuardResult> =>
    isObservable(result) ? firstValueFrom(result) : await result;

  it('routes the logged-out root to login', async () => {
    const result = await resolveGuard(TestBed.runInInjectionContext(() => sessionRootGuard(route, state)));

    expect(router.serializeUrl(result as ReturnType<Router['createUrlTree']>)).toBe('/login');
  });

  it('routes the authenticated administrator root to users', async () => {
    sessionUser = { id: 1, email: 'admin@example.com', firstName: 'Admin', lastName: 'User', role: 'PORTAL_SUPER_ADMIN' };
    const result = await resolveGuard(TestBed.runInInjectionContext(() => sessionRootGuard(route, state)));

    expect(router.serializeUrl(result as ReturnType<Router['createUrlTree']>)).toBe('/users');
  });

  it('redirects anonymous users to login and permits an administrator', async () => {
    const result = await resolveGuard(TestBed.runInInjectionContext(() => sessionAuthGuard(route, state)));

    expect(router.serializeUrl(result as ReturnType<Router['createUrlTree']>)).toBe(
      '/login?returnUrl=%2Fusers'
    );

    sessionUser = { id: 1, email: 'admin@example.com', firstName: 'Admin', lastName: 'User', role: 'PORTAL_SUPER_ADMIN' };
    expect(await resolveGuard(TestBed.runInInjectionContext(() => sessionAuthGuard(route, state)))).toBeTrue();
  });

  it('keeps anonymous users on login and redirects an authenticated admin to users', async () => {
    expect(await resolveGuard(TestBed.runInInjectionContext(() => sessionLoginGuard(route, state)))).toBeTrue();

    sessionUser = { id: 1, email: 'admin@example.com', firstName: 'Admin', lastName: 'User', role: 'PORTAL_SUPER_ADMIN' };
    const result = await resolveGuard(TestBed.runInInjectionContext(() => sessionLoginGuard(route, state)));

    expect(router.serializeUrl(result as ReturnType<Router['createUrlTree']>)).toBe('/users');
  });
});
