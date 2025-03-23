<script setup lang="ts">

import type { Channel } from '@/types/moderation.ts';

import LiveIndicator from './LiveIndicator.vue';

defineProps({
  channel: {
    type: Object as () => Channel,
    required: true,
  },
  size: {
    type: String,
    default: 'size-24',
  },
});

</script>

<template>
  <div id="channel-img" class="flex relative">
    <img :src="channel.broadcaster.profile_image_url" 
         alt=""
         class="rounded-full ring-2 ring-neutral-500/50"
         :class="size"
         :style="`background-color: hsl(from ${channel.broadcaster.color} h calc(s * 1.1) calc(l / 1.5))`"/>
    <LiveIndicator :channel="channel" />
  </div>
</template>

<style scoped lang="scss">
.sticky {
  container-type: scroll-state;
  
  #channel-img img {

    transition-property: transform, width, height;
    transition-duration: 500ms, 150ms, 150ms;
    transition-timing-function: ease-in-out, ease-in-out, ease-in-out;
    transition-delay: 0ms, 0ms, 0ms;
    transform-origin: bottom right;

    @container scroll-state(stuck: top) {
      transform-origin: bottom left;
      margin-top: 5rem;
      width: calc(var(--spacing) * 14);
      height: calc(var(--spacing) * 14);
    }
  }
}
</style>
