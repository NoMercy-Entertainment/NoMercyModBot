import { ref, watch, type Ref, toRaw, type UnwrapRef } from 'vue';
import type { Channel } from '@/types/moderation.ts';
import type { PlaybackStats, PlayerEmbed } from '@/types/player.ts';

export const playerEmbeds = ref(new Map<string, PlayerEmbed>());

const getPlayerEmbed = (channelId: string): PlayerEmbed | undefined => {
  return playerEmbeds.value.get(channelId) as PlayerEmbed;
}

export const disposePlayerEmbed = (channelId: string) => {
  return playerEmbeds.value.delete(channelId);
}

export const hasPlayerEmbed = (channelId: string): boolean => {
  return playerEmbeds.value.has(channelId);
}

export const addPlayerEmbed = (channel: Channel) => {
  const playerEmbed: Partial<PlayerEmbed> = {
    elementId: `player-${channel.broadcaster_login}`,
    config: {
      width: '100%',
      height: '100%',
      channel: channel.broadcaster_login,
      volume: 1,
      parent: ["modbot.nomercy.tv"],
    },
    qualities: [],
    quality: '',
  };

  return playerEmbeds.value.set(channel.id, playerEmbed as UnwrapRef<PlayerEmbed>);
}

export const updatePlayerEmbed = (channelId: string, playbackStats: PlaybackStats, qualities: string[], quality: string, elementRef: Ref<HTMLElement>) => {
  const oldEmbed = getPlayerEmbed(channelId);
  if (!oldEmbed) return;

  const playerEmbed: PlayerEmbed = {
    ...oldEmbed,
    playbackStats,
    qualities,
    quality,
    element: elementRef,
  };

  playerEmbeds.value.set(channelId, playerEmbed as UnwrapRef<PlayerEmbed>);
}