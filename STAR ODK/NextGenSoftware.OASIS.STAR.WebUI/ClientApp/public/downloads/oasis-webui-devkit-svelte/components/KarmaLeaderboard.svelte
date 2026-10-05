<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';

  let leaderboard = [];

  onMount(async () => {
    const response = await $oasisStore.client.get('/api/karma/leaderboard');
    leaderboard = response.data;
  });
</script>

<div class="oasis-karma-leaderboard">
  <h2>Karma Leaderboard</h2>
  {#each leaderboard as leader, i}
    <div class="leader">
      <div class="rank {i === 0 ? 'gold' : i === 1 ? 'silver' : i === 2 ? 'bronze' : ''}">{i + 1}</div>
      <div class="avatar">{leader.username.charAt(0)}</div>
      <div class="info">
        <strong>{leader.username}</strong>
        <small>Level {leader.level}</small>
      </div>
      <div class="karma">{leader.karma.toLocaleString()}</div>
    </div>
  {/each}
</div>

<style>
  .leader { display: flex; align-items: center; gap: 16px; padding: 12px; border-bottom: 1px solid #eee; }
  .rank { font-size: 24px; font-weight: 600; width: 40px; }
  .rank.gold { color: #f39c12; }
  .rank.silver { color: #95a5a6; }
  .rank.bronze { color: #cd7f32; }
  .avatar { width: 48px; height: 48px; border-radius: 50%; background: #e3f2fd; display: flex; align-items: center; justify-content: center; font-weight: 600; }
  .info { flex: 1; }
  .karma { font-size: 18px; font-weight: 600; color: #667eea; }
</style>



