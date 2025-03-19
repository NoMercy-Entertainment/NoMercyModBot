<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { user, initializeUserSession } from "@/store/user";

import AppLayout from '@/components/AppLayout.vue';

const isAuthenticated = computed(() => !!user.value?.accessToken);

onMounted(async () => {
  await initializeUserSession();
});
</script>

<template>
  <AppLayout :is-authenticated="isAuthenticated" />
</template>

<style scoped lang="scss"> 
</style>