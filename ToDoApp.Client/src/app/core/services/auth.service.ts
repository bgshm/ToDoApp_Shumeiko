import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, of, tap } from 'rxjs';
import { API_ROUTES, apiUrl } from '../api.config';
import { AuthConfigDto, AuthResponse, UserDto } from '../models/auth.models';

const TOKEN_STORAGE_KEY = 'todoapp.access-token';

/** Holds the session: the bearer token, the signed-in profile, and how to get both. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly tokenSignal = signal<string | null>(readStoredToken());
  private readonly userSignal = signal<UserDto | null>(null);
  private readonly configSignal = signal<AuthConfigDto | null>(null);

  readonly user = this.userSignal.asReadonly();
  readonly config = this.configSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.tokenSignal() !== null);

  /** Read synchronously by the HTTP interceptor and the SignalR connection. */
  get token(): string | null {
    return this.tokenSignal();
  }

  loadConfig(): Observable<AuthConfigDto> {
    return this.http
      .get<AuthConfigDto>(apiUrl(API_ROUTES.authConfig))
      .pipe(tap((config) => this.configSignal.set(config)));
  }

  loginWithGoogle(credential: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(apiUrl(API_ROUTES.google), { credential })
      .pipe(tap((response) => this.acceptSession(response)));
  }

  devLogin(email: string, displayName: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(apiUrl(API_ROUTES.devLogin), { email, displayName })
      .pipe(tap((response) => this.acceptSession(response)));
  }

  /**
   * Turns a token left in storage by a previous visit back into a session. Resolves to null
   * when there is no token or the server rejects it, in which case the session is cleared.
   */
  restoreSession(): Observable<UserDto | null> {
    if (this.tokenSignal() === null) {
      return of(null);
    }

    return this.http.get<UserDto>(apiUrl(API_ROUTES.me)).pipe(
      tap((user) => this.userSignal.set(user)),
      catchError(() => {
        this.clearSession();
        return of(null);
      })
    );
  }

  /** Tells the server the session is over, then drops the token whatever the server said. */
  logout(): Observable<unknown> {
    const request = this.tokenSignal() === null
      ? of(null)
      : this.http.post(apiUrl(API_ROUTES.logout), {}).pipe(catchError(() => of(null)));

    return request.pipe(tap(() => this.clearSession()));
  }

  clearSession(): void {
    this.tokenSignal.set(null);
    this.userSignal.set(null);
    writeStoredToken(null);
  }

  private acceptSession(response: AuthResponse): void {
    this.tokenSignal.set(response.accessToken);
    this.userSignal.set(response.user);
    writeStoredToken(response.accessToken);
  }
}

/**
 * Storage can throw outright when a browser is set to block site data, so every access is
 * guarded and a failure just means "no stored session".
 */
function readStoredToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  } catch {
    return null;
  }
}

function writeStoredToken(token: string | null): void {
  try {
    if (token === null) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    } else {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
    }
  } catch {
    // A session that lives only until the tab closes is better than a crash.
  }
}
