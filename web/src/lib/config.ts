import { env } from '$env/dynamic/public';

/**
 * Browser-reachable API base, used for outbound redirect links and the Deezer
 * pre-save bounce. Empty string means "same origin" (production behind Caddy).
 * Defaults to the local API port for dev.
 */
export const PUBLIC_API_BASE =
  env.PUBLIC_API_BASE !== undefined ? env.PUBLIC_API_BASE : 'http://localhost:5080';
