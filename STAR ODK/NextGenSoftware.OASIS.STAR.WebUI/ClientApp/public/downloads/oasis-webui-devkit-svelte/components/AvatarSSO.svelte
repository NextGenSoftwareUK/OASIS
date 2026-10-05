<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  import { OASISClient } from '@oasis/api-client';

  export let providers: string[] = ['holochain', 'ethereum', 'solana', 'polkadot'];
  export let theme: 'light' | 'dark' = 'dark';

  let isOpen = false;
  let loading: string | null = null;
  
  const dispatch = createEventDispatcher();
  const client = new OASISClient();

  async function login(provider: string) {
    loading = provider;
    
    try {
      const avatar = await client.authenticateWithProvider(provider);
      dispatch('success', avatar);
      isOpen = false;
    } catch (error) {
      dispatch('error', error);
    } finally {
      loading = null;
    }
  }
</script>

<div class="oasis-avatar-sso">
  <button class="trigger-btn" on:click={() => isOpen = true}>
    Sign In with OASIS
  </button>

  {#if isOpen}
    <div class="modal-overlay" on:click={() => isOpen = false}>
      <div class="modal" class:dark={theme === 'dark'} on:click|stopPropagation>
        <div class="modal-header">
          <h2>Sign In to OASIS</h2>
          <button class="close-btn" on:click={() => isOpen = false}>×</button>
        </div>
        
        <div class="providers">
          {#each providers as provider}
            <button
              class="provider-btn"
              disabled={loading === provider}
              on:click={() => login(provider)}
            >
              {loading === provider ? 'Connecting...' : `Connect with ${provider}`}
            </button>
          {/each}
        </div>
      </div>
    </div>
  {/if}
</div>

<style>
  .trigger-btn {
    padding: 0.75rem 1.5rem;
    background: #00bcd4;
    color: white;
    border: none;
    border-radius: 8px;
    font-size: 1rem;
    cursor: pointer;
    transition: background 0.2s;
  }

  .trigger-btn:hover {
    background: #0097a7;
  }

  .modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.7);
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 1000;
  }

  .modal {
    background: white;
    padding: 2rem;
    border-radius: 12px;
    box-shadow: 0 10px 40px rgba(0, 0, 0, 0.3);
    min-width: 300px;
    max-width: 500px;
  }

  .modal.dark {
    background: #1a1a1a;
    color: white;
  }

  .modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
  }

  .close-btn {
    background: none;
    border: none;
    font-size: 1.5rem;
    cursor: pointer;
    color: inherit;
  }

  .providers {
    display: flex;
    flex-direction: column;
    gap: 0.75rem;
  }

  .provider-btn {
    padding: 0.75rem 1rem;
    background: #f5f5f5;
    border: 1px solid #ddd;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.2s;
    text-transform: capitalize;
  }

  .modal.dark .provider-btn {
    background: #2a2a2a;
    border-color: #3a3a3a;
    color: white;
  }

  .provider-btn:hover:not(:disabled) {
    background: #00bcd4;
    border-color: #00bcd4;
    color: white;
  }

  .provider-btn:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
</style>
