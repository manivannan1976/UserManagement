import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AccountSessionService } from '../../../core/auth/account-session.service';

import { UserManagementService } from './user-managemen';

describe('UserManagementService', () => {
  let service: UserManagementService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AccountSessionService, useValue: { fetchCsrfToken: () => of('csrf-token') } }
      ]
    });
    service = TestBed.inject(UserManagementService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('requests a page with the selected search and filters', () => {
    service.getUsers({
      page: 2,
      pageSize: 25,
      search: '  atik ',
      status: 'Active',
      role: 'Broker Admin'
    }).subscribe();

    const request = httpTestingController.expectOne(
      request => request.url === `${environment.apiBaseUrl}/users`
    );
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('25');
    expect(request.request.params.get('search')).toBe('atik');
    expect(request.request.params.get('status')).toBe('Active');
    expect(request.request.params.get('role')).toBe('Broker Admin');
    request.flush({
      users: [],
      totalCount: 0,
      page: 2,
      pageSize: 25,
      statusCounts: {}
    });
    httpTestingController.verify();
  });

  it('posts a broker invitation and maps the created user id', () => {
    const invitedUser = {
      id: 17,
      firstName: 'Sam',
      lastName: 'Example',
      email: 'sam@example.com',
      jobTitle: null,
      role: 'BROKER_USER',
      status: 'Invited' as const,
      createdAt: '2026-10-22T08:00:00Z'
    };

    service.inviteUser({
      firstName: 'Sam',
      lastName: 'Example',
      email: 'sam@example.com',
      jobTitle: null,
      role: 'BROKER_USER'
    }).subscribe(response => expect(response.user.id).toBe('17'));

    const request = httpTestingController.expectOne(`${environment.apiBaseUrl}/users/invite`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body.role).toBe('BROKER_USER');
    request.flush({
      user: invitedUser,
      deliveryStatus: 'Preview',
      message: 'Email was not sent.',
      developmentActivationUrl: 'http://localhost:4200/activate-invitation?token=preview',
      developmentEmailPreviewHtml: '<p>S|2</p>'
    });
    httpTestingController.verify();
  });
});
