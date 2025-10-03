<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';

  let posts = [];

  onMount(async () => {
    const response = await $oasisStore.client.get('/api/social/feed');
    posts = response.data;
  });
</script>

<div class="social-feed">
  {#each posts as post}
    <div class="post">
      <div class="post-header">
        <div class="avatar"></div>
        <div><strong>{post.author}</strong><br/><small>{post.time}</small></div>
      </div>
      <p>{post.content}</p>
      <div class="actions">
        <button>👍 {post.likes}</button>
        <button>💬 {post.comments}</button>
      </div>
    </div>
  {/each}
</div>

<style>
  .post { background: white; padding: 20px; margin-bottom: 16px; border-radius: 8px; }
  .post-header { display: flex; gap: 12px; margin-bottom: 12px; }
  .avatar { width: 40px; height: 40px; border-radius: 50%; background: #e3f2fd; }
  .actions { display: flex; gap: 16px; margin-top: 12px; }
  .actions button { background: none; border: none; cursor: pointer; }
</style>



