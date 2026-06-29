<script lang="ts">
  import { PUBLIC_API_BASE } from '$lib/config';
  import { buildTheme } from '$lib/theme';
  import { platformMeta } from '$lib/platforms';
  import BrandIcon from '$lib/components/BrandIcon.svelte';
  import type { PageData } from './$types';

  let { data }: { data: PageData } = $props();
  const page = $derived(data.page);
  const theme = $derived(buildTheme(page.accentColor));

  // Deezer pre-save is hidden until Deezer reopens app registration (only demo
  // mode works right now). Spotify pre-save is live. Flip this to bring Deezer back.
  const DEEZER_PRESAVE_ENABLED = false;

  const redirect = (platform: string) =>
    `${PUBLIC_API_BASE}/api/r/${page.id}/${encodeURIComponent(platform)}`;

  // Returns the pre-save ("Add to Library") URL for a link, or null when the
  // link can't be pre-saved (no track id, or the provider is disabled).
  function presaveUrlFor(link: (typeof page.links)[number]): string | null {
    if (link.platform === 'spotify' && link.spotifyTrackId)
      return `${PUBLIC_API_BASE}/api/spotify/login?pageId=${page.id}&trackId=${link.spotifyTrackId}`;
    if (DEEZER_PRESAVE_ENABLED && link.platform === 'deezer' && link.deezerTrackId)
      return `${PUBLIC_API_BASE}/api/deezer/login?pageId=${page.id}&trackId=${link.deezerTrackId}`;
    return null;
  }

  // Pre-save result toast.
  let toast = $state<{ kind: 'ok' | 'failed' | 'cancelled'; msg: string } | null>(null);
  $effect(() => {
    const demo = data.presave?.endsWith('-demo');
    const kind = data.presave?.replace('-demo', '');
    if (kind === 'ok')
      toast = { kind: 'ok', msg: demo ? 'Pre-saved! (demo mode)' : 'Added to your library 🎧' };
    else if (kind === 'failed') toast = { kind: 'failed', msg: 'Couldn’t save — please try again.' };
    else if (kind === 'cancelled') toast = { kind: 'cancelled', msg: 'Pre-save cancelled.' };
    if (toast) {
      const t = setTimeout(() => (toast = null), 5000);
      return () => clearTimeout(t);
    }
  });

  const shareTitle = $derived(`${page.artistName} — ${page.title}`);

  // Share / copy link.
  let copied = $state(false);
  async function share() {
    const url = typeof location !== 'undefined' ? location.href : '';
    if (typeof navigator !== 'undefined' && navigator.share) {
      try {
        await navigator.share({ title: shareTitle, url });
      } catch {
        /* user dismissed */
      }
    } else if (typeof navigator !== 'undefined' && navigator.clipboard) {
      await navigator.clipboard.writeText(url);
      copied = true;
      setTimeout(() => (copied = false), 1800);
    }
  }
</script>

<svelte:head>
  <title>{shareTitle}</title>
  <meta name="description" content={`Listen to ${page.title} by ${page.artistName} on your favourite platform.`} />
  <meta property="og:type" content="music.song" />
  <meta property="og:title" content={shareTitle} />
  <meta property="og:description" content={`Stream or pre-save ${page.title} by ${page.artistName}.`} />
  {#if page.artworkUrl}<meta property="og:image" content={page.artworkUrl} />{/if}
  <meta name="twitter:card" content="summary_large_image" />
</svelte:head>

<main class="page" style="--accent: {theme.accent}; --accent-soft: {theme.accentSoft}; --on-accent: {theme.onAccent}; --tint: {theme.tint}; background: {theme.background};">
  <!-- Living background generated from the cover: a slow Ken-Burns blow-up of the
       artwork, plus three drifting glow blobs in the extracted palette. -->
  <div class="bg" aria-hidden="true">
    {#if page.artworkUrl}
      <div class="kb" style="background-image: url('{page.artworkUrl}');"></div>
    {/if}
    <div class="blob blob-1" style="background: {theme.mesh[0]};"></div>
    <div class="blob blob-2" style="background: {theme.mesh[1]};"></div>
    <div class="blob blob-3" style="background: {theme.mesh[2]};"></div>
    <div class="vignette"></div>
    <div class="grain"></div>
  </div>

  <!-- Top bar -->
  <header class="topbar">
    <button class="iconbtn" onclick={share} aria-label="Share this link">
      {#if copied}
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M20 6 9 17l-5-5" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/></svg>
      {:else}
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M18 8a3 3 0 1 0-2.83-4M6 12a3 3 0 1 0 0 .01M18 19a3 3 0 1 0-2.83-4M8.6 13.5l6.8 4M15.4 6.5l-6.8 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
      {/if}
    </button>
  </header>

  <div class="shell">
    <!-- Artwork -->
    <div class="art-wrap animate-pop-in">
      <div class="art-glow animate-glow-pulse" style="background: {theme.accent};"></div>
      {#if page.artworkUrl}
        <img class="art" src={page.artworkUrl} alt={`${page.title} cover art`} draggable="false" />
      {:else}
        <div class="art art-empty">🎵</div>
      {/if}
    </div>

    <!-- Title block -->
    <div class="titles animate-slide-up" style="animation-delay: 90ms;">
      <h1>{page.title}</h1>
      <p class="artist">{page.artistName}</p>
      {#if page.subtitle}
        <span class="tag"><span class="dot"></span>{page.subtitle}</span>
      {/if}
    </div>

    <!-- Platform links -->
    <ul class="links">
      {#each page.links as link, i (link.platform)}
        {@const meta = platformMeta(link.platform)}
        {@const presaveUrl = presaveUrlFor(link)}
        <li
          class="row animate-slide-up"
          style="--brand: {meta.color}; animation-delay: {130 + i * 60}ms;"
        >
          <a class="row-hit" href={redirect(link.platform)} target="_blank" rel="noopener" aria-label={`Open on ${meta.name}`}>
            <span class="chip"><BrandIcon platform={link.platform} size={22} /></span>
            <span class="row-name">{meta.name}</span>
          </a>

          <div class="row-actions">
            {#if presaveUrl}
              <a class="btn-presave" href={presaveUrl}>
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none"><path d="m12 21-1.45-1.32C5.4 15.03 2 11.94 2 8.13 2 5.1 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.1 22 8.13c0 3.81-3.4 6.9-8.55 11.55L12 21Z" fill="currentColor"/></svg>
                Pre-save
              </a>
            {/if}
            <a class="btn-play" href={redirect(link.platform)} target="_blank" rel="noopener">
              {link.actionLabel}
              <svg class="chev" width="15" height="15" viewBox="0 0 24 24" fill="none"><path d="m9 6 6 6-6 6" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"/></svg>
            </a>
          </div>
        </li>
      {/each}
    </ul>

    <!-- "Powered by SmartLink" hidden for now — re-add later.
    <footer class="foot animate-fade-in" style="animation-delay: 500ms;">
      <span class="brand-dot"></span>
      Powered by <strong>SmartLink</strong>
    </footer>
    -->
    <div style="height: 24px;"></div>
  </div>

  <!-- Toast -->
  {#if toast}
    <div class="toast toast-{toast.kind}" role="status">
      {#if toast.kind === 'ok'}
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M20 6 9 17l-5-5" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"/></svg>
      {/if}
      <span>{toast.msg}</span>
    </div>
  {/if}
</main>

<style>
  .page {
    position: relative;
    min-height: 100dvh;
    width: 100%;
    overflow: hidden;
    /* App-like feel: no text selection or long-press callouts on mobile. */
    user-select: none;
    -webkit-user-select: none;
    -webkit-touch-callout: none;
    -webkit-tap-highlight-color: transparent;
  }

  .bg {
    position: absolute;
    inset: 0;
    z-index: 0;
    overflow: hidden;
    pointer-events: none;
  }
  /* Ken-Burns blow-up of the cover: slowly zooms and drifts to "extend" the art. */
  .kb {
    position: absolute;
    inset: -16%;
    background-size: cover;
    background-position: center;
    filter: blur(64px) saturate(1.35);
    opacity: 0.3;
    transform-origin: center;
    will-change: transform;
    animation: kenburns 38s ease-in-out infinite alternate;
  }
  .blob {
    position: absolute;
    width: 75vw;
    max-width: 560px;
    aspect-ratio: 1;
    border-radius: 50%;
    filter: blur(80px);
    opacity: 0.55;
    mix-blend-mode: screen;
    will-change: transform;
  }
  .blob-1 { top: -14%; left: -16%; animation: drift1 22s ease-in-out infinite; }
  .blob-2 { bottom: -18%; right: -14%; opacity: 0.5; animation: drift2 28s ease-in-out infinite; }
  .blob-3 { top: 32%; right: -22%; opacity: 0.42; animation: drift3 34s ease-in-out infinite; }
  /* Keep content readable over the moving color. */
  .vignette {
    position: absolute;
    inset: 0;
    background:
      radial-gradient(120% 80% at 50% 30%, transparent 40%, rgba(10, 10, 11, 0.55) 100%),
      linear-gradient(to bottom, rgba(10, 10, 11, 0.2), transparent 25%, transparent 70%, rgba(10, 10, 11, 0.45));
  }
  .grain {
    position: absolute;
    inset: 0;
    opacity: 0.05;
    mix-blend-mode: overlay;
    background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='140' height='140'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='3' stitchTiles='stitch'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E");
  }

  @keyframes kenburns {
    0% { transform: scale(1.15) translate3d(0, 0, 0); }
    100% { transform: scale(1.4) translate3d(-3%, -4%, 0); }
  }
  @keyframes drift1 {
    0%, 100% { transform: translate3d(0, 0, 0) scale(1); }
    50% { transform: translate3d(8%, 10%, 0) scale(1.12); }
  }
  @keyframes drift2 {
    0%, 100% { transform: translate3d(0, 0, 0) scale(1.05); }
    50% { transform: translate3d(-10%, -7%, 0) scale(0.92); }
  }
  @keyframes drift3 {
    0%, 100% { transform: translate3d(0, 0, 0) scale(0.95); }
    50% { transform: translate3d(-7%, 9%, 0) scale(1.15); }
  }

  .topbar {
    position: relative;
    z-index: 3;
    display: flex;
    justify-content: flex-end;
    padding: 16px 16px 0;
    max-width: 30rem;
    margin: 0 auto;
  }
  .iconbtn {
    display: grid;
    place-items: center;
    height: 38px;
    width: 38px;
    border-radius: 999px;
    color: rgba(255, 255, 255, 0.8);
    background: rgba(255, 255, 255, 0.06);
    border: 1px solid rgba(255, 255, 255, 0.12);
    backdrop-filter: blur(8px);
    transition: transform 0.15s ease, background 0.2s ease, color 0.2s ease;
    cursor: pointer;
  }
  .iconbtn:hover { background: rgba(255, 255, 255, 0.12); color: #fff; }
  .iconbtn:active { transform: scale(0.92); }

  .shell {
    position: relative;
    z-index: 2;
    display: flex;
    flex-direction: column;
    align-items: center;
    width: 100%;
    max-width: 30rem;
    margin: 0 auto;
    padding: 18px 20px 36px;
  }

  .art-wrap {
    position: relative;
    width: 100%;
    max-width: 290px;
    margin-top: 14px;
  }
  .art-glow {
    position: absolute;
    inset: 8%;
    z-index: 0;
    border-radius: 24px;
    filter: blur(34px);
  }
  .art {
    position: relative;
    z-index: 1;
    width: 100%;
    aspect-ratio: 1;
    object-fit: cover;
    border-radius: 20px;
    box-shadow: 0 30px 60px -24px rgba(0, 0, 0, 0.85);
    outline: 1px solid rgba(255, 255, 255, 0.12);
    outline-offset: -1px;
  }
  .art-empty {
    display: grid;
    place-items: center;
    font-size: 3rem;
    background: rgba(255, 255, 255, 0.05);
  }

  .titles {
    text-align: center;
    margin-top: 22px;
  }
  .titles h1 {
    font-size: 1.6rem;
    line-height: 1.15;
    font-weight: 800;
    letter-spacing: -0.02em;
    color: #fff;
    margin: 0;
  }
  .artist {
    margin: 6px 0 0;
    font-size: 1rem;
    font-weight: 500;
    color: rgba(255, 255, 255, 0.66);
  }
  .tag {
    display: inline-flex;
    align-items: center;
    gap: 7px;
    margin-top: 12px;
    padding: 4px 12px;
    border-radius: 999px;
    font-size: 0.7rem;
    font-weight: 600;
    letter-spacing: 0.06em;
    text-transform: uppercase;
    color: rgba(255, 255, 255, 0.82);
    background: var(--tint);
    border: 1px solid rgba(255, 255, 255, 0.12);
  }
  .tag .dot {
    height: 6px;
    width: 6px;
    border-radius: 50%;
    background: var(--accent);
    box-shadow: 0 0 8px var(--accent);
  }

  .links {
    list-style: none;
    margin: 26px 0 0;
    padding: 0;
    width: 100%;
    display: flex;
    flex-direction: column;
    gap: 11px;
  }
  .row {
    position: relative;
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 8px 10px 8px 12px;
    border-radius: 16px;
    background: rgba(255, 255, 255, 0.045);
    border: 1px solid rgba(255, 255, 255, 0.09);
    backdrop-filter: blur(10px);
    overflow: hidden;
    transition: transform 0.22s cubic-bezier(0.16, 1, 0.3, 1), border-color 0.25s ease,
      box-shadow 0.25s ease, background 0.25s ease;
  }
  /* Brand glow that fades in on hover. */
  .row::before {
    content: '';
    position: absolute;
    inset: 0;
    z-index: 0;
    opacity: 0;
    background: radial-gradient(120% 140% at 0% 50%, color-mix(in srgb, var(--brand) 22%, transparent), transparent 60%);
    transition: opacity 0.3s ease;
  }
  .row:hover {
    transform: translateY(-2px);
    border-color: color-mix(in srgb, var(--brand) 55%, transparent);
    box-shadow: 0 14px 34px -18px color-mix(in srgb, var(--brand) 90%, transparent);
    background: rgba(255, 255, 255, 0.06);
  }
  .row:hover::before { opacity: 1; }
  .row:active { transform: translateY(0) scale(0.992); }

  .row-hit {
    position: relative;
    z-index: 1;
    display: flex;
    align-items: center;
    gap: 13px;
    flex: 1;
    min-width: 0;
    min-height: 46px;
    text-decoration: none;
  }
  .chip {
    display: grid;
    place-items: center;
    height: 42px;
    width: 42px;
    flex-shrink: 0;
    border-radius: 12px;
    background: color-mix(in srgb, var(--brand) 16%, transparent);
    border: 1px solid color-mix(in srgb, var(--brand) 28%, transparent);
    transition: transform 0.22s ease;
  }
  .row:hover .chip { transform: scale(1.06); }
  .row-name {
    font-size: 0.95rem;
    font-weight: 600;
    color: #fff;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .row-actions {
    position: relative;
    z-index: 1;
    display: flex;
    align-items: center;
    gap: 7px;
    flex-shrink: 0;
  }

  .btn-play {
    display: inline-flex;
    align-items: center;
    gap: 2px;
    padding: 8px 12px 8px 14px;
    border-radius: 999px;
    font-size: 0.8rem;
    font-weight: 700;
    color: rgba(255, 255, 255, 0.92);
    background: rgba(255, 255, 255, 0.07);
    border: 1px solid rgba(255, 255, 255, 0.12);
    text-decoration: none;
    transition: background 0.22s ease, color 0.22s ease, border-color 0.22s ease, transform 0.15s ease;
  }
  .btn-play .chev { opacity: 0.5; transition: transform 0.22s ease, opacity 0.22s ease; }
  .row:hover .btn-play {
    background: var(--brand);
    border-color: color-mix(in srgb, var(--brand) 80%, white 20%);
    color: #fff;
  }
  .row:hover .btn-play .chev { opacity: 1; transform: translateX(2px); }
  .btn-play:active { transform: scale(0.95); }

  .btn-presave {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 9px 14px;
    border-radius: 999px;
    font-size: 0.8rem;
    font-weight: 700;
    color: var(--on-accent);
    text-decoration: none;
    background: linear-gradient(
      100deg,
      var(--accent) 0%,
      color-mix(in srgb, var(--accent) 60%, white 40%) 50%,
      var(--accent) 100%
    );
    background-size: 200% 100%;
    box-shadow: 0 6px 18px -6px var(--accent);
    transition: transform 0.15s ease, box-shadow 0.22s ease;
  }
  .btn-presave:hover { box-shadow: 0 10px 24px -6px var(--accent); }
  .btn-presave:active { transform: scale(0.95); }

  .foot {
    display: flex;
    align-items: center;
    gap: 7px;
    margin-top: 30px;
    font-size: 0.72rem;
    color: rgba(255, 255, 255, 0.32);
  }
  .foot strong { color: rgba(255, 255, 255, 0.6); font-weight: 700; }
  .brand-dot {
    height: 7px;
    width: 7px;
    border-radius: 50%;
    background: var(--accent);
    box-shadow: 0 0 10px var(--accent);
  }

  .toast {
    position: fixed;
    inset-inline: 0;
    bottom: 22px;
    z-index: 50;
    margin: 0 auto;
    width: 90%;
    max-width: 22rem;
    display: flex;
    align-items: center;
    gap: 9px;
    padding: 13px 16px;
    border-radius: 14px;
    font-size: 0.88rem;
    font-weight: 600;
    color: #fff;
    box-shadow: 0 18px 40px -12px rgba(0, 0, 0, 0.6);
    border: 1px solid rgba(255, 255, 255, 0.16);
    backdrop-filter: blur(10px);
    animation: slideUp 0.45s cubic-bezier(0.16, 1, 0.3, 1) both;
  }
  .toast-ok { background: linear-gradient(120deg, #059669, #10b981); }
  .toast-failed { background: linear-gradient(120deg, #e11d48, #f43f5e); }
  .toast-cancelled { background: rgba(63, 63, 70, 0.92); }

  @keyframes slideUp {
    from { opacity: 0; transform: translateY(14px); }
    to { opacity: 1; transform: translateY(0); }
  }

  @media (prefers-reduced-motion: reduce) {
    .kb,
    .blob,
    .animate-glow-pulse { animation: none !important; }
  }
</style>
