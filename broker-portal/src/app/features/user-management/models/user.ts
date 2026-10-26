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
  agencyId: number | null;
  agencyName: string | null;
}

export interface PagedUsersResponse {
  users: PortalUser[];
  totalCount: number;
  page: number;
  pageSize: number;
  statusCounts: Record<string, number>;
}

export type InviteRole = 'BROKER_ADMIN' | 'BROKER_USER' | 'UNDERWRITER' | 'UNDERWRITER_MANAGER';

export const ROLE_LABELS: Record<string, string> = {
  PORTAL_SUPER_ADMIN: 'Portal Super Admin',
  UNDERWRITER: 'Underwriter',
  UNDERWRITER_MANAGER: 'Underwriter Manager',
  BROKER_ADMIN: 'Broker Admin',
  BROKER_USER: 'Broker User'
};

export interface Agency {
  id: number;
  name: string;
  duckCreekAgencyId: string | null;
  acceptedDomains: string[];
  products: string[];
  maxActiveUsers: number | null;
  seatsUsed: number;
}

export interface InviteUserRequest {
  firstName?: string | null;
  lastName?: string | null;
  email: string;
  jobTitle?: string | null;
  role: InviteRole;
  agencyId?: number | null;
}

export interface InvitationCreatedResponse {
  user: PortalUser;
  deliveryStatus: 'Sent' | 'Preview' | 'Failed';
  message: string | null;
  developmentActivationUrl: string | null;
  developmentEmailPreviewHtml: string | null;
}

export interface BulkInviteResult {
  email: string;
  succeeded: boolean;
  error: string | null;
  invitation: InvitationCreatedResponse | null;
}
