<script lang="ts">
  import { enhance } from '$app/forms';
  import type { PageData } from './$types';
  let { data }: { data: PageData } = $props();

  const pending = $derived(data.artists.filter((a) => !a.isApproved));
  const approved = $derived(data.artists.filter((a) => a.isApproved));
</script>

<svelte:head><title>Artists · Coasthill IV</title></svelte:head>

<div class="mb-6">
  <h1 class="text-2xl font-bold text-white">Artists</h1>
  <p class="mt-0.5 text-sm text-white/50">Approve registered artists to make their pages public.</p>
</div>

<!-- Pending approval -->
<section class="mb-8">
  <h2 class="mb-3 flex items-center gap-2 text-sm font-semibold text-white/90">
    Pending approval
    {#if pending.length}
      <span class="rounded-full bg-amber-500/15 px-2 py-0.5 text-[11px] font-semibold text-amber-300">{pending.length}</span>
    {/if}
  </h2>

  {#if pending.length === 0}
    <p class="rounded-xl border border-white/10 bg-white/[0.02] px-4 py-6 text-center text-sm text-white/40">
      Nothing waiting — you’re all caught up.
    </p>
  {:else}
    <div class="space-y-2">
      {#each pending as a (a.id)}
        <div class="flex items-center gap-3 rounded-xl border border-amber-500/20 bg-amber-500/[0.04] p-3">
          <div class="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-white/10 text-sm font-bold uppercase text-white/80">
            {a.username.slice(0, 2)}
          </div>
          <div class="min-w-0 flex-1">
            <p class="truncate font-semibold text-white">{a.username}</p>
            <p class="text-xs text-white/45">{a.pageCount} page{a.pageCount === 1 ? '' : 's'} · joined {new Date(a.createdAt).toLocaleDateString()}</p>
          </div>
          <form method="POST" action="?/approve" use:enhance>
            <input type="hidden" name="id" value={a.id} />
            <button class="rounded-lg bg-white px-3.5 py-1.5 text-xs font-semibold text-black transition hover:bg-white/90 active:scale-95">
              Approve
            </button>
          </form>
        </div>
      {/each}
    </div>
  {/if}
</section>

<!-- Approved -->
<section>
  <h2 class="mb-3 text-sm font-semibold text-white/90">Approved <span class="text-white/40">({approved.length})</span></h2>
  {#if approved.length === 0}
    <p class="rounded-xl border border-white/10 bg-white/[0.02] px-4 py-6 text-center text-sm text-white/40">No approved artists yet.</p>
  {:else}
    <div class="space-y-2">
      {#each approved as a (a.id)}
        <div class="flex items-center gap-3 rounded-xl border border-white/10 bg-white/[0.02] p-3">
          <div class="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-emerald-500/15 text-sm font-bold uppercase text-emerald-300">
            {a.username.slice(0, 2)}
          </div>
          <div class="min-w-0 flex-1">
            <p class="truncate font-semibold text-white">{a.username}</p>
            <p class="text-xs text-white/45">{a.pageCount} page{a.pageCount === 1 ? '' : 's'} · live on the roster</p>
          </div>
          <form method="POST" action="?/revoke" use:enhance>
            <input type="hidden" name="id" value={a.id} />
            <button class="rounded-lg border border-white/10 px-3 py-1.5 text-xs font-medium text-white/60 transition hover:bg-white/5 hover:text-white/90">
              Revoke
            </button>
          </form>
        </div>
      {/each}
    </div>
  {/if}
</section>
