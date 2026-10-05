<script lang="ts">
  let minimized = false;
  let newMessage = '';
  let messages = [];

  function toggle() {
    minimized = !minimized;
  }

  function send() {
    if (!newMessage.trim()) return;
    messages = [...messages, { sender: 'You', text: newMessage }];
    newMessage = '';
  }
</script>

<div class="chat-widget" class:minimized>
  <div class="header" on:click={toggle}>
    <span>💬 Chat</span>
    <span>{minimized ? '▲' : '▼'}</span>
  </div>
  {#if !minimized}
    <div class="body">
      <div class="messages">
        {#each messages as msg}
          <div class="message"><strong>{msg.sender}:</strong> {msg.text}</div>
        {/each}
      </div>
      <div class="input-area">
        <input bind:value={newMessage} on:keypress={(e) => e.key === 'Enter' && send()} placeholder="Type..." />
        <button on:click={send}>Send</button>
      </div>
    </div>
  {/if}
</div>

<style>
  .chat-widget { position: fixed; bottom: 20px; right: 20px; width: 350px; background: white; border-radius: 12px; box-shadow: 0 4px 16px rgba(0,0,0,0.2); z-index: 1000; }
  .chat-widget.minimized { width: 200px; }
  .header { background: #4A90E2; color: white; padding: 16px; border-radius: 12px 12px 0 0; display: flex; justify-content: space-between; cursor: pointer; }
  .body { height: 400px; display: flex; flex-direction: column; }
  .messages { flex: 1; overflow-y: auto; padding: 16px; }
  .input-area { display: flex; gap: 8px; padding: 16px; border-top: 1px solid #eee; }
  input { flex: 1; padding: 10px; border: 1px solid #ddd; border-radius: 6px; }
  button { padding: 10px 20px; background: #4A90E2; color: white; border: none; border-radius: 6px; }
</style>



