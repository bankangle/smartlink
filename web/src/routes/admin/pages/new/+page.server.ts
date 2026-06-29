import { fail, redirect } from '@sveltejs/kit';
import { adminApi, TOKEN_COOKIE } from '$lib/server/api';
import type { Actions } from './$types';

export const actions: Actions = {
  save: async ({ request, cookies, fetch }) => {
    const token = cookies.get(TOKEN_COOKIE);
    const form = await request.formData();
    const payload = String(form.get('payload') ?? '{}');

    const res = await adminApi(fetch, token, '/api/admin/pages', {
      method: 'POST',
      body: payload
    });

    if (!res.ok) {
      const msg = await res.text();
      return fail(res.status, { error: msg || 'Could not create the page.' });
    }

    const created = (await res.json()) as { id: string };
    throw redirect(303, `/admin/pages/${created.id}?created=1`);
  }
};
