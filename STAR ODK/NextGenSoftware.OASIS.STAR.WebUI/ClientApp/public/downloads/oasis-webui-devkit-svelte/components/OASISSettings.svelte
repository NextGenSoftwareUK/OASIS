<script lang="ts">
  import { oasisStore } from '../stores/oasis';

  let theme = 'light';
  let privacy = 'public';

  async function saveSettings() {
    await $oasisStore.client.post('/api/settings', { theme, privacy });
  }
</script>

<div class="oasis-settings">
  <div class="setting">
    <label>Theme</label>
    <select bind:value={theme}>
      <option value="light">Light</option>
      <option value="dark">Dark</option>
    </select>
  </div>
  <div class="setting">
    <label>Privacy</label>
    <select bind:value={privacy}>
      <option value="public">Public</option>
      <option value="private">Private</option>
    </select>
  </div>
  <button on:click={saveSettings}>Save Settings</button>
</div>

<style>
  .setting { margin-bottom: 20px; }
  label { display: block; margin-bottom: 8px; font-weight: 600; }
  select { width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
  button { padding: 12px 24px; background: #27ae60; color: white; border: none; border-radius: 4px; cursor: pointer; }
</style>



