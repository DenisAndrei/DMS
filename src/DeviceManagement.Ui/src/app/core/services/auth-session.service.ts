import { computed, Injectable, signal } from '@angular/core';

import { AuthResponse, AuthenticatedUser } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly storageKey = 'device-management-auth-session';
  private readonly sessionState = signal<AuthResponse | null>(this.readStoredSession());

  readonly session = this.sessionState.asReadonly();
  readonly isAuthenticated = computed(() => {
    const session = this.sessionState();
    if (!session) {
      return false;
    }

    return new Date(session.expiresAtUtc).getTime() > Date.now();
  });
  readonly currentUser = computed<AuthenticatedUser | null>(() =>
    this.isAuthenticated() ? (this.sessionState()?.user ?? null) : null
  );
  readonly token = computed(() => (this.isAuthenticated() ? this.sessionState()?.token ?? null : null));

  persistSession(response: AuthResponse): void {
    this.sessionState.set(response);
    localStorage.setItem(this.storageKey, JSON.stringify(response));
  }

  logout(): void {
    this.sessionState.set(null);
    localStorage.removeItem(this.storageKey);
  }

  private readStoredSession(): AuthResponse | null {
    const value = localStorage.getItem(this.storageKey);
    if (!value) {
      return null;
    }

    try {
      const parsed = JSON.parse(value) as AuthResponse;
      if (new Date(parsed.expiresAtUtc).getTime() <= Date.now()) {
        localStorage.removeItem(this.storageKey);
        return null;
      }

      return parsed;
    } catch {
      localStorage.removeItem(this.storageKey);
      return null;
    }
  }
}
