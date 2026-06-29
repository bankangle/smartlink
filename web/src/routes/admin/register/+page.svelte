<script lang="ts">
  import { enhance } from '$app/forms';
  import Spinner from '$lib/components/Spinner.svelte';
  let { form } = $props();
  let submitting = $state(false);
</script>

<svelte:head><title>Create an artist account · Coasthill IV</title></svelte:head>

<div class="relative flex min-h-[100dvh] items-center justify-center overflow-hidden bg-[#08090a] px-5 text-[#f7f8f8]">
  <!-- faint top beam + glow, matching the landing -->
  <div class="pointer-events-none absolute inset-x-0 top-0 h-80 overflow-hidden" aria-hidden="true">
    <div class="absolute left-1/2 top-0 h-px w-[520px] max-w-[90%] -translate-x-1/2" style="background:linear-gradient(90deg,transparent,rgba(255,255,255,.35),transparent)"></div>
    <div class="absolute left-1/2 top-[-200px] h-[420px] w-[620px] max-w-[120%] -translate-x-1/2 rounded-full bg-[#5b6ef5]/[0.08] blur-[120px]"></div>
  </div>

  <div class="relative w-full max-w-sm">
    <div class="mb-7 text-center">
      <a href="/" class="mx-auto mb-4 inline-flex items-center gap-2 text-sm font-semibold tracking-tight">
        <span class="grid h-5 w-5 place-items-center rounded-[5px] bg-white text-[10px] font-bold text-black">C</span>
        Coasthill <span class="text-white/40">IV</span>
      </a>
      <h1 class="text-xl font-semibold tracking-tight">Create your artist account</h1>
      <p class="mt-1.5 text-sm text-white/45">Start building smart links for your releases.</p>
    </div>

    <form
      method="POST"
      action="?/register"
      use:enhance={() => {
        submitting = true;
        return async ({ update }) => {
          await update();
          submitting = false;
        };
      }}
      class="space-y-3 rounded-xl border border-white/[0.08] bg-white/[0.02] p-6"
    >
      {#if form?.error}
        <div class="rounded-lg border border-rose-500/30 bg-rose-500/10 px-3 py-2 text-sm text-rose-200">{form.error}</div>
      {/if}

      <label class="block">
        <span class="mb-1 block font-mono text-[11px] uppercase tracking-[0.15em] text-white/45">Username</span>
        <input
          name="username"
          value={form?.username ?? ''}
          autocomplete="username"
          required
          class="w-full rounded-lg border border-white/[0.08] bg-black/30 px-3 py-2.5 text-sm outline-none transition focus:border-white/30"
        />
      </label>
      <label class="block">
        <span class="mb-1 block font-mono text-[11px] uppercase tracking-[0.15em] text-white/45">Password</span>
        <input
          name="password"
          type="password"
          autocomplete="new-password"
          required
          minlength="6"
          class="w-full rounded-lg border border-white/[0.08] bg-black/30 px-3 py-2.5 text-sm outline-none transition focus:border-white/30"
        />
      </label>
      <label class="block">
        <span class="mb-1 block font-mono text-[11px] uppercase tracking-[0.15em] text-white/45">Confirm password</span>
        <input
          name="confirm"
          type="password"
          autocomplete="new-password"
          required
          minlength="6"
          class="w-full rounded-lg border border-white/[0.08] bg-black/30 px-3 py-2.5 text-sm outline-none transition focus:border-white/30"
        />
      </label>

      <button
        disabled={submitting}
        class="flex w-full items-center justify-center gap-2 rounded-lg bg-white py-2.5 text-sm font-semibold text-black transition hover:bg-white/90 active:scale-[0.99] disabled:opacity-60"
      >
        {#if submitting}<Spinner size={16} />{/if}
        Create account
      </button>
    </form>

    <p class="mt-4 text-center text-sm text-white/40">
      Already have an account?
      <a href="/admin/login" class="font-medium text-white/75 underline-offset-2 transition hover:text-white hover:underline">Sign in</a>
    </p>
  </div>
</div>
