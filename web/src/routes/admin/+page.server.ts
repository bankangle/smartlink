import { error } from '@sveltejs/kit';
import { adminApi, TOKEN_COOKIE } from '$lib/server/api';
import type { PageListItem } from '$lib/types';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ cookies, fetch }) => {
  const token = cookies.get(TOKEN_COOKIE);
  const res = await adminApi(fetch, token, '/api/admin/pages');
  if (!res.ok) throw error(res.status, 'Could not load pages.');
  const pages = (await res.json()) as PageListItem[];
  return { pages };
};
