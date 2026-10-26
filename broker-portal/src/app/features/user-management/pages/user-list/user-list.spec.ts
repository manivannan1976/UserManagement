import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';

import { UserListComponent } from './user-list';
import { UserManagementService } from '../../services/user-managemen';
import { PortalUser } from '../../models/user';

describe('UserListComponent', () => {
  let component: UserListComponent;
  let fixture: ComponentFixture<UserListComponent>;
  let userService: jasmine.SpyObj<UserManagementService>;

  beforeEach(async () => {
    userService = jasmine.createSpyObj<UserManagementService>('UserManagementService', ['getUsers']);
    userService.getUsers.and.returnValue(of({
      users: [],
      totalCount: 0,
      page: 1,
      pageSize: 10,
      statusCounts: {}
    }));

    await TestBed.configureTestingModule({
      imports: [UserListComponent],
      providers: [
        { provide: UserManagementService, useValue: userService },
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

  it('shows resend eligibility for invited users only', () => {
    const invitedUser: PortalUser = {
      id: '17',
      firstName: 'Hari',
      lastName: 'Kishan',
      email: 'hari@gmail.com',
      jobTitle: null,
      role: 'BROKER_USER',
      status: 'Invited',
      createdAt: '2026-10-22T00:00:00Z'
    };
    const activeUser = { ...invitedUser, status: 'Active' as const };

    expect(component.isInvitationPending(invitedUser)).toBeTrue();
    expect(component.isInvitationPending(activeUser)).toBeFalse();
  });

  it('normalizes whitespace and case when checking invited status', () => {
    const invitedUser: PortalUser = {
      id: '17',
      firstName: 'Hari',
      lastName: 'Kishan',
      email: 'hari@gmail.com',
      jobTitle: null,
      role: 'BROKER_USER',
      status: 'Invited',
      createdAt: '2026-10-22T00:00:00Z'
    };
    const apiUser = { ...invitedUser, status: ' invited ' as PortalUser['status'] };

    expect(component.isInvitationPending(apiUser)).toBeTrue();
  });
});
