import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, finalize, map, of, shareReplay, switchMap, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CsrfTokenStore } from './csrf-token.store';

export interface AuthenticatedUser {
  id: number;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AccountSessionService {
  private readonly http = inject(HttpClient);
  private readonly csrfStore = inject(CsrfTokenStore);
  private readonly userSubject = new BehaviorSubject<AuthenticatedUser | null>(null);
  private sessionRequest: Observable<AuthenticatedUser | null> | null = null;

  readonly user$ = this.userSubject.asObservable();
  readonly isAuthenticated$ = this.user$.pipe(map(user => user !== null));
  readonly email$ = this.user$.pipe(map(user => user?.email ?? null));

  checkSession(): Observable<AuthenticatedUser | null> {
    if (this.sessionRequest) {
      return this.sessionRequest;
    }

    this.sessionRequest = this.http.get<AuthenticatedUser>(
      `${environment.apiBaseUrl}/auth/session`
    ).pipe(
      tap(user => this.userSubject.next(user)),
      catchError(() => {
        this.userSubject.next(null);
        return of(null);
      }),
      finalize(() => this.sessionRequest = null),
      shareReplay({ bufferSize: 1, refCount: false })
    );
    return this.sessionRequest;
  }

  login(email: string, password: string): Observable<AuthenticatedUser> {
    return this.http.post<AuthenticatedUser>(
      `${environment.apiBaseUrl}/auth/login`,
      { email, password }
    ).pipe(tap(user => this.userSubject.next(user)));
  }

  logout(): Observable<void> {
    return this.fetchCsrfToken().pipe(
      switchMap(() => this.http.post<void>(`${environment.apiBaseUrl}/auth/logout`, {})),
      tap(() => this.userSubject.next(null))
    );
  }

  fetchCsrfToken(): Observable<string> {
    return this.http.get<{ requestToken: string }>(
      `${environment.apiBaseUrl}/auth/csrf`
    ).pipe(
      tap(response => this.csrfStore.set(response.requestToken)),
      map(response => response.requestToken)
    );
  }
}
