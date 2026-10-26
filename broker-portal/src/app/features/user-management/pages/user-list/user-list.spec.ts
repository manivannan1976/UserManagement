import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';

import { AccountSessionService, AuthenticatedUser } from '../../../../core/auth/account-session.service';
import { UserListComponent } from './user-list';
import { UserManagementService } from '../../services/user-managemen';
import { PortalUser } from '../../models/user';

const buildUser = (overrides: Partial<PortalUser> = {}): PortalUser => ({
  id: '17',
  firstName: 'Hari',
  lastName: 'Kishan',
  email: 'hari@gmail.com',
  jobTitle: null,
  role: 'BROKER_USER',
  status: 'Invited',
  createdAt: '2026-10-22T00:00:00Z',
  agencyId: 2,
  agencyName: 'Acme Brokerage',
  ...overrides
});

describe('UserListComponent', () => {
  let component: UserListComponent;
  let fixture: ComponentFixture<UserListComponent>;
  let userService: jasmine.SpyObj<UserManagementService>;

  const sessionUser: AuthenticatedUser = {
    id: 5,
    email: 'boss@acme.com',
    firstName: 'Ada',
    lastName: 'Boss',
    role: 'BROKER_ADMIN',
    agencyId: 2,
    agencyName: 'Acme Brokerage',
    workspace: { key: 'DUCK_CREEK_BROKER', label: 'Duck Creek broker workspace', url: null },
    jobTitle: null
  };

  beforeEach(async () => {
    userService = jasmine.createSpyObj<UserManagementService>(
      'UserManagementService', ['getUsers', 'getAgencies', 'deactivateUser', 'reactivateUser']);
    userService.getUsers.and.returnValue(of({
      users: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
      statusCounts: {}
    }));
    userService.getAgencies.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [UserListComponent],
      providers: [
        { provide: UserManagementService, useValue: userService },
        { provide: AccountSessionService, useValue: { checkSession: () => of(sessionUser) } },
        {
          provide: MatDialog,
          useValue: { open: () => ({ afterClosed: () => of(undefined) }) }
        }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(UserListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('scopes the page to the broker admin agency and hides the agency column', () => {
    expect(component.isSuperAdmin).toBeFalse();
    expect(component.agencyName).toBe('Acme Brokerage');
    expect(userService.getAgencies).not.toHaveBeenCalled();
  });

  it('allows resending to invited and expired users only', () => {
    expect(component.isInvitationPending(buildUser({ status: 'Invited' }))).toBeTrue();
    expect(component.isInvitationPending(buildUser({ status: 'Expired' }))).toBeTrue();
    expect(component.isInvitationPending(buildUser({ status: 'Active' }))).toBeFalse();
    expect(component.isInvitationPending(buildUser({ status: 'Deactivated' }))).toBeFalse();
  });

  it('normalizes whitespace and case when checking invited status', () => {
    const apiUser = buildUser({ status: ' invited ' as PortalUser['status'] });

    expect(component.isInvitationPending(apiUser)).toBeTrue();
  });

  it('offers deactivate for others but never for yourself or a super admin', () => {
    expect(component.canDeactivate(buildUser({ status: 'Active' }))).toBeTrue();
    expect(component.canDeactivate(buildUser({ id: '5', status: 'Active' }))).toBeFalse();
    expect(component.canDeactivate(buildUser({ role: 'PORTAL_SUPER_ADMIN' }))).toBeFalse();
    expect(component.canDeactivate(buildUser({ status: 'Deactivated' }))).toBeFalse();
  });

  it('offers reactivate only for deactivated users', () => {
    expect(component.canReactivate(buildUser({ status: 'Deactivated' }))).toBeTrue();
    expect(component.canReactivate(buildUser({ status: 'Active' }))).toBeFalse();
  });

  it('does not deactivate when the confirmation is declined', () => {
    spyOn(window, 'confirm').and.returnValue(false);

    component.deactivate(buildUser({ status: 'Active' }));

    expect(userService.deactivateUser).not.toHaveBeenCalled();
  });

  it('deactivates after confirmation', () => {
    spyOn(window, 'confirm').and.returnValue(true);
    userService.deactivateUser.and.returnValue(of(buildUser({ status: 'Deactivated' })));

    component.deactivate(buildUser({ status: 'Active' }));

    expect(userService.deactivateUser).toHaveBeenCalledWith('17');
    expect(component.message).toContain('was deactivated');
  });

  it('shows a placeholder name until an invitee confirms theirs', () => {
    expect(component.displayName(buildUser({ firstName: '', lastName: '' }))).toBe('Awaiting activation');
    expect(component.initials(buildUser({ firstName: '', lastName: '' }))).toBe('H');
  });
});
