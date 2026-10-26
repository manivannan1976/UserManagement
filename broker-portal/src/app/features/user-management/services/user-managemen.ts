import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable, switchMap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AccountSessionService } from '../../../core/auth/account-session.service';
import {
  Agency,
  BulkInviteResult,
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

type ApiInvitationCreated = Omit<InvitationCreatedResponse, 'user'> & { user: ApiPortalUser };

export interface UserQuery {
  page: number;
  pageSize: number;
  search: string;
  status: string;
  role: string;
  agencyId?: number | null;
  refreshKey?: number;
}

const toUser = (user: ApiPortalUser): PortalUser => ({ ...user, id: String(user.id) });
const toInvitation = (response: ApiInvitationCreated): InvitationCreatedResponse => ({
  ...response,
  user: toUser(response.user)
});

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

    if (query.agencyId) {
      params = params.set('agencyId', query.agencyId);
    }

    return this.http.get<ApiPagedUsersResponse>(`${environment.apiBaseUrl}/users`, { params }).pipe(
      map(response => ({ ...response, users: response.users.map(toUser) }))
    );
  }

  getAgencies(): Observable<Agency[]> {
    return this.http.get<Agency[]>(`${environment.apiBaseUrl}/agencies`);
  }

  inviteUser(request: InviteUserRequest): Observable<InvitationCreatedResponse> {
    return this.session.fetchCsrfToken().pipe(
      switchMap(() => this.http.post<ApiInvitationCreated>(
        `${environment.apiBaseUrl}/users/invite`, request)),
      map(toInvitation)
    );
  }

  /** Invites many users at once; each row succeeds or fails independently. */
  inviteUsers(invitations: InviteUserRequest[]): Observable<BulkInviteResult[]> {
    return this.session.fetchCsrfToken().pipe(
      switchMap(() => this.http.post<
        (Omit<BulkInviteResult, 'invitation'> & { invitation: ApiInvitationCreated | null })[]
      >(`${environment.apiBaseUrl}/users/invite/bulk`, { invitations })),
      map(results => results.map(result => ({
        ...result,
        invitation: result.invitation ? toInvitation(result.invitation) : null
      })))
    );
  }

  resendInvitation(userId: string): Observable<InvitationCreatedResponse> {
    return this.session.fetchCsrfToken().pipe(
      switchMap(() => this.http.post<ApiInvitationCreated>(
        `${environment.apiBaseUrl}/users/${encodeURIComponent(userId)}/resend-invitation`, {})),
      map(toInvitation)
    );
  }

  deactivateUser(userId: string): Observable<PortalUser> {
    return this.session.fetchCsrfToken().pipe(
      switchMap(() => this.http.post<ApiPortalUser>(
        `${environment.apiBaseUrl}/users/${encodeURIComponent(userId)}/deactivate`,
        { confirm: true })),
      map(toUser)
    );
  }

  reactivateUser(userId: string): Observable<PortalUser> {
    return this.session.fetchCsrfToken().pipe(
      switchMap(() => this.http.post<ApiPortalUser>(
        `${environment.apiBaseUrl}/users/${encodeURIComponent(userId)}/reactivate`, {})),
      map(toUser)
    );
  }
}
