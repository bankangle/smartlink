<script lang="ts">
  import { platformMeta } from '$lib/platforms';
  import type { PageStats } from '$lib/types';

  let { stats }: { stats: PageStats } = $props();

  const maxPlatform = $derived(Math.max(1, ...stats.byPlatform.map((p) => p.clicks)));

  // Build a continuous last-14-days series so the chart isn't gappy.
  const days = $derived.by(() => {
    const map = new Map(stats.byDay.map((d) => [d.date, d.clicks]));
    const out: { date: string; clicks: number; label: string }[] = [];
    const today = new Date();
    for (let i = 13; i >= 0; i--) {
      const d = new Date(today);
      d.setDate(today.getDate() - i);
      const key = d.toISOString().slice(0, 10);
      out.push({
        date: key,
        clicks: map.get(key) ?? 0,
        label: d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
      });
    }
    return out;
  });
  const maxDay = $derived(Math.max(1, ...days.map((d) => d.clicks)));
</script>

<div class="space-y-5">
  <!-- KPIs -->
  <div class="grid grid-cols-2 gap-3">
    <div class="rounded-xl border border-white/10 bg-white/[0.03] p-4">
      <p class="text-xs font-medium text-white/50">Total clicks</p>
      <p class="mt-1 text-2xl font-bold text-white">{stats.totalClicks}</p>
    </div>
    <div class="rounded-xl border border-white/10 bg-white/[0.03] p-4">
      <p class="text-xs font-medium text-white/50">Pre-saves</p>
      <p class="mt-1 text-2xl font-bold text-white">{stats.preSaves}</p>
    </div>
  </div>

  <!-- Per-platform -->
  <div class="rounded-xl border border-white/10 bg-white/[0.03] p-4">
    <h3 class="mb-3 text-sm font-semibold text-white">Clicks by platform</h3>
    {#if stats.byPlatform.length === 0}
      <p class="py-3 text-center text-sm text-white/40">No clicks yet.</p>
    {:else}
      <div class="space-y-2.5">
        {#each stats.byPlatform as p (p.platform)}
          {@const meta = platformMeta(p.platform)}
          <div class="flex items-center gap-3">
            <span class="w-24 shrink-0 truncate text-xs font-medium text-white/70">{meta.name}</span>
            <div class="h-2.5 flex-1 overflow-hidden rounded-full bg-white/5">
              <div class="h-full rounded-full" style="width: {(p.clicks / maxPlatform) * 100}%; background: {meta.color};"></div>
            </div>
            <span class="w-8 shrink-0 text-right text-xs font-semibold text-white">{p.clicks}</span>
          </div>
        {/each}
      </div>
    {/if}
  </div>

  <!-- Per-day -->
  <div class="rounded-xl border border-white/10 bg-white/[0.03] p-4">
    <h3 class="mb-3 text-sm font-semibold text-white">Last 14 days</h3>
    <div class="flex h-28 items-end gap-1">
      {#each days as d (d.date)}
        <div class="group relative flex flex-1 flex-col items-center justify-end">
          <div
            class="w-full rounded-t bg-gradient-to-t from-indigo-500 to-fuchsia-400 transition-all"
            style="height: {Math.max(2, (d.clicks / maxDay) * 100)}%;"
            title="{d.label}: {d.clicks}"
          ></div>
        </div>
      {/each}
    </div>
    <div class="mt-1.5 flex justify-between text-[10px] text-white/30">
      <span>{days[0]?.label}</span>
      <span>{days[days.length - 1]?.label}</span>
    </div>
  </div>
</div>
