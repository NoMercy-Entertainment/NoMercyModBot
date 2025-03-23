<script setup lang="ts">
import { onMounted, ref } from 'vue';

import { updatePlayerEmbed } from '@/store/player.ts';
import type { PlayerEmbed } from '@/types/player.ts';

const props = defineProps({
  playerEmbed: {
    type: Object as () => PlayerEmbed,
    required: true,
  }
});

const elementRef = ref();

onMounted(() => {
  // @ts-ignore
  const player = new window.Twitch.Player(props.playerEmbed.elementId, props.playerEmbed!.config);
  
  player.addEventListener('playing', () => {
    const playbackStats = player.getPlaybackStats();
    const qualities = player.getQualities();
    const quality = player.getQuality();

    updatePlayerEmbed(props.playerEmbed.config.channel, playbackStats, qualities, quality, elementRef.value!);
  });
});

</script>

<template>
  <div ref="elementRef"
       :id="props.playerEmbed.elementId" 
       class="block relative aspect-video h-fit w-available py-2 mb-2 -mx-2 sm:mx-0"></div>
</template>

<style scoped>

</style>