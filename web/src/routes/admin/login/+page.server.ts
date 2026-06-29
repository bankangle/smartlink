import { fail, redirect } from '@sveltejs/kit';
import { INTERNAL_API_BASE } from '$lib/server/config';
import { TOKEN_COOKIE, USER_COOKIE, ADMIN_COOKIE, PENDING_COOKIE } from '$lib/server/api';
import type { Actions } from './$types';

const TWELVE_HOURS = 60 * 60 * 12;

export const actions: Actions = {
  login: async ({ request, cookies, fetch }) => {
    const form = await request.formData();
    const username = String(form.get('username') ?? '').trim();
    const password = String(form.get('password') ?? '');

    if (!username || !password) return fail(400, { error: 'Enter a username and password.', username });

    const res = await fetch(`${INTERNAL_API_BASE}/api/admin/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });

    if (!res.ok) return fail(401, { error: 'Invalid username or password.', username });

    const data = (await res.json()) as {
      token: string;
      username: string;
      isAdmin: boolean;
      isApproved: boolean;
    };
    const base = { path: '/', sameSite: 'lax' as const, maxAge: TWELVE_HOURS };
    cookies.set(TOKEN_COOKIE, data.token, { ...base, httpOnly: true });
    cookies.set(USER_COOKIE, data.username, { ...base, httpOnly: false });
    cookies.set(ADMIN_COOKIE, data.isAdmin ? '1' : '0', { ...base, httpOnly: false });
    cookies.set(PENDING_COOKIE, data.isApproved ? '0' : '1', { ...base, httpOnly: false });

    throw redirect(303, '/admin');
  },

  logout: async ({ cookies }) => {
    cookies.delete(TOKEN_COOKIE, { path: '/' });
    cookies.delete(USER_COOKIE, { path: '/' });
    cookies.delete(ADMIN_COOKIE, { path: '/' });
    cookies.delete(PENDING_COOKIE, { path: '/' });
    throw redirect(303, '/admin/login');
  }
};
