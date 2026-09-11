import { HttpErrorResponse } from '@angular/common/http';

/**
 * Turns a failed request into something worth showing a person. The API returns RFC 7807
 * problem documents, so prefer their detail text and fall back by status.
 */
export function describeHttpError(error: unknown, fallback = 'Something went wrong.'): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'Cannot reach the server. Check that the API is running.';
  }

  const body = error.error as { detail?: unknown; title?: unknown; errors?: unknown } | null;

  if (body !== null && typeof body === 'object') {
    // ASP.NET model validation returns a map of field -> messages.
    if (body.errors !== null && typeof body.errors === 'object') {
      const messages = Object.values(body.errors as Record<string, unknown>)
        .flatMap((value) => (Array.isArray(value) ? (value as unknown[]) : [value]))
        .filter((value): value is string => typeof value === 'string');

      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (typeof body.detail === 'string' && body.detail.length > 0) {
      return body.detail;
    }

    if (typeof body.title === 'string' && body.title.length > 0) {
      return body.title;
    }
  }

  return error.status === 401 ? 'Your session has expired. Please sign in again.' : fallback;
}
