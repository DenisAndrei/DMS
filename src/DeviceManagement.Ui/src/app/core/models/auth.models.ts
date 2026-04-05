export interface AuthenticatedUser {
  userId: number;
  email: string;
  name: string;
  role: string;
  location: string;
}

export interface AuthResponse {
  token: string;
  expiresAtUtc: string;
  user: AuthenticatedUser;
}

export interface AuthRequest {
  email: string;
  password: string;
}
