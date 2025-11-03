<template>
  <slot :client="client" :isAuthenticated="isAuthenticated" :user="user" />
</template>

<script setup lang="ts">
import { ref, provide } from 'vue';
import axios from 'axios';

const props = defineProps<{ apiUrl: string; apiKey?: string }>();

const client = ref(axios.create({ baseURL: props.apiUrl, headers: props.apiKey ? { 'X-API-Key': props.apiKey } : {} }));
const isAuthenticated = ref(false);
const user = ref(null);

provide('oasisClient', client);
provide('oasisAuth', { isAuthenticated, user });
</script>



