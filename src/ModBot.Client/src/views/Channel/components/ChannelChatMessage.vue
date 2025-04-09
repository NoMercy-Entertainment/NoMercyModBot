<script setup lang="ts">
import { computed, onMounted, type PropType, ref } from 'vue';
import type { BadgeSet, ChatEmoteSet, ChatMessage, EmoteSet } from '@/types/moderation.ts';

const props = defineProps({
  message: {
    type: Object as () => ChatMessage,
    required: true
  },
  badges: {
    type: Object as () => BadgeSet,
    required: false,
    default: () => []
  },
  emotes: {
    type: Array as () => EmoteSet[],
    required: false,
    default: () => []
  },
  scrollToBottom: {
    type: Function as PropType<(force?: boolean) => void>,
    required: false,
    default: () => {
    }
  }
});

const elementRef = ref();

const formatTimestamp = computed(() => {
  try {
    const date = new Date(Number(props.message.timestamp || props.message.tmiSentTs));
    if (isNaN(date.getTime())) {
      return '';
    }

    const diff = (Date.now() - date.getTime()) / 1000;
    const rtf = new Intl.RelativeTimeFormat('nl-NL', {
      numeric: 'auto'
    });

    if (diff < 60) {
      return rtf.format(-Math.round(diff), 'second');
    } else if (diff < 3600) {
      return rtf.format(-Math.round(diff / 60), 'minute');
    } else if (diff < 86400) {
      return rtf.format(-Math.round(diff / 3600), 'hour');
    } else {
      return rtf.format(-Math.round(diff / 86400), 'day');
    }
  } catch {
    return '';
  }
});
const badgeUrl = (badge: { key: string, value: string }) => {
  const badgeSet = props.badges[badge.key];
  let version = badgeSet[badge.value];
  
  return version?.image_url_2x;
};
const messageParser = (message: string, emotes: ChatEmoteSet) => {
  if (!emotes) return message;

  // Create a StringInfo-like structure for handling UTF-16 characters
  const text = Array.from(message);

  // Sort emotes by start index in reverse order to handle overlapping
  const replacements = emotes.Emotes.map((emote) => {
    // Get the actual substring using array positions
    const emoteName = text.slice(emote.StartIndex, emote.EndIndex + 1).join('');

    return {
      start: emote.StartIndex,
      end: emote.EndIndex,
      placeholder: `__EMOTE_${emote.Id}__`,
      html: `<img src="https://static-cdn.jtvnw.net/emoticons/v2/${emote.Id}/default/dark/1.0" class="h-fit" alt="${emoteName}"/>`
    };
  }).sort((a, b) => b.start - a.start);

  // Replace text with placeholders
  let processedMessage = message;
  for (const { start, end, placeholder } of replacements) {
    const before = Array.from(processedMessage).slice(0, start).join('');
    const after = Array.from(processedMessage).slice(end + 1).join('');
    processedMessage = before + placeholder + after;
  }

  // Replace placeholders with HTML
  for (const { placeholder, html } of replacements) {
    processedMessage = processedMessage.replace(placeholder, html);
  }

  return processedMessage;
};

const parsedMessage = computed(() => {
  return messageParser(props.message.message, props.message.emoteSet);
});

onMounted(() => {
  props.scrollToBottom();
});

</script>

<template>
  <div ref="elementRef" class="w-available flex flex-col first:mt-auto even:bg-neutral-800/50 odd:bg-neutral-700/50">
    <div class="flex items-start space-x-2 p-2 rounded transition-colors">
      <div class="flex-1 flex flex-col">
        <div class="flex items-center gap-1">
          <div v-if="message.badges && message.badges.length" class="flex gap-1">
            <img v-for="badge in message.badges"
                 :key="badge.key"
                 :src="badgeUrl(badge)"
                 class="w-4 h-4"
                 alt="chat badge">
          </div>

          <span class="font-medium" :style="{ 
            color: message.colorHex || '#9146FF' 
          }">
            {{ message.displayName }}
          </span>
          <span class="text-neutral-400">:</span>

          <span class="ml-auto text-xs text-gray-500 whitespace-nowrap">
              {{ formatTimestamp }}
            </span>
        </div>

        <div class="text-neutral-100 flex flex-wrap gap-0.5 items-center w-fit" v-html="parsedMessage"
             :class="{ 
                'bg-[var(--theme)] p-1 leading-none': message.isHighlighted, 
                }"
        ></div>
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
::-webkit-scrollbar {
  @apply w-1.5;
}

::-webkit-scrollbar-track {
  @apply bg-transparent;
}

::-webkit-scrollbar-thumb {
  @apply bg-white/20 rounded-sm hover:bg-white/30;
}
</style>