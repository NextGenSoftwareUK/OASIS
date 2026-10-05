<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';
  
  let messages = [];
  let newMessage = '';

  onMount(async () => {
    const response = await $oasisStore.client.get('/api/messages');
    messages = response.data;
  });

  async function sendMessage() {
    if (!newMessage.trim()) return;
    await $oasisStore.client.post('/api/messages', { text: newMessage });
    newMessage = '';
    const response = await $oasisStore.client.get('/api/messages');
    messages = response.data;
  }
</script>

<div class="oasis-messaging">
  <div class="messages">
    {#each messages as message}
      <div class="message">
        <strong>{message.sender}:</strong> {message.text}
      </div>
    {/each}
  </div>
  <div class="input-area">
    <input bind:value={newMessage} on:keypress={(e) => e.key === 'Enter' && sendMessage()} placeholder="Type a message..." />
    <button on:click={sendMessage}>Send</button>
  </div>
</div>

<style>
  .oasis-messaging { padding: 20px; }
  .messages { max-height: 400px; overflow-y: auto; margin-bottom: 16px; }
  .message { padding: 12px; border-bottom: 1px solid #eee; }
  .input-area { display: flex; gap: 8px; }
  input { flex: 1; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
  button { padding: 10px 20px; background: #4A90E2; color: white; border: none; border-radius: 4px; cursor: pointer; }
</style>



