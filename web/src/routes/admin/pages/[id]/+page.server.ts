import { error, fail, redirect } from '@sveltejs/kit';
import { adminApi, TOKEN_COOKIE } from '$lib/server/api';
import type { AdminPage, PageStats } from '$lib/types';
import type { Actions, PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, cookies, fetch }) => {
  const token = cookies.get(TOKEN_COOKIE);

  const [pageRes, statsRes] = await Promise.all([
    adminApi(fetch, token, `/api/admin/pages/${params.id}`),
    adminApi(fetch, token, `/api/admin/pages/${params.id}/stats`)
  ]);

  if (pageRes.status === 404) throw error(404, 'Page not found.');
  if (!pageRes.ok) throw error(pageRes.status, 'Could not load the page.');

  const page = (await pageRes.json()) as AdminPage;
  const stats = statsRes.ok ? ((await statsRes.json()) as PageStats) : null;

  return { adminPage: page, stats };
};

export const actions: Actions = {
  save: async ({ params, request, cookies, fetch }) => {
    const token = cookies.get(TOKEN_COOKIE);
    const form = await request.formData();
    const payload = String(form.get('payload') ?? '{}');

    const res = await adminApi(fetch, token, `/api/admin/pages/${params.id}`, {
      method: 'PUT',
      body: payload
    });

    if (!res.ok) {
      const msg = await res.text();
      return fail(res.status, { error: msg || 'Could not save changes.' });
    }
    return { saved: true };
  },

  delete: async ({ params, cookies, fetch }) => {
    const token = cookies.get(TOKEN_COOKIE);
    const res = await adminApi(fetch, token, `/api/admin/pages/${params.id}`, { method: 'DELETE' });
    if (!res.ok) return fail(res.status, { error: 'Could not delete the page.' });
    throw redirect(303, '/admin');
  }
};
