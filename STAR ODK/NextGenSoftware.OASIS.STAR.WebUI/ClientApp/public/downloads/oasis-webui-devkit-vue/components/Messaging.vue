<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue';
import { OASISClient } from '@oasis/api-client';

interface Props {
  chatId: string;
  avatarId: string;
  theme?: 'light' | 'dark';
}

const props = withDefaults(defineProps<Props>(), {
  theme: 'dark'
});

const messages = ref<any[]>([]);
const newMessage = ref('');
const loading = ref(false);
const messagesContainer = ref<HTMLElement | null>(null);
const client = new OASISClient();

onMounted(async () => {
  await loadMessages();
  scrollToBottom();
});

async function loadMessages() {
  loading.value = true;
  try {
    messages.value = await client.getChatMessages(props.chatId);
  } finally {
    loading.value = false;
  }
}

async function sendMessage() {
  if (!newMessage.value.trim()) return;

  const content = newMessage.value;
  newMessage.value = '';

  try {
    await client.sendMessage(props.chatId, props.avatarId, content);
    await loadMessages();
    await nextTick();
    scrollToBottom();
  } catch (error) {
    console.error('Error sending message:', error);
  }
}

function scrollToBottom() {
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
  }
}
</script>

<template>
  <div class="oasis-messaging" :class="[`oasis-messaging--${theme}`]">
    <div ref="messagesContainer" class="messages">
      <div v-if="loading" class="loading">Loading messages...</div>
      <div
        v-for="msg in messages"
        :key="msg.id"
        class="message"
        :class="{ 'message--own': msg.avatarId === avatarId }"
      >
        <div class="message-avatar">{{ msg.avatarName?.charAt(0) }}</div>
        <div class="message-content">
          <div class="message-header">
            <span class="message-author">{{ msg.avatarName }}</span>
            <span class="message-time">{{ new Date(msg.timestamp).toLocaleTimeString() }}</span>
          </div>
          <div class="message-text">{{ msg.content }}</div>
        </div>
      </div>
    </div>

    <div class="input-area">
      <input
        v-model="newMessage"
        type="text"
        placeholder="Type a message..."
        @keyup.enter="sendMessage"
      />
      <button @click="sendMessage">Send</button>
    </div>
  </div>
</template>

<style scoped>
.oasis-messaging {
  display: flex;
  flex-direction: column;
  height: 500px;
  background: white;
  border-radius: 12px;
  overflow: hidden;
}

.oasis-messaging--dark {
  background: #1a1a1a;
  color: white;
}

.messages {
  flex: 1;
  overflow-y: auto;
  padding: 1rem;
}

.message {
  display: flex;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.message--own {
  flex-direction: row-reverse;
}

.message-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #00bcd4;
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: bold;
  flex-shrink: 0;
}

.message-content {
  flex: 1;
  max-width: 70%;
}

.message-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 0.25rem;
  font-size: 0.875rem;
}

.message-author {
  font-weight: bold;
}

.message-time {
  opacity: 0.7;
}

.message-text {
  background: #f5f5f5;
  padding: 0.75rem;
  border-radius: 8px;
}

.oasis-messaging--dark .message-text {
  background: #2a2a2a;
}

.input-area {
  display: flex;
  gap: 0.5rem;
  padding: 1rem;
  border-top: 1px solid #ddd;
}

.oasis-messaging--dark .input-area {
  border-top-color: #2a2a2a;
}

.input-area input {
  flex: 1;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 8px;
  background: white;
  color: #333;
}

.oasis-messaging--dark .input-area input {
  background: #2a2a2a;
  border-color: #3a3a3a;
  color: white;
}

.input-area button {
  padding: 0.75rem 1.5rem;
  background: #00bcd4;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.2s;
}

.input-area button:hover {
  background: #0097a7;
}
</style>
