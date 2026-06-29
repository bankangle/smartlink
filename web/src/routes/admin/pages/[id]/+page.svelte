<script lang="ts">
  import { enhance } from '$app/forms';
  import { page } from '$app/state';
  import PageEditor from '$lib/components/PageEditor.svelte';
  import Analytics from '$lib/components/Analytics.svelte';
  import type { PageData } from './$types';

  let { data, form }: { data: PageData; form: any } = $props();

  const justCreated = $derived(page.url.searchParams.get('created') === '1');
  let confirmingDelete = $state(false);
  let tab = $state<'edit' | 'analytics'>('edit');
</script>

<svelte:head><title>{data.adminPage.title} · SmartLink admin</title></svelte:head>

<div class="mb-5">
  <a href="/admin" class="text-sm text-white/40 transition hover:text-white/70">← Back to pages</a>
  <div class="mt-2 flex flex-wrap items-center justify-between gap-3">
    <h1 class="text-2xl font-bold text-white">{data.adminPage.title}</h1>
    <a href="/{data.adminPage.slug}" target="_blank" rel="noopener"
      class="rounded-lg border border-white/10 px-3 py-1.5 text-sm font-medium text-white/80 transition hover:bg-white/5">
      View page ↗
    </a>
  </div>
</div>

{#if justCreated}
  <div class="mb-4 rounded-lg border border-emerald-500/30 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-200">
    Page created. Share <span class="font-semibold">/{data.adminPage.slug}</span> with the world 🎉
  </div>
{/if}
{#if form?.saved}
  <div class="mb-4 rounded-lg border border-emerald-500/30 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-200">Changes saved.</div>
{/if}

<!-- Tabs -->
<div class="mb-5 inline-flex rounded-lg border border-white/10 bg-white/[0.03] p-1 text-sm">
  <button onclick={() => (tab = 'edit')}
    class="rounded-md px-4 py-1.5 font-medium transition {tab === 'edit' ? 'bg-white text-black' : 'text-white/60 hover:text-white'}">Edit</button>
  <button onclick={() => (tab = 'analytics')}
    class="rounded-md px-4 py-1.5 font-medium transition {tab === 'analytics' ? 'bg-white text-black' : 'text-white/60 hover:text-white'}">Analytics</button>
</div>

{#if tab === 'edit'}
  <PageEditor initial={data.adminPage} mode="edit" formError={form?.error ?? null} />

  <!-- Danger zone -->
  <div class="mt-8 rounded-2xl border border-rose-500/20 bg-rose-500/[0.04] p-4">
    <h2 class="text-sm font-semibold text-rose-200">Delete this page</h2>
    <p class="mt-0.5 text-xs text-white/50">This permanently removes the page and its click history.</p>
    {#if confirmingDelete}
      <form method="POST" action="?/delete" use:enhance class="mt-3 flex items-center gap-2">
        <span class="text-sm text-white/80">Are you sure?</span>
        <button class="rounded-lg bg-rose-500 px-3 py-1.5 text-sm font-semibold text-white transition hover:bg-rose-400">Yes, delete</button>
        <button type="button" onclick={() => (confirmingDelete = false)}
          class="rounded-lg border border-white/10 px-3 py-1.5 text-sm text-white/70 transition hover:bg-white/5">Cancel</button>
      </form>
    {:else}
      <button onclick={() => (confirmingDelete = true)}
        class="mt-3 rounded-lg border border-rose-500/30 px-3 py-1.5 text-sm font-medium text-rose-200 transition hover:bg-rose-500/10">Delete page</button>
    {/if}
  </div>
{:else if data.stats}
  <Analytics stats={data.stats} />
{:else}
  <p class="text-sm text-white/40">Analytics are unavailable right now.</p>
{/if}
