import { redirect } from '@sveltejs/kit';
import { adminApi, TOKEN_COOKIE, USER_COOKIE, ADMIN_COOKIE, PENDING_COOKIE } from '$lib/server/api';
import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = async ({ cookies, url, fetch }) => {
  const token = cookies.get(TOKEN_COOKIE);
  const username = cookies.get(USER_COOKIE);
  const isAdmin = cookies.get(ADMIN_COOKIE) === '1';
  const pending = cookies.get(PENDING_COOKIE) === '1';
  const isPublic = url.pathname === '/admin/login' || url.pathname === '/admin/register';

  if (!token && !isPublic) throw redirect(303, '/admin/login');
  if (token && isPublic) throw redirect(303, '/admin');

  // Surface a count of artists awaiting approval so the moderator can't miss it.
  let pendingArtists = 0;
  if (isAdmin && token) {
    try {
      const res = await adminApi(fetch, token, '/api/admin/artists');
      if (res.ok) {
        const artists = (await res.json()) as { isApproved: boolean }[];
        pendingArtists = artists.filter((a) => !a.isApproved).length;
      }
    } catch {
      // Non-fatal: the badge just won't show.
    }
  }

  return { username, isAdmin, pending, pendingArtists };
};
