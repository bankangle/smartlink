<script lang="ts">
  import { platformMeta } from '$lib/platforms';
  let { platform, size = 24 }: { platform: string; size?: number } = $props();
  const meta = $derived(platformMeta(platform));
</script>

<svg
  width={size}
  height={size}
  viewBox="0 0 24 24"
  style="color: {meta.color}"
  aria-hidden="true"
  role="img"
>
  {@html meta.icon}
</svg>
