import { env } from '$env/dynamic/private';

/** Server-to-server API base for SSR loads and admin proxying (never sent to the browser). */
export const INTERNAL_API_BASE = env.INTERNAL_API_BASE ?? 'http://localhost:5080';
