<script setup lang="ts">
import { onMounted, ref } from 'vue';

import useServerClient from '@/lib/clients/useServerClient.ts';

import type { Channel } from '@/types/moderation.ts';

import ChannelCard from '@/views/Channel/components/ChannelCard.vue';

const showing = ref(false);
const { data, error, isLoading } = useServerClient<Channel[]>();

onMounted(() => {
  setTimeout(() => {
    showing.value = true;
  }, 150);
});

</script>

<template>
  <template v-if="!isLoading && showing">
    <ul role="list"
        class="w-available h-available flex flex-wrap gap-4 sm:gap-[calc(100%/60)] p-2 sm:p-7 content-start text-neutral-900 dark:text-white">

      <template v-for="channel in data ?? []" :key="channel.id">
        <ChannelCard :channel="channel" />
      </template>
    </ul>
  </template>
</template>

<style scoped>

</style>