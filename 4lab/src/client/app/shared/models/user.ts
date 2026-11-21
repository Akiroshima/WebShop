export type Role = 'customer' | 'admin';

export interface PublicUser {
  id: string;
  login: string;
  fullName: string;
  email: string;
  phone: string | null;
  role: Role;
}

export interface DbUser extends PublicUser {
  password: string;
  createdAt: string;
}

export interface Profile {
  fullName: string;
  email: string;
  phone: string | null;
}

export type RecoveryChannel = 'email' | 'phone';

export interface RecoveryRequestSent {
  channel: RecoveryChannel;
  maskedContact: string;
}

export interface RecoveryConfirmResult {
  resetToken: string;
}

export interface LoginResponse {
  user: PublicUser;
}
