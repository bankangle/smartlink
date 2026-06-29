<script lang="ts">
  import PageEditor from '$lib/components/PageEditor.svelte';
  let { form } = $props();
</script>

<svelte:head><title>New page · SmartLink admin</title></svelte:head>

<div class="mb-6">
  <a href="/admin" class="text-sm text-white/40 transition hover:text-white/70">← Back to pages</a>
  <h1 class="mt-2 text-2xl font-bold text-white">New smart link</h1>
</div>

<PageEditor initial={{}} mode="new" formError={form?.error ?? null} />
