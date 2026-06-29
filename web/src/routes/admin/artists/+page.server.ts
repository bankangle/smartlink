import { error, fail, redirect } from '@sveltejs/kit';
import { adminApi, TOKEN_COOKIE, ADMIN_COOKIE } from '$lib/server/api';
import type { Actions, PageServerLoad } from './$types';

interface ArtistRow {
  id: string;
  username: string;
  isApproved: boolean;
  pageCount: number;
  createdAt: string;
}

export const load: PageServerLoad = async ({ cookies, fetch }) => {
  // Moderation is moderator-only; bounce artists back to their dashboard.
  if (cookies.get(ADMIN_COOKIE) !== '1') throw redirect(303, '/admin');

  const token = cookies.get(TOKEN_COOKIE);
  const res = await adminApi(fetch, token, '/api/admin/artists');
  if (!res.ok) throw error(res.status, 'Could not load artists.');
  const artists = (await res.json()) as ArtistRow[];
  return { artists };
};

export const actions: Actions = {
  approve: async ({ request, cookies, fetch }) => {
    const id = String((await request.formData()).get('id') ?? '');
    const res = await adminApi(fetch, cookies.get(TOKEN_COOKIE), `/api/admin/artists/${id}/approve`, {
      method: 'POST'
    });
    if (!res.ok) return fail(res.status, { error: 'Could not approve that artist.' });
    return { ok: true };
  },
  revoke: async ({ request, cookies, fetch }) => {
    const id = String((await request.formData()).get('id') ?? '');
    const res = await adminApi(fetch, cookies.get(TOKEN_COOKIE), `/api/admin/artists/${id}/revoke`, {
      method: 'POST'
    });
    if (!res.ok) return fail(res.status, { error: 'Could not revoke that artist.' });
    return { ok: true };
  }
};
