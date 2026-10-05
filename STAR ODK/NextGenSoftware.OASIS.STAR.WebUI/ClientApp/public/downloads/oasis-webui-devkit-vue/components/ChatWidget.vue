<template>
  <div class="oasis-chat-widget" :class="{ minimized: isMinimized }">
    <div class="chat-header" @click="toggleMinimize">
      <div class="header-info">
        <span class="chat-icon">💬</span>
        <span>Chat</span>
        <span v-if="unreadCount > 0" class="unread-badge">{{ unreadCount }}</span>
      </div>
      <button class="minimize-btn">{{ isMinimized ? '▲' : '▼' }}</button>
    </div>

    <div v-if="!isMinimized" class="chat-body">
      <div class="chat-messages" ref="messagesContainer">
        <div v-for="message in messages" :key="message.id" 
             :class="['message', { 'own-message': message.isOwn }]">
          <div class="message-avatar">
            {{ message.sender.charAt(0) }}
          </div>
          <div class="message-content">
            <div class="message-sender">{{ message.sender }}</div>
            <div class="message-text">{{ message.text }}</div>
            <div class="message-time">{{ formatTime(message.timestamp) }}</div>
          </div>
        </div>
      </div>

      <div class="chat-input">
        <input 
          v-model="newMessage" 
          @keypress.enter="sendMessage"
          placeholder="Type a message..."
        />
        <button @click="sendMessage" :disabled="!newMessage.trim()">
          Send
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue';
import { useOASIS } from '../composables/useOASIS';

interface Message {
  id: string;
  sender: string;
  text: string;
  timestamp: Date;
  isOwn: boolean;
}

const { client } = useOASIS();
const messages = ref<Message[]>([]);
const newMessage = ref('');
const isMinimized = ref(false);
const unreadCount = ref(0);
const messagesContainer = ref<HTMLElement | null>(null);

onMounted(async () => {
  await loadMessages();
  scrollToBottom();
});

async function loadMessages() {
  try {
    const response = await client.value.get('/chat/messages');
    messages.value = response.data;
  } catch (error) {
    console.error('Failed to load messages:', error);
  }
}

async function sendMessage() {
  if (!newMessage.value.trim()) return;

  try {
    const message = {
      text: newMessage.value,
      timestamp: new Date()
    };

    await client.value.post('/chat/messages', message);
    
    messages.value.push({
      id: Date.now().toString(),
      sender: 'You',
      text: newMessage.value,
      timestamp: new Date(),
      isOwn: true
    });

    newMessage.value = '';
    await nextTick();
    scrollToBottom();
  } catch (error) {
    console.error('Failed to send message:', error);
  }
}

function toggleMinimize() {
  isMinimized.value = !isMinimized.value;
  if (!isMinimized.value) {
    unreadCount.value = 0;
    nextTick(() => scrollToBottom());
  }
}

function scrollToBottom() {
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
  }
}

function formatTime(timestamp: Date) {
  return new Date(timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
}
</script>

<style scoped>
.oasis-chat-widget {
  position: fixed;
  bottom: 20px;
  right: 20px;
  width: 350px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.2);
  z-index: 1000;
  transition: all 0.3s;
}

.oasis-chat-widget.minimized {
  width: 200px;
}

.chat-header {
  background: #4A90E2;
  color: white;
  padding: 16px;
  border-radius: 12px 12px 0 0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  cursor: pointer;
}

.header-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.unread-badge {
  background: #e74c3c;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 12px;
}

.minimize-btn {
  background: none;
  border: none;
  color: white;
  cursor: pointer;
  font-size: 16px;
}

.chat-body {
  display: flex;
  flex-direction: column;
  height: 400px;
}

.chat-messages {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.message {
  display: flex;
  gap: 12px;
}

.message.own-message {
  flex-direction: row-reverse;
}

.message-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  background: #e3f2fd;
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 600;
  color: #1976d2;
}

.message-content {
  max-width: 70%;
}

.message.own-message .message-content {
  text-align: right;
}

.message-sender {
  font-size: 12px;
  font-weight: 600;
  margin-bottom: 4px;
  color: #666;
}

.message-text {
  background: #f5f5f5;
  padding: 8px 12px;
  border-radius: 12px;
  display: inline-block;
}

.message.own-message .message-text {
  background: #4A90E2;
  color: white;
}

.message-time {
  font-size: 11px;
  color: #999;
  margin-top: 4px;
}

.chat-input {
  display: flex;
  gap: 8px;
  padding: 16px;
  border-top: 1px solid #eee;
}

.chat-input input {
  flex: 1;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 6px;
}

.chat-input button {
  background: #4A90E2;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 6px;
  cursor: pointer;
}

.chat-input button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>



