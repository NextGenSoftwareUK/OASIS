<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';

  let achievements = [];
  let activeTab = 'all';
  
  $: filteredAchievements = activeTab === 'all' 
    ? achievements 
    : activeTab === 'unlocked' 
      ? achievements.filter(a => a.unlocked)
      : achievements.filter(a => !a.unlocked);

  onMount(async () => {
    const response = await $oasisStore.client.get('/api/achievements');
    achievements = response.data;
  });
</script>

<div class="oasis-achievements">
  <h2>Achievements & Badges</h2>
  
  <div class="filter-tabs">
    <button class:active={activeTab === 'all'} on:click={() => activeTab = 'all'}>All</button>
    <button class:active={activeTab === 'unlocked'} on:click={() => activeTab = 'unlocked'}>Unlocked</button>
    <button class:active={activeTab === 'locked'} on:click={() => activeTab = 'locked'}>Locked</button>
  </div>

  <div class="achievements-grid">
    {#each filteredAchievements as achievement}
      <div class="achievement-card" class:locked={!achievement.unlocked}>
        <div class="icon">
          {achievement.icon}
          {#if achievement.unlocked}<span class="badge">✓</span>{/if}
        </div>
        <div class="info">
          <h3>{achievement.name}</h3>
          <p>{achievement.description}</p>
          <span class="karma-reward">+{achievement.karmaReward} Karma</span>
        </div>
      </div>
    {/each}
  </div>
</div>

<style>
  .filter-tabs { display: flex; gap: 8px; margin-bottom: 24px; }
  .filter-tabs button { padding: 8px 16px; border: 1px solid #ddd; background: white; border-radius: 20px; cursor: pointer; }
  .filter-tabs button.active { background: #4A90E2; color: white; }
  .achievements-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 20px; }
  .achievement-card { display: flex; gap: 16px; background: white; padding: 20px; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
  .achievement-card.locked { opacity: 0.6; }
  .icon { font-size: 48px; position: relative; }
  .badge { position: absolute; top: -4px; right: -4px; width: 20px; height: 20px; background: #27ae60; color: white; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 12px; }
  .info h3 { margin: 0 0 8px 0; font-size: 16px; }
  .karma-reward { background: #fff3cd; color: #856404; padding: 4px 8px; border-radius: 4px; font-size: 12px; font-weight: 600; }
</style>



