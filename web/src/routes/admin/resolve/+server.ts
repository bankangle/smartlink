import { json, error } from '@sveltejs/kit';
import { adminApi, TOKEN_COOKIE } from '$lib/server/api';
import type { RequestHandler } from './$types';

/** Proxies the Odesli resolve call so the editor can preview links over AJAX. */
export const POST: RequestHandler = async ({ request, cookies, fetch }) => {
  const token = cookies.get(TOKEN_COOKIE);
  const body = await request.json();

  const res = await adminApi(fetch, token, '/api/admin/resolve', {
    method: 'POST',
    body: JSON.stringify({ url: body.url ?? '' })
  });

  if (!res.ok) {
    const text = await res.text();
    throw error(res.status, text || 'Could not resolve that link.');
  }

  return json(await res.json());
};
