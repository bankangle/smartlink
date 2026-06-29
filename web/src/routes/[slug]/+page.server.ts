import { error } from '@sveltejs/kit';
import { INTERNAL_API_BASE } from '$lib/server/config';
import type { PublicPage } from '$lib/types';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ params, url, fetch, setHeaders }) => {
  const res = await fetch(`${INTERNAL_API_BASE}/api/pages/${encodeURIComponent(params.slug)}`);

  if (res.status === 404) throw error(404, 'This smart link doesn’t exist (or isn’t published yet).');
  if (!res.ok) throw error(502, 'Could not load this page right now.');

  const page = (await res.json()) as PublicPage;

  // Let CDNs/browsers cache the public page briefly; clicks are tracked server-side.
  setHeaders({ 'cache-control': 'public, max-age=60' });

  const presave = url.searchParams.get('presave'); // ok | failed | cancelled | null
  return { page, presave };
};
