import { isPlatformBrowser } from '@angular/common';
import { inject, Injectable, InjectionToken, PLATFORM_ID } from '@angular/core';
import { BehaviorSubject, map, Observable, tap, timer } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DemoAuthConfig {
  enabled: boolean;
  email: string;
  password: string;
  sessionStorageKey: string;
}

export const DEMO_AUTH_CONFIG = new InjectionToken<DemoAuthConfig>('DEMO_AUTH_CONFIG', {
  providedIn: 'root',
  factory: () => environment.demoAuth
});

@Injectable({ providedIn: 'root' })
export class DemoAuthService {
  private readonly config = inject(DEMO_AUTH_CONFIG);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly authenticatedSubject = new BehaviorSubject<boolean>(
    this.readExistingSession()
  );

  readonly isAuthenticated$ = this.authenticatedSubject.asObservable();
  readonly email$ = this.isAuthenticated$.pipe(
    map(isAuthenticated => (isAuthenticated ? this.config.email : null))
  );

  isAuthenticated(): boolean {
    return this.authenticatedSubject.value;
  }

  login(email: string, password: string): Observable<boolean> {
    return timer(350).pipe(
      map(() => {
        const credentialsMatch =
          this.config.enabled &&
          email.trim().toLowerCase() === this.config.email.toLowerCase() &&
          password === this.config.password;

        return credentialsMatch;
      }),
      tap(isValid => {
        if (isValid) {
          this.authenticatedSubject.next(true);
          this.writeSession();
        }
      })
    );
  }

  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      window.sessionStorage.removeItem(this.config.sessionStorageKey);
    }

    this.authenticatedSubject.next(false);
  }

  private readExistingSession(): boolean {
    return (
      this.config.enabled &&
      isPlatformBrowser(this.platformId) &&
      window.sessionStorage.getItem(this.config.sessionStorageKey) === 'authenticated'
    );
  }

  private writeSession(): void {
    if (isPlatformBrowser(this.platformId)) {
      window.sessionStorage.setItem(this.config.sessionStorageKey, 'authenticated');
    }
  }
}
