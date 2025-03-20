<script setup lang="ts">
import useServerClient from '@/lib/clients/useServerClient.ts';

import type { Channel } from '@/types/moderation.ts';

import ChannelCard from '@/components/Cards/ChannelCard.vue';

const { data, error, isLoading } = useServerClient<Channel[]>();

</script>

<template>
  <template v-if="!isLoading">
    <ul role="list"
        class="w-available h-available flex flex-wrap gap-7 p-8 content-start text-neutral-900 dark:text-white">

      <template v-for="channel in data ?? []" :key="channel.id">
        <ChannelCard :channel="channel" />
      </template>
    </ul>
  </template>
</template>

<style scoped>

</style>