<script lang="ts">
  import { createEventDispatcher } from 'svelte';

  export let nft: any;
  export let theme: 'light' | 'dark' = 'dark';

  const dispatch = createEventDispatcher();

  function handleClick() {
    dispatch('click', nft);
  }
</script>

<div class="nft-card" class:dark={theme === 'dark'} on:click={handleClick}>
  <img src={nft.imageUrl} alt={nft.name} class="nft-image" />
  <div class="nft-info">
    <h4 class="nft-name">{nft.name}</h4>
    {#if nft.collection}
      <p class="nft-collection">{nft.collection}</p>
    {/if}
    <p class="nft-price">{nft.price || 0} OASIS</p>
  </div>
</div>

<style>
  .nft-card {
    background: white;
    border: 1px solid #ddd;
    border-radius: 8px;
    overflow: hidden;
    cursor: pointer;
    transition: transform 0.2s, box-shadow 0.2s;
  }
  
  .nft-card.dark {
    background: #2a2a2a;
    border-color: #3a3a3a;
    color: white;
  }
  
  .nft-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  }
  
  .nft-image {
    width: 100%;
    height: 200px;
    object-fit: cover;
  }
  
  .nft-info {
    padding: 1rem;
  }
  
  .nft-name {
    font-weight: bold;
    margin: 0 0 0.5rem 0;
  }
  
  .nft-collection {
    font-size: 0.875rem;
    opacity: 0.7;
    margin: 0 0 0.5rem 0;
  }
  
  .nft-price {
    color: #00bcd4;
    font-weight: bold;
    margin: 0;
  }
</style>

