import type { RequestHandler } from './$types';

// Served at /robots.txt. Built dynamically so the Sitemap line points at whatever
// domain the site is running on (Render, a tunnel, localhost, …).
export const GET: RequestHandler = ({ url }) => {
  const body = `User-agent: *
Allow: /
# Keep the admin area out of search results.
Disallow: /admin

Sitemap: ${url.origin}/sitemap.xml
`;
  return new Response(body, {
    headers: {
      'Content-Type': 'text/plain; charset=utf-8',
      'Cache-Control': 'public, max-age=86400'
    }
  });
};
