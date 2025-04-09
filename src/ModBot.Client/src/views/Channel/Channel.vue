<script setup lang="ts">
import { onMounted, onUnmounted, watch } from 'vue';

import type { Channel } from '@/types/moderation.ts';
import type { PlayerEmbed } from '@/types/player.ts';
import type { ChatEmbed } from '@/types/chat.ts';

import useServerClient from '@/lib/clients/useServerClient.ts';

import { disposePlayerEmbed, playerEmbeds } from '@/store/player.ts';
import { disposeChatEmbed, chatEmbeds } from '@/store/chat.ts';
import { setThemeColor } from '@/store/channel.ts';
import { user } from '@/store/user.ts';

import ChannelHeader from '@/views/Channel/components/ChannelHeader.vue';
import ChannelPlayer from '@/views/Channel/components/ChannelPlayer.vue';
import ChannelChat from '@/views/Channel/components/ChannelChat.vue';
import { useRoute } from 'vue-router';

const route = useRoute();

const { data: channel, error, isLoading } = useServerClient<Channel>({
  queryKey: [route.path],
});

watch(channel, (value) => {
  if (!value) return;
  setThemeColor(value.broadcaster.color);
});

onMounted(() => {
  if (!channel.value) return;
  setThemeColor(channel.value.broadcaster.color);
});

onUnmounted(async () => {
  if (!channel.value) return;

  // Dispose embeds
  disposePlayerEmbed(channel.value.id);
  await disposeChatEmbed(channel.value.id);

  setThemeColor(user.value.color);
});

</script>

<template>
  <div v-if="channel"
       class="min-h-[calc(100dvh+8.1rem)] max-h-[calc(100dvh+8.1rem)] sm:min-h-[calc(100dvh+12.1rem)] sm:max-h-[calc(100dvh+12.1rem)] w-available flex flex-col overflow-clip">
    <ChannelHeader :channel="channel" />
    <div class="flex flex-col sm:flex-row flex-1 h-[50dvh] w-available relative p-2 justify-between overflow-clip">
      <ChannelPlayer
        v-for="[key, playerEmbed] in playerEmbeds.entries()"
        :key="key"
        :playerEmbed="playerEmbed as PlayerEmbed" />
  
      <ChannelChat
        v-for="chatEmbed in chatEmbeds.entries()"
        :key="chatEmbed[0]"
        :chatEmbed="chatEmbed[1] as ChatEmbed" />
    </div>
  </div>
</template>
