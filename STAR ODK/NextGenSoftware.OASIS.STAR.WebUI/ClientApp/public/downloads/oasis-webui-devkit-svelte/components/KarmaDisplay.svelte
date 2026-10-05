<script lang="ts">
  import { onMount } from 'svelte';
  import { OASISClient } from '@oasis/api-client';

  export let avatarId: string;
  export let theme: 'light' | 'dark' = 'dark';
  export let showHistory = true;

  let karma: any = null;
  let history: any[] = [];
  let loading = false;
  
  const client = new OASISClient();

  onMount(async () => {
    loading = true;
    try {
      karma = await client.getAvatarKarma(avatarId);
      if (showHistory) {
        history = await client.getKarmaHistory(avatarId);
      }
    } finally {
      loading = false;
    }
  });
</script>

<div class="oasis-karma" class:dark={theme === 'dark'}>
  <div class="karma-current">
    <h3>Karma Points</h3>
    {#if loading}
      <div class="karma-value">Loading...</div>
    {:else}
      <div class="karma-value">{karma?.total || 0}</div>
    {/if}
  </div>

  {#if karma}
    <div class="karma-stats">
      <div class="stat">
        <span class="label">Rank</span>
        <span class="value">#{karma.rank || '-'}</span>
      </div>
      <div class="stat">
        <span class="label">Level</span>
        <span class="value">{karma.level || 1}</span>
      </div>
      <div class="stat">
        <span class="label">Next Level</span>
        <span class="value">{karma.nextLevelAt || '-'}</span>
      </div>
    </div>
  {/if}

  {#if showHistory && history.length > 0}
    <div class="history">
      <h4>Recent Activity</h4>
      {#each history as entry}
        <div class="history-item">
          <span>{entry.reason}</span>
          <span class:positive={entry.amount > 0} class:negative={entry.amount < 0}>
            {entry.amount > 0 ? '+' : ''}{entry.amount}
          </span>
        </div>
      {/each}
    </div>
  {/if}
</div>

<style>
  .oasis-karma {
    padding: 1.5rem;
    background: white;
    color: #333;
    border-radius: 12px;
  }
  
  .oasis-karma.dark {
    background: #1a1a1a;
    color: white;
  }
  
  .karma-current {
    text-align: center;
    margin-bottom: 1.5rem;
  }
  
  .karma-value {
    font-size: 3rem;
    font-weight: bold;
    color: #00bcd4;
    margin: 0.5rem 0;
  }
  
  .karma-stats {
    display: flex;
    justify-content: space-around;
    gap: 1rem;
  }
  
  .stat {
    text-align: center;
  }
  
  .stat .label {
    display: block;
    font-size: 0.875rem;
    opacity: 0.7;
    margin-bottom: 0.25rem;
  }
  
  .stat .value {
    font-size: 1.25rem;
    font-weight: bold;
  }
  
  .history {
    margin-top: 1.5rem;
    border-top: 1px solid #ddd;
    padding-top: 1rem;
  }
  
  .history-item {
    display: flex;
    justify-content: space-between;
    padding: 0.5rem 0;
  }
  
  .positive {
    color: #4caf50;
  }
  
  .negative {
    color: #f44336;
  }
</style>

