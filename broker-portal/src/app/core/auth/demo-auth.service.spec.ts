import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { DEMO_AUTH_CONFIG, DemoAuthService } from './demo-auth.service';

const demoConfig = {
  enabled: true,
  email: 'admin@brokerportal.local',
  password: 'Admin@123',
  sessionStorageKey: 'brokerPortal.demoSession.test'
};

describe('DemoAuthService', () => {
  let auth: DemoAuthService;

  beforeEach(() => {
    sessionStorage.removeItem(demoConfig.sessionStorageKey);
    TestBed.configureTestingModule({
      providers: [{ provide: DEMO_AUTH_CONFIG, useValue: demoConfig }]
    });
    auth = TestBed.inject(DemoAuthService);
  });

  afterEach(() => {
    sessionStorage.removeItem(demoConfig.sessionStorageKey);
  });

  it('rejects incorrect credentials', fakeAsync(() => {
    let result: boolean | undefined;
    auth.login(demoConfig.email, 'incorrect').subscribe(value => (result = value));
    tick(350);

    expect(result).toBeFalse();
    expect(auth.isAuthenticated()).toBeFalse();
  }));

  it('authenticates the demo user and stores only a session flag', fakeAsync(() => {
    let result: boolean | undefined;
    auth.login(demoConfig.email, demoConfig.password).subscribe(value => (result = value));
    tick(350);

    expect(result).toBeTrue();
    expect(auth.isAuthenticated()).toBeTrue();
    expect(sessionStorage.getItem(demoConfig.sessionStorageKey)).toBe('authenticated');
    expect(sessionStorage.getItem(demoConfig.sessionStorageKey)).not.toContain(demoConfig.password);

    auth.logout();
    expect(auth.isAuthenticated()).toBeFalse();
    expect(sessionStorage.getItem(demoConfig.sessionStorageKey)).toBeNull();
  }));
});
