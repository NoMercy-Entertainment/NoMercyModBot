import { ref, toRaw, type UnwrapRef } from 'vue';

import type { ChatEmbed } from '@/types/chat';
import type { Channel, ChatMessage } from '@/types/moderation';
import SocketClient from '@/lib/clients/socketClient/SocketClient.ts';
import { user } from '@/store/user.ts';

const messagesByChannel = ref<Map<string, ChatMessage[]>>(new Map());
export const chatEmbeds = ref(new Map<string, ChatEmbed>());
const sockets = new Map<string, SocketClient>();

const getChatEmbed = (channelId: string): ChatEmbed | undefined => {
  return chatEmbeds.value.get(channelId) as ChatEmbed;
}

export const hasChatEmbed = (channelId: string): boolean => {
  return chatEmbeds.value.has(channelId);
}

export const addChatEmbed = async (channel: Channel) => {
  
  messagesByChannel.value.set(channel.broadcaster_id,
    channel.broadcaster.broadcaster_chat_messages || []);
  
  const socket = new SocketClient(
    'wss://api-modbot.nomercy.tv',
    user.value.access_token!,
    'chatHub'
  );

  await socket.setup();
  await socket.connection?.invoke('JoinChannel', channel.broadcaster_login);

  sockets.set(channel.broadcaster_id, socket);

  const chatEmbed: Partial<ChatEmbed> = {
    elementId: `chat-${channel.broadcaster_login}`,
    config: {
      width: '100%',
      height: '100%',
      channel: channel.broadcaster_login,
      channelId: channel.broadcaster_id,
      channelPoints: 0,
    },
    get messages() {
      return messagesByChannel.value.get(channel.broadcaster_id) || [];
    },
    sendMessage: async (message: string) => {
      if (!socket.isConnected()) return;
      await socket.connection?.invoke('SendMessage', {
        botUsername: channel.broadcaster_login,
        channelId: channel.broadcaster_id,
        message,
      });
    }
  };
  
  socket.connection?.on('ReceiveMessage', (message: ChatMessage) => {
    console.log('Received message', message);
    const messages = messagesByChannel.value.get(channel.broadcaster_id) || [];
    messagesByChannel.value.set(channel.broadcaster_id, [...messages, message]);
  });

  chatEmbeds.value = new Map(toRaw(chatEmbeds.value.set(channel.broadcaster_id,
    chatEmbed as UnwrapRef<ChatEmbed>)));
};

export const disposeChatEmbed = async (channelId: string) => {
  const socket = sockets.get(channelId);
  if (socket) {
    await socket.connection?.invoke('LeaveChannel', channelId);
    await socket.dispose();
    sockets.delete(channelId);
  }
  
  messagesByChannel.value.delete(channelId);
  messagesByChannel.value = messagesByChannel.value;
  
  chatEmbeds.value.delete(channelId);
  chatEmbeds.value = chatEmbeds.value;
}

export const updateChatEmbed = (channelId: string) => {
  const oldEmbed = getChatEmbed(channelId);
  if (!oldEmbed) return;

  const chatEmbed: ChatEmbed = {
    ...oldEmbed,
  };

  chatEmbeds.value.set(channelId, chatEmbed as UnwrapRef<ChatEmbed>);
}