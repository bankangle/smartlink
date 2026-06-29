import { INTERNAL_API_BASE } from './config';

type FetchFn = typeof fetch;

/** Calls the backend API with the admin bearer token attached. */
export async function adminApi(
  fetchFn: FetchFn,
  token: string | undefined,
  path: string,
  init: RequestInit = {}
): Promise<Response> {
  const headers = new Headers(init.headers);
  if (token) headers.set('Authorization', `Bearer ${token}`);
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
  return fetchFn(`${INTERNAL_API_BASE}${path}`, { ...init, headers });
}

export const TOKEN_COOKIE = 'sl_token';
export const USER_COOKIE = 'sl_user';
export const ADMIN_COOKIE = 'sl_admin'; // '1' for the moderator, '0' for artists
export const PENDING_COOKIE = 'sl_pending'; // '1' while an artist awaits approval
