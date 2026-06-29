<script lang="ts">
  import { page } from '$app/state';
  import { enhance } from '$app/forms';
  let { children, data } = $props();

  const onLogin = $derived(page.url.pathname === '/admin/login');
</script>

{#if onLogin || !data.username}
  {@render children()}
{:else}
  <div class="min-h-[100dvh] bg-[#0a0a0b]">
    <header class="sticky top-0 z-20 border-b border-white/10 bg-[#0a0a0b]/80 backdrop-blur">
      <div class="mx-auto flex max-w-5xl items-center justify-between px-4 py-3.5">
        <a href="/admin" class="flex items-center gap-2 font-bold text-white">
          <span class="grid h-7 w-7 place-items-center rounded-lg bg-white text-xs font-bold text-black">C</span>
          Coasthill <span class="text-white/40">IV</span>
        </a>
        <div class="flex items-center gap-3 text-sm">
          {#if data.isAdmin}
            <a href="/admin" class="font-medium text-white/70 transition hover:text-white" class:text-white={page.url.pathname === '/admin'}>Pages</a>
            <a href="/admin/artists" class="flex items-center gap-1.5 font-medium text-white/70 transition hover:text-white" class:text-white={page.url.pathname === '/admin/artists'}>
              Artists
              {#if data.pendingArtists > 0}
                <span class="grid h-5 min-w-5 place-items-center rounded-full bg-amber-500 px-1.5 text-[11px] font-bold text-black">{data.pendingArtists}</span>
              {/if}
            </a>
          {/if}
          <span class="hidden text-white/50 sm:inline">{data.username}</span>
          <form method="POST" action="/admin/login?/logout" use:enhance>
            <button class="rounded-lg border border-white/10 px-3 py-1.5 font-medium text-white/70 transition hover:bg-white/5">
              Sign out
            </button>
          </form>
        </div>
      </div>
    </header>
    <main class="mx-auto max-w-5xl px-4 py-6 sm:py-8">
      {@render children()}
    </main>
  </div>
{/if}
