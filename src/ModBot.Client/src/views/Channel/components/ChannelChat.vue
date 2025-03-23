<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from 'vue';

import type { ChatEmbed } from '@/types/chat.ts';
import type { BadgeSet, EmoteSet } from '@/types/moderation.ts';

import useServerClient from '@/lib/clients/useServerClient.ts';
import { hasChatEmbed } from '@/store/chat.ts';
import { hasPlayerEmbed } from '@/store/player.ts';

import ChatMessage from '@/views/Channel/components/ChatMessage.vue';
import ChatInput from '@/views/Channel/components/ChatInput.vue';

const props = defineProps({
  chatEmbed: {
    type: Object as () => ChatEmbed,
    required: true
  }
});

const { data: badges } = useServerClient<BadgeSet[]>({
  enabled: !!props.chatEmbed.config.channelId,
  path: `/channels/${props.chatEmbed.config.channelId}/badges`
});

const { data: emotes } = useServerClient<EmoteSet[]>({
  enabled: !!props.chatEmbed.config.channelId,
  path: `/channels/${props.chatEmbed.config.channelId}/emotes`
});

const elementRef = ref<HTMLElement>();
const messagesRef = ref<HTMLElement>();
const isAtBottom = ref(true);
const scrollBlocked = ref(false);
const scrollToBottom = (force?: boolean) => {
  if (!messagesRef.value) return;
  
  if (force) {
    scrollBlocked.value = false;
  }

  nextTick(() => {
    if (scrollBlocked.value) return;
    messagesRef.value!.scrollTop = messagesRef.value!.scrollHeight;
  });
};

const handleScroll = () => {
  if (!messagesRef.value) return;

  const { scrollTop, scrollHeight, clientHeight } = messagesRef.value;
  isAtBottom.value = Math.abs(scrollHeight - clientHeight - scrollTop) < 10;
  
  scrollBlocked.value = Math.abs(scrollHeight - clientHeight - scrollTop) > 50;
};

const playerVisible = computed(() => {
  return hasPlayerEmbed(props.chatEmbed.config.channelId);
});

onMounted(() => {
  scrollToBottom();
  setInterval(() => {
    scrollToBottom();
  }, 500);
});
  
</script>

<template>
  <div ref="elementRef"
       v-if="badges"
       :id="props.chatEmbed.elementId"
       class="w-available h-[inherit] sm:w-[400px] sm:h-[640px] bg-neutral-900 flex flex-col gap-2 -m-2 sm:m-0 sm:p-2 relative overflow-y-clip overflow-x-clip"
       :class="{ 
           'basis-[55vh]': playerVisible,
           'basis-[85vh]': !playerVisible
         }"
  >
    <div ref="messagesRef"
         class="flex flex-col flex-1 space-y-2 overflow-y-auto overflow-x-clip w-available dark-scrollbar"
         @scroll="handleScroll">
      <ChatMessage v-for="message in props.chatEmbed.messages"
                   :key="message.id"
                   :badges="badges"
                   :emotes="emotes"
                   :scrollToBottom="scrollToBottom"
                   :message="message" />
    </div>
    
    <button v-if="scrollBlocked" 
            class="absolute z-50 bottom-28 p-2 rounded-md inset-x-1/2 -translate-x-1/2 w-40 bg-neutral-900 text-neutral-100" @click="() => scrollToBottom(true)">
      Scroll to bottom
    </button>
    
    <ChatInput
      :config="props.chatEmbed.config"
      @send="props.chatEmbed.sendMessage"
    />
  </div>
</template>