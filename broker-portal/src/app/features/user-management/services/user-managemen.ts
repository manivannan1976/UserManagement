import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable, switchMap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AccountSessionService } from '../../../core/auth/account-session.service';
import {
  InviteUserRequest,
  InvitationCreatedResponse,
  PagedUsersResponse,
  PortalUser
} from '../models/user';

interface ApiPortalUser extends Omit<PortalUser, 'id'> {
  id: number;
}

interface ApiPagedUsersResponse extends Omit<PagedUsersResponse, 'users'> {
  users: ApiPortalUser[];
}

export interface UserQuery {
  page: number;
  pageSize: number;
  search: string;
  status: string;
  role: string;
  refreshKey?: number;
}

@Injectable({
  providedIn: 'root',
})
export class UserManagementService {
  private readonly http = inject(HttpClient);
  private readonly session = inject(AccountSessionService);

  getUsers(query: UserQuery): Observable<PagedUsersResponse> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    const search = query.search.trim();
    if (search) {
      params = params.set('search', search);
    }

    if (query.status !== 'All') {
      params = params.set('status', query.status);
    }

    if (query.role !== 'All') {
      params = params.set('role', query.role);
    }

    return this.http.get<ApiPagedUsersResponse>(`${environment.apiBaseUrl}/users`, { params }).pipe(
      map(response => ({
        ...response,
        users: response.users.map(user => ({ ...user, id: String(user.id) }))
      }))
    );
  }

  inviteUser(request: InviteUserRequest): Observable<InvitationCreatedResponse> {
    return this.session.fetchCsrfToken().pipe(
      switchMap(() => this.http.post<
        Omit<InvitationCreatedResponse, 'user'> & { user: ApiPortalUser }
      >(`${environment.apiBaseUrl}/users/invite`, request)),
      map(response => ({
        ...response,
        user: { ...response.user, id: String(response.user.id) }
      }))
    );
  }

  resendInvitation(userId: string): Observable<InvitationCreatedResponse> {
    return this.session.fetchCsrfToken().pipe(
      switchMap(() => this.http.post<
        Omit<InvitationCreatedResponse, 'user'> & { user: ApiPortalUser }
      >(`${environment.apiBaseUrl}/users/${encodeURIComponent(userId)}/resend-invitation`, {})),
      map(response => ({
        ...response,
        user: { ...response.user, id: String(response.user.id) }
      }))
    );
  }
}
