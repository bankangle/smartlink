import { INTERNAL_API_BASE } from '$lib/server/config';
import type { PublicPageSummary } from '$lib/types';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ fetch, setHeaders }) => {
  let artists: PublicPageSummary[] = [];
  try {
    const res = await fetch(`${INTERNAL_API_BASE}/api/pages`);
    if (res.ok) artists = (await res.json()) as PublicPageSummary[];
  } catch {
    // Landing page still renders even if the API is briefly unavailable.
  }

  setHeaders({ 'cache-control': 'public, max-age=60' });
  return { artists };
};
