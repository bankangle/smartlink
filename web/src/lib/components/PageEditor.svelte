<script lang="ts">
  import { enhance } from '$app/forms';
  import { ORDER, platformMeta } from '$lib/platforms';
  import { buildTheme } from '$lib/theme';
  import { slugify } from '$lib/slug';
  import BrandIcon from './BrandIcon.svelte';
  import Spinner from './Spinner.svelte';
  import type { AdminPage, LinkInput, ResolveResult } from '$lib/types';

  let {
    initial,
    mode,
    formError = null
  }: { initial: Partial<AdminPage>; mode: 'new' | 'edit'; formError?: string | null } = $props();

  // ---- form state ----
  let sourceUrl = $state(initial.sourceUrl ?? '');
  let slug = $state(initial.slug ?? '');
  let artistName = $state(initial.artistName ?? '');
  let title = $state(initial.title ?? '');
  let subtitle = $state(initial.subtitle ?? '');
  let artworkUrl = $state(initial.artworkUrl ?? '');
  let accentColor = $state(initial.accentColor ?? '#5b6ef5');
  let isPublished = $state(initial.isPublished ?? true);
  let links = $state<LinkInput[]>(
    (initial.links ?? []).map((l) => ({ ...l }))
  );

  let resolving = $state(false);
  let resolveError = $state<string | null>(null);
  let submitting = $state(false);
  let slugTouched = $state(mode === 'edit');

  const theme = $derived(buildTheme(accentColor));
  const enabledLinks = $derived(links.filter((l) => l.isEnabled && l.url));

  // platforms not yet present, for the "add link" picker
  const availablePlatforms = $derived(ORDER.filter((p) => !links.some((l) => l.platform === p)));
  let addPlatform = $state('');

  function maybeAutoSlug() {
    // The public URL is the artist's name (e.g. /the-killers). Editable below.
    if (!slugTouched && artistName) {
      slug = slugify(artistName);
    }
  }

  async function resolve() {
    if (!sourceUrl.trim()) return;
    resolving = true;
    resolveError = null;
    try {
      const res = await fetch('/admin/resolve', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ url: sourceUrl.trim() })
      });
      if (!res.ok) {
        resolveError = (await res.text()) || 'Could not resolve that link.';
        return;
      }
      const data = (await res.json()) as ResolveResult;
      if (data.artistName) artistName = data.artistName;
      if (data.title) title = data.title;
      if (data.artworkUrl) artworkUrl = data.artworkUrl;
      if (data.accentColor) accentColor = data.accentColor;

      // Merge resolved links, preserving any manual ones already present.
      const resolved: LinkInput[] = data.links.map((l, i) => ({
        platform: l.platform,
        url: l.url,
        actionLabel: platformMeta(l.platform).name ? defaultAction(l.platform) : 'Play',
        sortOrder: i * 10,
        isEnabled: true,
        deezerTrackId: l.deezerTrackId,
        spotifyTrackId: l.spotifyTrackId
      }));
      // keep existing manual links not in resolved set
      const extra = links.filter((l) => !resolved.some((r) => r.platform === l.platform));
      links = [...resolved, ...extra].map((l, i) => ({ ...l, sortOrder: i * 10 }));

      maybeAutoSlug();
    } catch (e) {
      resolveError = 'Network error while resolving.';
    } finally {
      resolving = false;
    }
  }

  function defaultAction(platform: string): string {
    if (platform === 'itunes') return 'Download';
    if (platform === 'amazonStore') return 'Buy';
    if (platform === 'youtube') return 'Watch';
    return 'Play';
  }

  function move(i: number, dir: -1 | 1) {
    const j = i + dir;
    if (j < 0 || j >= links.length) return;
    const next = [...links];
    [next[i], next[j]] = [next[j], next[i]];
    links = next.map((l, k) => ({ ...l, sortOrder: k * 10 }));
  }

  function remove(i: number) {
    links = links.filter((_, k) => k !== i).map((l, k) => ({ ...l, sortOrder: k * 10 }));
  }

  function addLink() {
    if (!addPlatform) return;
    links = [
      ...links,
      {
        platform: addPlatform,
        url: '',
        actionLabel: defaultAction(addPlatform),
        sortOrder: links.length * 10,
        isEnabled: true,
        deezerTrackId: null,
        spotifyTrackId: null
      }
    ];
    addPlatform = '';
  }

  // Serialized payload sent to the server action.
  const payload = $derived(
    JSON.stringify({
      slug: slug.trim(),
      artistName: artistName.trim(),
      title: title.trim(),
      subtitle: subtitle.trim() || null,
      artworkUrl: artworkUrl.trim() || null,
      accentColor: accentColor || null,
      sourceUrl: sourceUrl.trim() || null,
      isPublished,
      links: links
        .filter((l) => l.url.trim())
        .map((l, i) => ({
          platform: l.platform,
          url: l.url.trim(),
          actionLabel: l.actionLabel || 'Play',
          sortOrder: i * 10,
          isEnabled: l.isEnabled,
          deezerTrackId: l.deezerTrackId || null,
          spotifyTrackId: l.spotifyTrackId || null
        }))
    })
  );

  const canSave = $derived(slug.trim() && artistName.trim() && title.trim());
</script>

<form
  method="POST"
  action="?/save"
  use:enhance={() => {
    submitting = true;
    return async ({ update }) => {
      await update();
      submitting = false;
    };
  }}
  class="grid gap-6 lg:grid-cols-[1fr_320px]"
>
  <input type="hidden" name="payload" value={payload} />

  <!-- LEFT: form -->
  <div class="space-y-6">
    {#if formError}
      <div class="rounded-lg border border-rose-500/30 bg-rose-500/10 px-3 py-2 text-sm text-rose-200">{formError}</div>
    {/if}

    <!-- Source / resolve -->
    <section class="rounded-2xl border border-white/10 bg-white/[0.03] p-4">
      <h2 class="mb-1 text-sm font-semibold text-white">Auto-fill from a link</h2>
      <p class="mb-3 text-xs text-white/50">Paste a Spotify, Apple Music or other DSP URL — we’ll find every platform.</p>
      <div class="flex gap-2">
        <input
          bind:value={sourceUrl}
          placeholder="https://open.spotify.com/track/…"
          class="min-w-0 flex-1 rounded-lg border border-white/10 bg-black/30 px-3 py-2 text-sm text-white outline-none focus:border-indigo-400/60"
        />
        <button
          type="button"
          onclick={resolve}
          disabled={resolving || !sourceUrl.trim()}
          class="flex shrink-0 items-center gap-1.5 rounded-lg bg-indigo-500 px-3.5 py-2 text-sm font-semibold text-white transition hover:bg-indigo-400 disabled:opacity-50"
        >
          {#if resolving}<Spinner size={15} />{/if}
          Resolve
        </button>
      </div>
      {#if resolveError}
        <p class="mt-2 text-xs text-rose-300">{resolveError}</p>
      {/if}
    </section>

    <!-- Details -->
    <section class="space-y-3 rounded-2xl border border-white/10 bg-white/[0.03] p-4">
      <h2 class="text-sm font-semibold text-white">Details</h2>
      <div class="grid gap-3 sm:grid-cols-2">
        <label class="block">
          <span class="mb-1 block text-xs font-medium text-white/60">Artist</span>
          <input bind:value={artistName} oninput={maybeAutoSlug}
            class="w-full rounded-lg border border-white/10 bg-black/30 px-3 py-2 text-sm text-white outline-none focus:border-indigo-400/60" />
        </label>
        <label class="block">
          <span class="mb-1 block text-xs font-medium text-white/60">Title</span>
          <input bind:value={title} oninput={maybeAutoSlug}
            class="w-full rounded-lg border border-white/10 bg-black/30 px-3 py-2 text-sm text-white outline-none focus:border-indigo-400/60" />
        </label>
      </div>
      <label class="block">
        <span class="mb-1 block text-xs font-medium text-white/60">Subtitle <span class="text-white/30">(optional)</span></span>
        <input bind:value={subtitle} placeholder="Out now"
          class="w-full rounded-lg border border-white/10 bg-black/30 px-3 py-2 text-sm text-white outline-none focus:border-indigo-400/60" />
      </label>
      <div class="grid gap-3 sm:grid-cols-[1fr_auto]">
        <label class="block">
          <span class="mb-1 block text-xs font-medium text-white/60">URL slug</span>
          <div class="flex items-center rounded-lg border border-white/10 bg-black/30 px-3 focus-within:border-indigo-400/60">
            <span class="text-sm text-white/30">/</span>
            <input bind:value={slug} oninput={() => (slugTouched = true)}
              class="w-full bg-transparent px-1 py-2 text-sm text-white outline-none" />
          </div>
        </label>
        <label class="block">
          <span class="mb-1 block text-xs font-medium text-white/60">Accent</span>
          <input type="color" bind:value={accentColor}
            class="h-[38px] w-full cursor-pointer rounded-lg border border-white/10 bg-black/30" />
        </label>
      </div>
      <label class="block">
        <span class="mb-1 block text-xs font-medium text-white/60">Artwork URL</span>
        <input bind:value={artworkUrl} placeholder="https://…/cover.jpg"
          class="w-full rounded-lg border border-white/10 bg-black/30 px-3 py-2 text-sm text-white outline-none focus:border-indigo-400/60" />
      </label>
      <label class="flex items-center gap-2 pt-1">
        <input type="checkbox" bind:checked={isPublished} class="h-4 w-4 rounded accent-indigo-500" />
        <span class="text-sm text-white/80">Published (visible to the public)</span>
      </label>
    </section>

    <!-- Links -->
    <section class="rounded-2xl border border-white/10 bg-white/[0.03] p-4">
      <div class="mb-3 flex items-center justify-between">
        <h2 class="text-sm font-semibold text-white">Platforms</h2>
        <span class="text-xs text-white/40">{enabledLinks.length} active</span>
      </div>

      {#if links.length === 0}
        <p class="py-4 text-center text-sm text-white/40">No links yet — resolve a URL or add one below.</p>
      {/if}

      <div class="space-y-2">
        {#each links as link, i (link.platform)}
          {@const meta = platformMeta(link.platform)}
          <div class="rounded-xl border border-white/10 bg-black/20 p-3" class:opacity-50={!link.isEnabled}>
            <div class="flex items-center gap-2">
              <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg" style="background: {meta.color}1a;">
                <BrandIcon platform={link.platform} size={18} />
              </span>
              <span class="flex-1 text-sm font-semibold text-white">{meta.name}</span>
              <div class="flex items-center gap-1">
                <button type="button" onclick={() => move(i, -1)} disabled={i === 0}
                  class="rounded p-1 text-white/40 hover:bg-white/10 hover:text-white disabled:opacity-30" aria-label="Move up">↑</button>
                <button type="button" onclick={() => move(i, 1)} disabled={i === links.length - 1}
                  class="rounded p-1 text-white/40 hover:bg-white/10 hover:text-white disabled:opacity-30" aria-label="Move down">↓</button>
                <button type="button" onclick={() => remove(i)}
                  class="rounded p-1 text-rose-300/70 hover:bg-rose-500/10 hover:text-rose-300" aria-label="Remove">✕</button>
              </div>
            </div>
            <div class="mt-2 flex flex-wrap items-center gap-2">
              <input bind:value={link.url} placeholder="https://…"
                class="min-w-0 flex-1 rounded-lg border border-white/10 bg-black/30 px-2.5 py-1.5 text-xs text-white outline-none focus:border-indigo-400/60" />
              <input bind:value={link.actionLabel} placeholder="Play"
                class="w-20 rounded-lg border border-white/10 bg-black/30 px-2.5 py-1.5 text-xs text-white outline-none focus:border-indigo-400/60" />
              <label class="flex items-center gap-1.5 text-xs text-white/60">
                <input type="checkbox" bind:checked={link.isEnabled} class="h-3.5 w-3.5 rounded accent-indigo-500" />
                On
              </label>
            </div>
            {#if link.platform === 'deezer'}
              <div class="mt-2 flex items-center gap-2">
                <span class="text-[11px] font-medium text-fuchsia-300/80">Deezer track ID (enables pre-save)</span>
                <input bind:value={link.deezerTrackId} placeholder="e.g. 953097"
                  class="w-32 rounded-lg border border-white/10 bg-black/30 px-2.5 py-1 text-xs text-white outline-none focus:border-fuchsia-400/60" />
              </div>
            {/if}
            {#if link.platform === 'spotify'}
              <div class="mt-2 flex items-center gap-2">
                <span class="text-[11px] font-medium text-emerald-300/80">Spotify track ID (enables pre-save)</span>
                <input bind:value={link.spotifyTrackId} placeholder="e.g. 4uLU6hMCjMI75M1A2tKUQC"
                  class="w-48 rounded-lg border border-white/10 bg-black/30 px-2.5 py-1 text-xs text-white outline-none focus:border-emerald-400/60" />
              </div>
            {/if}
          </div>
        {/each}
      </div>

      {#if availablePlatforms.length}
        <div class="mt-3 flex gap-2">
          <select bind:value={addPlatform}
            class="flex-1 rounded-lg border border-white/10 bg-black/30 px-3 py-2 text-sm text-white outline-none focus:border-indigo-400/60">
            <option value="">Add a platform…</option>
            {#each availablePlatforms as p}
              <option value={p}>{platformMeta(p).name}</option>
            {/each}
          </select>
          <button type="button" onclick={addLink} disabled={!addPlatform}
            class="rounded-lg border border-white/15 px-3 py-2 text-sm font-medium text-white/80 transition hover:bg-white/5 disabled:opacity-40">Add</button>
        </div>
      {/if}
    </section>

    <div class="flex items-center gap-3">
      <button disabled={!canSave || submitting}
        class="flex items-center gap-2 rounded-lg bg-white px-5 py-2.5 text-sm font-semibold text-black transition hover:bg-white/90 active:scale-[0.99] disabled:opacity-50">
        {#if submitting}<Spinner size={16} />{/if}
        {mode === 'new' ? 'Create page' : 'Save changes'}
      </button>
      <a href="/admin" class="text-sm text-white/50 transition hover:text-white/80">Cancel</a>
    </div>
  </div>

  <!-- RIGHT: live preview -->
  <div class="lg:sticky lg:top-20 lg:self-start">
    <p class="mb-2 text-xs font-medium uppercase tracking-wide text-white/40">Live preview</p>
    <div class="overflow-hidden rounded-2xl border border-white/10">
      <div class="flex flex-col items-center px-5 py-8" style="background: {theme.background};">
        {#if artworkUrl}
          <img src={artworkUrl} alt="" class="aspect-square w-32 rounded-xl object-cover shadow-xl ring-1 ring-white/10" />
        {:else}
          <div class="grid aspect-square w-32 place-items-center rounded-xl bg-white/5 text-3xl ring-1 ring-white/10">🎵</div>
        {/if}
        <h3 class="mt-4 text-center text-base font-bold text-white">{title || 'Track title'}</h3>
        <p class="text-sm text-white/70">{artistName || 'Artist name'}</p>
        {#if subtitle}<p class="mt-0.5 text-xs text-white/50">{subtitle}</p>{/if}
        <div class="mt-4 w-full space-y-1.5">
          {#each enabledLinks.slice(0, 5) as l (l.platform)}
            {@const meta = platformMeta(l.platform)}
            <div class="flex items-center gap-2 rounded-lg border border-white/10 bg-white/[0.04] px-2.5 py-1.5">
              <BrandIcon platform={l.platform} size={15} />
              <span class="flex-1 text-xs font-medium text-white">{meta.name}</span>
              <span class="rounded-full px-2 py-0.5 text-[10px] font-semibold text-white/90" style="background: {theme.tint};">{l.actionLabel}</span>
            </div>
          {/each}
          {#if enabledLinks.length > 5}
            <p class="pt-1 text-center text-[10px] text-white/40">+{enabledLinks.length - 5} more</p>
          {/if}
        </div>
      </div>
    </div>
  </div>
</form>
