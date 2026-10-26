import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AccountSessionService } from '../../core/auth/account-session.service';

export interface InvitationValidation {
  isValid: boolean;
  email: string | null;
  state: string | null;
}

@Injectable({ providedIn: 'root' })
export class InvitationService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(AccountSessionService);

  validate(token: string): Observable<InvitationValidation> {
    return this.http.get<InvitationValidation>(`${environment.apiBaseUrl}/invitations/validate`, {
      params: { token }
    });
  }

  activate(token: string, password: string, confirmPassword: string): Observable<void> {
    return this.session.fetchCsrfToken().pipe(
      switchMap(() => this.http.post<void>(`${environment.apiBaseUrl}/invitations/activate`, {
        token,
        password,
        confirmPassword
      }))
    );
  }
}
