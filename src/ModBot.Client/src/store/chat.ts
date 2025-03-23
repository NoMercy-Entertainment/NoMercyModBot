import { ref, toRaw, type UnwrapRef, watch } from 'vue';

import type { ChatEmbed } from '@/types/chat.ts';
import type { Channel } from '@/types/moderation.ts';

export const chatEmbeds = ref(new Map<string, ChatEmbed>());

const getChatEmbed = (channelId: string): ChatEmbed | undefined => {
  return chatEmbeds.value.get(channelId) as ChatEmbed;
}

export const disposeChatEmbed = (channelId: string) => {
  return chatEmbeds.value.delete(channelId);
}

export const hasChatEmbed = (channelId: string): boolean => {
  return chatEmbeds.value.has(channelId);
}

export const addChatEmbed = (channel: Channel) => {
  const chatEmbed: Partial<ChatEmbed> = {
    elementId: `chat-${channel.broadcaster_login}`,
    config: {
      width: '100%',
      height: '100%',
      channel: channel.broadcaster_login,
      channelId: channel.broadcaster_id,
      channelPoints: 0,
    },
    messages: channel.broadcaster.broadcaster_chat_messages,    
    sendMessage(message: string) {
      // Send message to chat
      console.log(message);
    }
  };

  return chatEmbeds.value.set(channel.id, chatEmbed as UnwrapRef<ChatEmbed>);
}

export const updateChatEmbed = (channelId: string) => {
  const oldEmbed = getChatEmbed(channelId);
  if (!oldEmbed) return;

  const chatEmbed: ChatEmbed = {
    ...oldEmbed,
  };

  chatEmbeds.value.set(channelId, chatEmbed as UnwrapRef<ChatEmbed>);
}