<script lang="ts">
  import { buildTheme } from '$lib/theme';
  import type { PageData } from './$types';

  let { data }: { data: PageData } = $props();
  const artists = $derived(data.artists ?? []);
  const year = new Date().getFullYear();

  // Covers for the drifting background wall — repeated so the marquee never runs dry.
  const covers = $derived(artists.map((a) => a.artworkUrl).filter((u): u is string => !!u));
  const wall = $derived([...covers, ...covers, ...covers].slice(0, Math.max(covers.length * 2, 12)));

  const stats = $derived([
    '3B+ streams',
    '#1 Billboard Electronic',
    'FEEL · 183M TikTok views',
    `${artists.length} artists`
  ]);
</script>

<svelte:head>
  <title>Coasthill IV — the roster</title>
  <meta name="description" content="One smart link per release — every streaming platform, plus per-platform click analytics." />
</svelte:head>

<main class="relative min-h-[100dvh] overflow-hidden bg-[#070708] text-white">
  <!-- artist entry — the only way in; the moderator just remembers /admin -->
  <a
    href="/admin/login"
    class="absolute right-4 top-4 z-30 rounded-full border border-white/15 bg-white/5 px-4 py-1.5 text-xs font-semibold text-white/80 backdrop-blur transition hover:border-white/30 hover:text-white"
  >
    I’m an artist →
  </a>

  <!-- ===== HERO ===== -->
  <section class="relative isolate overflow-hidden">
    <!-- drifting wall of real covers -->
    <div class="cover-wall pointer-events-none absolute inset-0 -z-10" aria-hidden="true">
      <div class="wall-row wall-a">
        {#each wall as src}<div class="tile" style="background-image:url('{src}')"></div>{/each}
      </div>
      <div class="wall-row wall-b">
        {#each wall as src}<div class="tile" style="background-image:url('{src}')"></div>{/each}
      </div>
      <div class="wall-row wall-a">
        {#each wall as src}<div class="tile" style="background-image:url('{src}')"></div>{/each}
      </div>
    </div>

    <!-- dark wash + bottom fade -->
    <div class="pointer-events-none absolute inset-0 -z-10 bg-[#070708]/72"></div>
    <div class="pointer-events-none absolute inset-x-0 bottom-0 -z-10 h-64 bg-gradient-to-t from-[#070708] to-transparent"></div>

    <div class="mx-auto max-w-6xl px-5 pb-10 pt-24 sm:px-8 sm:pt-32">
      <div class="mx-auto max-w-3xl text-center">
        <h1 class="text-balance text-5xl font-black leading-[0.95] tracking-tight sm:text-7xl">
          <span class="block animate-slide-up">Every artist.</span>
          <span class="grad-text block animate-slide-up" style="animation-delay:90ms">One smart link.</span>
        </h1>

        <p class="mx-auto mt-5 max-w-md animate-slide-up text-base text-white/65" style="animation-delay:160ms">
          A landing page for every release — all the streaming platforms in one place,
          with per-platform click analytics. Tap an artist below.
        </p>
      </div>

      <!-- name marquee -->
      {#if artists.length}
        <div class="marquee relative mt-12 flex overflow-hidden [mask-image:linear-gradient(90deg,transparent,#000_12%,#000_88%,transparent)]">
          <div class="marquee-track flex shrink-0 items-center gap-10 pr-10">
            {#each [...artists, ...artists] as a}
              <a href="/{a.slug}" class="whitespace-nowrap text-2xl font-bold text-white/35 transition hover:text-white sm:text-3xl">
                {a.artistName}
              </a>
            {/each}
          </div>
        </div>
      {/if}

      <!-- stats ticker -->
      <div class="mt-8 flex flex-wrap items-center justify-center gap-x-3 gap-y-2 text-center text-xs text-white/40">
        {#each stats as s, i}
          {#if i > 0}<span class="text-white/20">•</span>{/if}
          <span class="font-medium text-white/55">{s}</span>
        {/each}
      </div>
    </div>
  </section>

  <!-- ===== GALLERY ===== -->
  <section class="relative mx-auto max-w-6xl px-5 pb-24 sm:px-8">
    {#if artists.length}
      <div class="mb-6 flex items-end justify-between border-b border-white/[0.06] pb-3">
        <h2 class="text-lg font-bold tracking-tight text-white/90">The roster</h2>
        <span class="text-xs text-white/40">{artists.length} artists</span>
      </div>

      <div class="grid grid-cols-2 gap-3 sm:grid-cols-3 sm:gap-4 lg:grid-cols-4">
        {#each artists as a, i (a.slug)}
          {@const theme = buildTheme(a.accentColor)}
          <a
            href="/{a.slug}"
            class="card group relative flex animate-pop-in flex-col overflow-hidden rounded-2xl border border-white/10 bg-white/[0.03] p-3"
            style="animation-delay:{Math.min(i * 55, 600)}ms; --glow:{theme.accent};"
          >
            <div class="pointer-events-none absolute -inset-px rounded-2xl opacity-0 transition-opacity duration-300 group-hover:opacity-100" style="box-shadow:0 0 0 1px var(--glow), 0 14px 50px -12px var(--glow);"></div>

            <div class="relative aspect-square w-full overflow-hidden rounded-xl ring-1 ring-white/10">
              {#if a.artworkUrl}
                <img
                  src={a.artworkUrl}
                  alt="{a.artistName} — {a.title}"
                  loading="lazy"
                  class="h-full w-full object-cover transition duration-700 ease-out group-hover:scale-110"
                />
              {:else}
                <div class="grid h-full w-full place-items-center text-4xl" style="background:{theme.background}">♪</div>
              {/if}
              <div class="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/55 via-transparent to-transparent"></div>
              <span class="absolute left-2 top-2 translate-y-1 rounded-full px-2 py-0.5 text-[10px] font-bold text-white opacity-0 backdrop-blur-sm transition-all duration-300 group-hover:translate-y-0 group-hover:opacity-100" style="background:{theme.accent}">
                Open ↗
              </span>
              <span class="absolute bottom-2 right-2 rounded-full bg-black/55 px-2 py-0.5 text-[10px] font-semibold text-white/90 backdrop-blur-sm">
                {a.linkCount} links
              </span>
            </div>

            <div class="relative mt-3 px-0.5 pb-1">
              <p class="truncate text-sm font-bold leading-tight">{a.artistName}</p>
              <p class="truncate text-xs text-white/55">{a.title}</p>
            </div>
          </a>
        {/each}
      </div>
    {:else}
      <div class="mt-16 rounded-2xl border border-white/10 bg-white/[0.03] p-10 text-center">
        <p class="text-white/60">No published artists yet.</p>
      </div>
    {/if}
  </section>

  <footer class="relative mx-auto max-w-6xl px-5 pb-10 text-center text-xs text-white/30 sm:px-8">
    {year}
  </footer>
</main>

<style>
  /* animated gradient headline */
  .grad-text {
    background: linear-gradient(100deg, #a78bfa, #f0abfc, #7dd3fc, #a78bfa);
    background-size: 300% 100%;
    -webkit-background-clip: text;
    background-clip: text;
    color: transparent;
    animation: grad-pan 6s ease-in-out infinite;
  }
  @keyframes grad-pan {
    0%, 100% { background-position: 0% 50%; }
    50% { background-position: 100% 50%; }
  }

  /* drifting cover wall */
  .cover-wall {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
    transform: rotate(-8deg) scale(1.4);
    transform-origin: center;
    filter: blur(2px) saturate(1.1);
    opacity: 0.55;
  }
  .wall-row { display: flex; gap: 0.75rem; width: max-content; }
  .wall-a { animation: wall-left 60s linear infinite; }
  .wall-b { animation: wall-right 75s linear infinite; }
  .tile {
    width: 8.5rem;
    height: 8.5rem;
    flex: 0 0 auto;
    border-radius: 0.9rem;
    background-size: cover;
    background-position: center;
  }
  @keyframes wall-left { from { transform: translateX(0); } to { transform: translateX(-50%); } }
  @keyframes wall-right { from { transform: translateX(-50%); } to { transform: translateX(0); } }

  /* artist-name marquee */
  .marquee-track { animation: marquee 32s linear infinite; }
  .marquee:hover .marquee-track { animation-play-state: paused; }
  @keyframes marquee { from { transform: translateX(0); } to { transform: translateX(-50%); } }

  @media (prefers-reduced-motion: reduce) {
    .grad-text, .wall-a, .wall-b, .marquee-track { animation: none !important; }
    .grad-text { background-position: 0% 50%; }
  }
</style>
