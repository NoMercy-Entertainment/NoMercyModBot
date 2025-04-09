<script setup lang="ts">
import { type PropType, ref } from 'vue';
import { 
  CogIcon, 
  GiftIcon, 
  PaperAirplaneIcon, 
} from '@heroicons/vue/20/solid';

import type { ChannelEmbedConfig } from '@/types/chat.ts';

import ChatPopup from '@/views/Channel/components/ChannelChatPopup.vue';

defineProps({
  config: {
    type: Object as PropType<ChannelEmbedConfig>,
    required: true,
  },
});

const message = ref('');
const showConfig = ref(false);
const showPoints = ref(false);

const emit = defineEmits<{
  (event: 'send', message: string): void
}>();

const sendMessage = () => {
  if (!message.value.trim()) return;
  emit('send', message.value);
  message.value = '';
};

const toggleConfig = () => {
  showConfig.value = !showConfig.value;
  showPoints.value = false;
};

const togglePoints = () => {
  showPoints.value = !showPoints.value;
  showConfig.value = false;
};
</script>

<template>
  <div class="flex flex-col w-available mx-2 sm:mx-0">
    <!-- Popups -->
    <ChatPopup
      :show="showConfig"
      :title="$t('chat.settings')"
      @close="showConfig = false"
    >
      <!-- Add config content here -->
    </ChatPopup>

    <ChatPopup
      :show="showPoints"
      :title="$t('chat.channelPoints')"
      @close="showPoints = false"
    >
      <!-- Add points content here -->
    </ChatPopup>

    <!-- Input -->
    <input
      v-model="message"
      type="text"
      @keyup.enter="sendMessage"
      class="w-full bg-neutral-800 text-white rounded-md px-4 py-2 mb-2 sm:mb-0
             placeholder-neutral-400 border border-neutral-700
             focus:outline-none focus:ring-2 focus:ring-neutral-600
             hover:border-neutral-600 transition-colors z-0"
      :placeholder="$t('chat.placeholder')"
    />

    <!-- Actions -->
    <div class="flex justify-between items-center gap-2 z-50 bg-neutral-900 w-available -mx-2 pb-2 sm:-mb-2 px-2 sm:p-2"
         :class="{
            'brightness-70': showConfig || showPoints,
         }"
    >
      <div class="flex gap-2 w-available">
        <button
          @click="togglePoints"
          class="flex items-center gap-2 px-1.5 py-1 bg-neutral-800 text-neutral-400
                 hover:bg-neutral-700 hover:text-white rounded-md
                 transition-colors"
        >
          <GiftIcon class="size-5" />
          <span>{{ config.channelPoints }}</span>
        </button>
        <button
          @click="toggleConfig"
          class="ml-auto px-1.5 py-1 bg-neutral-800 text-neutral-400
                 hover:bg-neutral-700 hover:text-white rounded-md
                 transition-colors"
        >
          <CogIcon class="size-5" />
        </button>
      </div>

      <button
        @click="sendMessage"
        class="flex items-center gap-2 px-1.5 py-1 bg-neutral-800 text-neutral-400
               hover:bg-neutral-700 hover:text-white rounded-md
               transition-colors disabled:opacity-50
               disabled:cursor-not-allowed"
        :disabled="!message.trim()"
      >
        <span>{{ $t('chat.send') }}</span>
        <PaperAirplaneIcon class="size-5" />
      </button>
    </div>
  </div>
</template>