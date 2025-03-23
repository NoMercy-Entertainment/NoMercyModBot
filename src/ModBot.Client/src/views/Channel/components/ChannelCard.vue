<script setup lang="ts">
import { ClockIcon } from '@heroicons/vue/20/solid';

import type { Channel } from '@/types/moderation.ts';
import LiveIndicator from '@/views/Channel/components/LiveIndicator.vue';
import ChannelImage from '@/views/Channel/components/ChannelImage.vue';

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
      <ChannelImage :channel="channel" size="w-36 size-36" />
      <h3 class="mt-6 text-base sm:text-lg font-medium"
          :style="`text-shadow: 
                     -1px -1px 2px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), 1px -1px 2px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), -1px 1px 2px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), 1px 1px 2px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)),
                     -2px -2px 15px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), 2px -2px 15px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), -2px 2px 15px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), 2px 2px 15px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5))
                  `">
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