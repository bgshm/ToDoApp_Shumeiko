import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { RealtimeService } from '../services/realtime.service';

/**
 * Attaches the bearer token, tags the request with this window's SignalR connection id,
 * and treats a 401 as "the session is over".
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const realtime = inject(RealtimeService);
  const router = inject(Router);

  const token = auth.token;
  const connectionId = realtime.connectionId();

  if (token !== null) {
    const headers: Record<string, string> = { Authorization: `Bearer ${token}` };

    // Lets the server leave this window out of the broadcast it is about to send.
    if (connectionId !== null) {
      headers['X-Connection-Id'] = connectionId;
    }

    request = request.clone({ setHeaders: headers });
  }

  return next(request).pipe(
    catchError((error: unknown) => {
      // A rejected token means the stored session is worthless; drop it and start over.
      // The sign-in call itself is exempt, so a failed sign-in shows its own message.
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !request.url.includes('/api/auth/')
      ) {
        auth.clearSession();
        void router.navigate(['/login']);
      }

      return throwError(() => error);
    })
  );
};
