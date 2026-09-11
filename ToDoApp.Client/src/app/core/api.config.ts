/**
 * Base URL for the REST API and the SignalR hub.
 *
 * Empty means same-origin: in development the Angular dev server proxies /api and /hubs
 * to the .NET server (see proxy.conf.json), and in a deployed setup the two are expected
 * to sit behind one origin. Point this at an absolute URL to talk to an API elsewhere --
 * the server's CORS policy already allows the dev-server origins.
 */
export const API_BASE_URL = '';

export const API_ROUTES = {
  authConfig: '/api/auth/config',
  google: '/api/auth/google',
  devLogin: '/api/auth/dev-login',
  me: '/api/auth/me',
  logout: '/api/auth/logout',
  tasks: '/api/tasks',
  board: '/api/tasks/board',
  categories: '/api/categories',
  boardHub: '/hubs/board'
} as const;

export function apiUrl(path: string): string {
  return `${API_BASE_URL}${path}`;
}
