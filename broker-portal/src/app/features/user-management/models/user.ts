export type UserStatus =
  | 'Active'
  | 'Invited'
  | 'Expired'
  | 'Deactivated'
  | 'Setup Incomplete';

export interface PortalUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  jobTitle: string | null;
  role: string;
  status: UserStatus;
  createdAt: string;
}

export interface PagedUsersResponse {
  users: PortalUser[];
  totalCount: number;
  page: number;
  pageSize: number;
  statusCounts: Record<string, number>;
}

export type BrokerRole = 'BROKER_ADMIN' | 'BROKER_USER';

export interface InviteUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  jobTitle: string | null;
  role: BrokerRole;
}

export interface InvitationCreatedResponse {
  user: PortalUser;
  deliveryStatus: 'Sent' | 'Preview' | 'Failed';
  message: string | null;
  developmentActivationUrl: string | null;
  developmentEmailPreviewHtml: string | null;
}