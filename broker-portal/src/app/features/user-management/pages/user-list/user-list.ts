import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  ElementRef,
  OnInit,
  ViewChild,
  inject
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { EMPTY, Subject, catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import {
  Agency,
  InvitationCreatedResponse,
  PagedUsersResponse,
  PortalUser,
  ROLE_LABELS,
  UserStatus
} from '../../models/user';
import { UserManagementService, UserQuery } from '../../services/user-managemen';
import { InviteDialogResult, InviteUserDialog } from './invite-user-dialog';
import { AccountSessionService } from '../../../../core/auth/account-session.service';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule
  ],
  templateUrl: './user-list.html',
  styleUrl: './user-list.scss'
})
export class UserListComponent implements OnInit {
  private readonly userService = inject(UserManagementService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly changeDetector = inject(ChangeDetectorRef);
  private readonly session = inject(AccountSessionService);
  private readonly queryChanges = new Subject<UserQuery>();

  @ViewChild('invitationFeedback')
  private invitationFeedback?: ElementRef<HTMLElement>;

  searchText = '';
  selectedStatus: UserStatus | 'All' = 'All';
  selectedRole = 'All';
  selectedAgencyId: number | null = null;
  agencies: Agency[] = [];
  isSuperAdmin = false;
  currentUserId: number | null = null;
  agencyName: string | null = null;
  readonly roleLabels = ROLE_LABELS;
  message = '';
  users: PortalUser[] = [];
  totalCount = 0;
  statusCounts: Record<string, number> = {};
  page = 1;
  pageSize = 10;
  readonly pageSizes = [10, 25, 50, 100];
  loading = false;
  error = '';
  private refreshKey = 0;
  activationPreviewUrl: string | null = null;
  emailPreviewHtml: string | null = null;

  ngOnInit(): void {
    this.session.checkSession().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(user => {
      this.isSuperAdmin = user?.role === 'PORTAL_SUPER_ADMIN';
      this.currentUserId = user?.id ?? null;
      this.agencyName = user?.agencyName ?? null;
      if (this.isSuperAdmin) {
        this.userService.getAgencies().pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({ next: agencies => this.agencies = agencies, error: () => this.agencies = [] });
      }
    });

    this.queryChanges.pipe(
      debounceTime(250),
      distinctUntilChanged((previous, current) =>
        previous.page === current.page &&
        previous.pageSize === current.pageSize &&
        previous.search.trim() === current.search.trim() &&
        previous.status === current.status &&
        previous.role === current.role &&
        previous.agencyId === current.agencyId &&
        previous.refreshKey === current.refreshKey
      ),
      switchMap(query => {
        this.loading = true;
        this.error = '';
        return this.userService.getUsers(query).pipe(
          catchError(error => {
            this.loading = false;
            this.users = [];
            this.totalCount = 0;
            this.statusCounts = {};
            this.error = this.getLoadError(error);
            return EMPTY;
          })
        );
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(result => this.applyResult(result));

    this.loadUsers();
  }

  get filteredUsers(): PortalUser[] {
    return this.users;
  }

  get totalUsers(): number {
    return this.totalCount;
  }

  get activeUsers(): number {
    return this.statusCounts['Active'] ?? 0;
  }

  get pendingUsers(): number {
    return this.statusCounts['Invited'] ?? 0;
  }

  get incompleteUsers(): number {
    return this.statusCounts['Setup Incomplete'] ?? 0;
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get rangeStart(): number {
    return this.totalCount === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  getStatusClass(status: UserStatus): string {
    const statusClasses: Record<UserStatus, string> = {
      Active: 'status-active',
      Invited: 'status-invited',
      Expired: 'status-expired',
      Deactivated: 'status-deactivated',
      'Setup Incomplete': 'status-incomplete'
    };

    return statusClasses[status];
  }

  /** Pending and expired invitations can be resent without re-entering the user's data. */
  isInvitationPending(user: PortalUser): boolean {
    const status = user.status.trim().toLowerCase();
    return status === 'invited' || status === 'expired';
  }

  canDeactivate(user: PortalUser): boolean {
    return user.status !== 'Deactivated' && user.role !== 'PORTAL_SUPER_ADMIN' &&
      String(this.currentUserId) !== user.id;
  }

  canReactivate(user: PortalUser): boolean {
    return user.status === 'Deactivated';
  }

  displayName(user: PortalUser): string {
    const name = `${user.firstName} ${user.lastName}`.trim();
    return name || 'Awaiting activation';
  }

  initials(user: PortalUser): string {
    const letters = `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.trim();
    return (letters || user.email.charAt(0)).toUpperCase();
  }

  deactivate(user: PortalUser): void {
    const confirmed = window.confirm(
      `Deactivate ${this.displayName(user)} (${user.email})?

` +
      'They will lose access to the portal immediately. You can reactivate them later.');
    if (!confirmed) {
      return;
    }

    this.userService.deactivateUser(user.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.afterStatusChange(`${user.email} was deactivated.`),
      error: error => this.showActionError(error, 'The user could not be deactivated.')
    });
  }

  reactivate(user: PortalUser): void {
    this.userService.reactivateUser(user.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => this.afterStatusChange(`${user.email} was reactivated.`),
      error: error => this.showActionError(error, 'The user could not be reactivated.')
    });
  }

  private afterStatusChange(message: string): void {
    this.message = message;
    this.activationPreviewUrl = null;
    this.emailPreviewHtml = null;
    this.refreshKey++;
    this.loadUsers();
    this.scrollToInvitationFeedback();
  }

  private showActionError(error: unknown, fallback: string): void {
    this.message = error instanceof HttpErrorResponse && typeof error.error?.message === 'string'
      ? error.error.message
      : fallback;
    this.scrollToInvitationFeedback();
  }

  onFiltersChanged(): void {
    this.page = 1;
    this.loadUsers();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.page) {
      return;
    }
    this.page = page;
    this.loadUsers();
  }

  onPageSizeChanged(): void {
    this.page = 1;
    this.loadUsers();
  }

  clearFilters(): void {
    this.searchText = '';
    this.selectedStatus = 'All';
    this.selectedRole = 'All';
    this.selectedAgencyId = null;
    this.message = '';
    this.onFiltersChanged();
  }

  onInviteUser(): void {
    this.dialog.open(InviteUserDialog, {
      width: '580px',
      maxWidth: 'calc(100vw - 32px)',
      disableClose: true
    }).afterClosed().pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe((result: InviteDialogResult | undefined) => {
      if (!result) {
        return;
      }

      this.activationPreviewUrl = result.activationUrl;
      this.emailPreviewHtml = result.emailPreviewHtml;
      this.message = result.message;
      this.searchText = '';
      this.selectedStatus = 'All';
      this.selectedRole = 'All';
      this.page = 1;
      this.refreshKey++;
      this.loadUsers();
    });
  }

  resendInvitation(user: PortalUser): void {
    if (!this.isInvitationPending(user)) {
      return;
    }

    this.userService.resendInvitation(user.id).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (response: InvitationCreatedResponse) => {
        this.message = response.deliveryStatus === 'Sent'
          ? `A new invitation was sent to ${user.email}.`
          : `A replacement invitation was created for ${user.email}. ${response.message ?? ''}`;
        this.activationPreviewUrl = response.developmentActivationUrl;
        this.emailPreviewHtml = response.developmentEmailPreviewHtml;
        this.refreshKey++;
        this.loadUsers();
        this.scrollToInvitationFeedback();
      },
      error: error => {
        this.message = error instanceof HttpErrorResponse && typeof error.error?.message === 'string'
          ? error.error.message
          : 'The invitation could not be resent. Please try again.';
        this.scrollToInvitationFeedback();
      }
    });
  }

  onViewUser(user: PortalUser): void {
    this.message =
      `Selected ${user.firstName} ${user.lastName} (${user.id}). ` +
      'User details will be implemented next.';
  }

  private loadUsers(): void {
    this.loading = true;
    this.queryChanges.next({
      page: this.page,
      pageSize: this.pageSize,
      search: this.searchText,
      status: this.selectedStatus,
      role: this.selectedRole,
      agencyId: this.selectedAgencyId,
      refreshKey: this.refreshKey
    });
  }

  private applyResult(result: PagedUsersResponse): void {
    this.users = result.users;
    this.totalCount = result.totalCount;
    this.page = result.page;
    this.pageSize = result.pageSize;
    this.statusCounts = result.statusCounts;
    this.loading = false;
  }

  private scrollToInvitationFeedback(): void {
    this.changeDetector.detectChanges();
    this.invitationFeedback?.nativeElement.scrollIntoView({
      behavior: 'smooth',
      block: 'center'
    });
  }

  private getLoadError(error: unknown): string {
    if (error instanceof HttpErrorResponse && error.status === 0) {
      return 'Unable to reach the Users API. Check that the API is running and its database connection is configured.';
    }

    return 'The users could not be loaded. Please try again later.';
  }
}