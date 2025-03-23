<script setup lang="ts">
import { computed } from 'vue';

import { PlayIcon, XMarkIcon } from '@heroicons/vue/20/solid';

import type { Channel } from '@/types/moderation.ts';

import { addPlayerEmbed, disposePlayerEmbed, hasPlayerEmbed } from '@/store/player.ts';
import { addChatEmbed, disposeChatEmbed, hasChatEmbed } from '@/store/chat.ts';

import ChannelImage from '@/views/Channel/components/ChannelImage.vue';
import ChannelHeaderButton from '@/views/Channel/components/ChannelHeaderButton.vue';

const props = defineProps({
  channel: {
    type: Object as () => Channel,
    required: true,
  },
});

const createPlayer = () => {
  addPlayerEmbed(props.channel);
}

const killPlayer = () => {
  disposePlayerEmbed(props.channel.id);
}

const playerVisible = computed(() => {
  return hasPlayerEmbed(props.channel.id);
});

const createChat = () => {
  addChatEmbed(props.channel);
}

const killChat = () => {
  disposeChatEmbed(props.channel.id);
}

const chatVisible = computed(() => {
  return hasChatEmbed(props.channel.id);
});

</script>

<template>

  <div class="sticky -top-48 z-9999 text-neutral-900 dark:text-white h-[17rem] bg-neutral-100 dark:bg-neutral-800"
       :style="`background-color: hsl(from ${channel?.broadcaster.color} h calc(s / 1.5) calc(l / 1))`"
  >
    <template v-if="channel">
      <div class="relative">
        <img class="h-46 sm:h-32 w-full object-cover lg:h-48" v-if="channel.broadcaster.offline_image_url"
             :src="channel.broadcaster.offline_image_url" alt=""/>
        <div class="h-46 sm:h-32 w-full object-cover lg:h-48" v-else
             :style="`background-color: hsl(from ${channel?.broadcaster.color} h calc(s / 1.5) calc(l / 1.5))`"></div>
      </div>
      
      <div id="channel-img-container" class="w-full flex flex-nowrap gap-4 items-end px-4 sm:px-6 lg:px-8 transition-all duration-500 h-16 sm:h-32 absolute bottom-0 mb-4">

        <ChannelImage :channel="channel" size="size-24 sm:size-32" />
        
          <div class="flex flex-nowrap sm:flex sm:min-w-0 sm:flex-1 sm:items-center sm:justify-end sm:space-x-6 sm:pb-0">
            <div class="min-w-0 flex-1 sm:hidden md:flex items-center gap-4 justify-between">
              <h1 id="channel-name" class="truncate text-4xl font-bold p-1.5 leading-none transition-all duration-100"
                  :style="`text-shadow: 
                     -1px -1px 5px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), 1px -1px 5px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), -1px 1px 5px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), 1px 1px 5px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)),
                     -2px -2px 5px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), 2px -2px 5px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), -2px 2px 5px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5)), 2px 2px 5px hsl(from ${channel.broadcaster.color} h s calc(l * 0.5))
                  `"
              >
                {{ channel.broadcaster_name }}
              </h1>
              <div class="flex items-center justify-center space-x-3 sm:space-y-0 sm:space-x-4">
                <ChannelHeaderButton v-if="playerVisible" @click="killPlayer">
                  <XMarkIcon class="size-5 text-gray-400" aria-hidden="true" />
                  <span>Hide Player</span>
                </ChannelHeaderButton>
                <ChannelHeaderButton v-else @click="createPlayer">
                  <PlayIcon class="size-5 text-gray-400" aria-hidden="true" />
                  <span>Show Player</span>
                </ChannelHeaderButton>
                <ChannelHeaderButton v-if="chatVisible" @click="killChat">
                  <XMarkIcon class="size-5 text-gray-400" aria-hidden="true" />
                  <span>Hide Chat</span>
                </ChannelHeaderButton>
                <ChannelHeaderButton v-else @click="createChat">
                  <PlayIcon class="size-5 text-gray-400" aria-hidden="true" />
                  <span>Show Chat</span>
                </ChannelHeaderButton>
              </div>
            </div>
          </div>
        
      </div>
    </template>
  </div>
</template>

<style scoped lang="scss">
.sticky {
  container-type: scroll-state;
  
  #channel-name {
    @container scroll-state(stuck: top) {
      @media screen and (max-device-width: 400px) {
        font-size: var(--text-2xl);
        line-height: var(--tw-leading, var(--text-2xl--line-height));
      }
    }
  }

  #channel-img-container {

    transition-property: transform;
    transition-duration: 500ms;
    transition-timing-function: ease-in-out;
    transition-delay: 0ms;
    transform-origin: bottom right;
    
    @container scroll-state(stuck: top) {
      margin-bottom: 0.75rem;
    }
  }
}
</style>
