import type { Ref } from 'vue';
import type { ChatMessage } from '@/types/moderation.ts';

export interface ChatEmbed {
  config: ChannelEmbedConfig;
  element?: Ref<HTMLElement | null>;
  elementId: string;
  messages: ChatMessage[];
  sendMessage (message: string): void;
}

export interface ChannelEmbedConfig {
  width: string,
  height: string,
  channel: string,
  channelId: string,
  channelPoints: number,
}