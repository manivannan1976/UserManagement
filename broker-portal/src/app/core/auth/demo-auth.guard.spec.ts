import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, GuardResult, MaybeAsync, provideRouter, Router, RouterStateSnapshot } from '@angular/router';
import { firstValueFrom, isObservable, of } from 'rxjs';
import { AccountSessionService, AuthenticatedUser } from './account-session.service';
import { sessionAuthGuard, sessionLoginGuard, sessionRootGuard, userAdminGuard } from './demo-auth.guard';

const buildUser = (role: string): AuthenticatedUser => ({
  id: 1,
  email: 'person@example.com',
  firstName: 'Test',
  lastName: 'Person',
  role,
  agencyId: role.startsWith('BROKER') ? 2 : null,
  agencyName: role.startsWith('BROKER') ? 'Acme Brokerage' : null,
  workspace: { key: 'PORTAL_ADMIN', label: 'Portal administration', url: null },
  jobTitle: null
});

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
  const urlOf = (result: GuardResult) =>
    router.serializeUrl(result as ReturnType<Router['createUrlTree']>);

  it('routes the logged-out root to login', async () => {
    const result = await resolveGuard(TestBed.runInInjectionContext(() => sessionRootGuard(route, state)));

    expect(urlOf(result)).toBe('/login');
  });

  it('lands administrators on users and everyone else on the portal home', async () => {
    for (const role of ['PORTAL_SUPER_ADMIN', 'BROKER_ADMIN']) {
      sessionUser = buildUser(role);
      const result = await resolveGuard(TestBed.runInInjectionContext(() => sessionRootGuard(route, state)));
      expect(urlOf(result)).withContext(role).toBe('/users');
    }

    for (const role of ['BROKER_USER', 'UNDERWRITER', 'UNDERWRITER_MANAGER']) {
      sessionUser = buildUser(role);
      const result = await resolveGuard(TestBed.runInInjectionContext(() => sessionRootGuard(route, state)));
      expect(urlOf(result)).withContext(role).toBe('/home');
    }
  });

  it('redirects anonymous users to login and permits any signed-in user', async () => {
    const result = await resolveGuard(TestBed.runInInjectionContext(() => sessionAuthGuard(route, state)));

    expect(urlOf(result)).toBe('/login?returnUrl=%2Fusers');

    sessionUser = buildUser('BROKER_USER');
    expect(await resolveGuard(TestBed.runInInjectionContext(() => sessionAuthGuard(route, state)))).toBeTrue();
  });

  it('only lets super admins and broker admins into user administration', async () => {
    const run = () => resolveGuard(TestBed.runInInjectionContext(() => userAdminGuard(route, state)));

    expect(urlOf(await run())).toBe('/login?returnUrl=%2Fusers');

    for (const role of ['PORTAL_SUPER_ADMIN', 'BROKER_ADMIN']) {
      sessionUser = buildUser(role);
      expect(await run()).withContext(role).toBeTrue();
    }

    for (const role of ['BROKER_USER', 'UNDERWRITER', 'UNDERWRITER_MANAGER']) {
      sessionUser = buildUser(role);
      expect(urlOf(await run())).withContext(role).toBe('/unauthorized');
    }
  });

  it('keeps anonymous users on login and redirects signed-in users to their landing page', async () => {
    expect(await resolveGuard(TestBed.runInInjectionContext(() => sessionLoginGuard(route, state)))).toBeTrue();

    sessionUser = buildUser('BROKER_ADMIN');
    const admin = await resolveGuard(TestBed.runInInjectionContext(() => sessionLoginGuard(route, state)));
    expect(urlOf(admin)).toBe('/users');

    sessionUser = buildUser('BROKER_USER');
    const broker = await resolveGuard(TestBed.runInInjectionContext(() => sessionLoginGuard(route, state)));
    expect(urlOf(broker)).toBe('/home');
  });
});
