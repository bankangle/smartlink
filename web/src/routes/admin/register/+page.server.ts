import { fail, redirect } from '@sveltejs/kit';
import { INTERNAL_API_BASE } from '$lib/server/config';
import { TOKEN_COOKIE, USER_COOKIE, ADMIN_COOKIE, PENDING_COOKIE } from '$lib/server/api';
import type { Actions } from './$types';

const TWELVE_HOURS = 60 * 60 * 12;

export const actions: Actions = {
  register: async ({ request, cookies, fetch }) => {
    const form = await request.formData();
    const username = String(form.get('username') ?? '').trim();
    const password = String(form.get('password') ?? '');
    const confirm = String(form.get('confirm') ?? '');

    if (username.length < 3) return fail(400, { error: 'Username must be at least 3 characters.', username });
    if (password.length < 6) return fail(400, { error: 'Password must be at least 6 characters.', username });
    if (password !== confirm) return fail(400, { error: 'Passwords don’t match.', username });

    const res = await fetch(`${INTERNAL_API_BASE}/api/admin/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });

    if (res.status === 409) return fail(409, { error: 'That username is already taken.', username });
    if (!res.ok) return fail(400, { error: 'Couldn’t create the account — please try again.', username });

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
  }
};
