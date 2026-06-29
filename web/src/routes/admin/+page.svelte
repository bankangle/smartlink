<script lang="ts">
  import { page } from '$app/state';
  import type { PageData } from './$types';
  let { data }: { data: PageData } = $props();
  const pending = $derived(page.data.pending as boolean | undefined);
</script>

<svelte:head><title>Pages · Coasthill IV</title></svelte:head>

{#if pending}
  <div class="mb-5 flex items-start gap-3 rounded-xl border border-amber-500/30 bg-amber-500/10 px-4 py-3 text-sm text-amber-200">
    <span class="mt-0.5 text-base">⏳</span>
    <div>
      <p class="font-semibold">Your account is awaiting approval.</p>
      <p class="mt-0.5 text-amber-200/80">You can build your pages now, but they stay hidden from the public roster until a moderator approves your account.</p>
    </div>
  </div>
{/if}

<div class="mb-6 flex items-center justify-between">
  <div>
    <h1 class="text-2xl font-bold text-white">Your pages</h1>
    <p class="mt-0.5 text-sm text-white/50">{data.pages.length} smart link{data.pages.length === 1 ? '' : 's'}</p>
  </div>
  <a
    href="/admin/pages/new"
    class="rounded-lg bg-white px-4 py-2 text-sm font-semibold text-black transition hover:bg-white/90 active:scale-95"
  >
    + New page
  </a>
</div>

{#if data.pages.length === 0}
  <div class="rounded-2xl border border-dashed border-white/15 px-6 py-16 text-center">
    <div class="mb-3 text-4xl">🎵</div>
    <h2 class="text-lg font-semibold text-white">No pages yet</h2>
    <p class="mx-auto mt-1 max-w-xs text-sm text-white/50">
      Create your first smart link — paste a Spotify or Apple Music URL and we’ll find the rest.
    </p>
    <a
      href="/admin/pages/new"
      class="mt-5 inline-block rounded-lg bg-white px-4 py-2 text-sm font-semibold text-black transition hover:bg-white/90"
    >
      + New page
    </a>
  </div>
{:else}
  <div class="grid gap-3 sm:grid-cols-2">
    {#each data.pages as p (p.id)}
      <div class="group flex items-center gap-3 rounded-xl border border-white/10 bg-white/[0.03] p-3 transition hover:border-white/20">
        {#if p.artworkUrl}
          <img src={p.artworkUrl} alt="" class="h-14 w-14 shrink-0 rounded-lg object-cover ring-1 ring-white/10" />
        {:else}
          <div class="grid h-14 w-14 shrink-0 place-items-center rounded-lg bg-white/5 text-xl">🎵</div>
        {/if}
        <div class="min-w-0 flex-1">
          <div class="flex items-center gap-2">
            <h3 class="truncate font-semibold text-white">{p.title}</h3>
            {#if !p.isPublished}
              <span class="shrink-0 rounded-full bg-amber-500/15 px-2 py-0.5 text-[10px] font-semibold text-amber-300">Draft</span>
            {/if}
          </div>
          <p class="truncate text-sm text-white/50">{p.artistName}</p>
          <div class="mt-1 flex items-center gap-3 text-xs text-white/40">
            <span>{p.totalClicks} click{p.totalClicks === 1 ? '' : 's'}</span>
            <span>·</span>
            <span>/{p.slug}</span>
          </div>
        </div>
        <div class="flex shrink-0 flex-col items-end gap-1.5">
          <a
            href="/admin/pages/{p.id}"
            class="rounded-lg border border-white/10 px-3 py-1.5 text-xs font-medium text-white/80 transition hover:bg-white/5"
          >
            Edit
          </a>
          <a
            href="/{p.slug}"
            target="_blank"
            rel="noopener"
            class="text-xs text-white/40 transition hover:text-white/70"
          >
            View ↗
          </a>
        </div>
      </div>
    {/each}
  </div>
{/if}
