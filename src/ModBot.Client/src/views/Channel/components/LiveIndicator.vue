<script setup lang="ts">

import type { Channel } from '@/types/moderation.ts';

defineProps({
  channel: {
    type: Object as () => Channel,
    required: true,
  },
});

</script>

<template>
  <div v-if="channel.is_live"
       class="live-indicator absolute -bottom-2 left-1/2 -translate-x-1/2 px-1.5 py-1 rounded-sm text-sm font-bold leading-none bg-red-600 text-white">
    LIVE
  </div>
</template>


<style scoped lang="scss">
.sticky {
  container-type: scroll-state;

  .live-indicator {
    transition-property: all;
    transition-duration: 150ms;
    transition-timing-function: ease-in-out;
    transition-delay: 0ms;
    transform-origin: bottom right;

    @container scroll-state(stuck: top) {
      font-size: 0.5rem;
      bottom: calc(var(--spacing) * -1);
      padding-inline: calc(var(--spacing) * 1);
      padding-block: calc(var(--spacing) * 0.5);
    }
  }
}
</style>
