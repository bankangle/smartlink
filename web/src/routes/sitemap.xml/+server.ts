import { INTERNAL_API_BASE } from '$lib/server/config';
import type { PublicPageSummary } from '$lib/types';
import type { RequestHandler } from './$types';

// Served at /sitemap.xml. Lists the landing page plus every published artist page
// so search engines and link unfurlers can discover the whole roster.
export const GET: RequestHandler = async ({ url, fetch }) => {
  let slugs: string[] = [];
  try {
    const res = await fetch(`${INTERNAL_API_BASE}/api/pages`);
    if (res.ok) {
      const pages = (await res.json()) as PublicPageSummary[];
      slugs = pages.map((p) => p.slug);
    }
  } catch {
    // Sitemap still lists the home page even if the API is briefly unavailable.
  }

  const escape = (s: string) =>
    s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

  const locs = ['', ...slugs]
    .map((path) => `  <url><loc>${escape(`${url.origin}/${path}`)}</loc></url>`)
    .join('\n');

  const xml = `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${locs}
</urlset>`;

  return new Response(xml, {
    headers: {
      'Content-Type': 'application/xml; charset=utf-8',
      'Cache-Control': 'public, max-age=3600'
    }
  });
};
