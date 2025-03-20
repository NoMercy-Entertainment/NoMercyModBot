<script setup lang="ts">
import { ClockIcon } from '@heroicons/vue/20/solid';

import type { Channel } from '@/types/moderation.ts';

defineProps({
  channel: {
    type: Object as () => Channel,
    required: true,
  },
});

const channelTime = (timezone: string | null) => {
  if (!timezone) {
    return 'unknown';
  }
  
  const date = new Date();
  
  const options: Intl.DateTimeFormatOptions | undefined = {
    timeZone: timezone,
    hour: 'numeric',
    minute: 'numeric',
    hour12: false,
  };
  
  return new Intl.DateTimeFormat('en-US', options).format(date);
};

const generateGradient = (color: string | null) => {
  if (!color) return '';

  return `radial-gradient(circle at 25% 25%, rgb(from ${color} r g b / 0.6) 0%, transparent 50%),
          radial-gradient(circle at 75% 25%, rgb(from ${color} r g b / 0.4) 0%, transparent 50%),
          radial-gradient(circle at 50% 50%, rgb(from ${color} r g b / 0.8) 0%, transparent 55%),
          radial-gradient(circle at 25% 75%, rgb(from ${color} r g b / 0.4) 0%, transparent 50%),
          radial-gradient(circle at 75% 75%, rgb(from ${color} r g b / 0.6) 0%, transparent 50%)`;
};
</script>

<template>
  <RouterLink :to="channel.link"
    class="relative overflow-hidden h-min flex flex-col divide-y divide-neutral-200 dark:divide-neutral-700 rounded-lg bg-neutral-100 dark:bg-neutral-900 text-center shadow-sm">
    <div class="absolute inset-0 opacity-30"
         :style="`background: ${generateGradient(channel.broadcaster.color)}`" />
    <div class="flex flex-1 flex-col p-7">
      <div class="relative mx-auto">
        <img class="min-w-36 size-36 shrink-0 rounded-full"
             :style="`background-color: hsl(from ${channel.broadcaster.color} h calc(s / 1.5) calc(l / 1.8))`"
             :src="channel.broadcaster.profile_image_url"
             :alt="`${channel.broadcaster.display_name} profile image`" />
        <div v-if="channel.is_live"
             class="absolute -bottom-2 left-1/2 -translate-x-1/2 px-2.5 py-0.5 rounded-sm text-xs font-medium bg-red-600 text-white">
          LIVE
        </div>
      </div>
      <h3 class="mt-6 text-sm font-medium">
        {{ channel.broadcaster_name }}
      </h3>
    </div>
    <div>
      <div class="-mt-px flex divide-x divide-neutral-200 dark:divide-neutral-700">
        <div class="flex w-0 flex-1">
          <div class="relative -mr-px inline-flex w-0 flex-1 items-center justify-center gap-x-3 rounded-bl-lg border border-transparent py-4 text-sm font-semibold">
            <ClockIcon class="size-5 text-neutral-400" aria-hidden="true" />
            {{ channelTime(channel.broadcaster.timezone) }}
          </div>
        </div>
      </div>
    </div>
  </RouterLink>
</template>

<style scoped>

</style>