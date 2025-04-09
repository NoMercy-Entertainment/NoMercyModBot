<script setup lang="ts">
import { XMarkIcon } from '@heroicons/vue/20/solid';

defineProps<{
  show: boolean;
  title: string;
}>();

defineEmits<{
  (event: 'close'): void;
}>();
</script>

<template>
    <div
         class="absolute inset-0 bg-black/25 transition-all duration-100 z-10"
         :class="{
            'opacity-0 translate-y-[200%] inert pointer-events-none': !show,
            '': show,
         }"
         @click="$emit('close')" />
      <div
           class="absolute inset-x-2 bottom-[calc(0%+3rem)] w-available h-auto bg-neutral-900 rounded-md border border-neutral-700 overflow-hidden transform transition-all duration-100 z-20"
           :class="{
                'opacity-0 translate-y-[200%] inert pointer-events-none': !show,
                '': show,  
           }"
      >
        <div class="flex items-center justify-between p-2 border-b border-neutral-700">
          <h3 class="text-white text-lg font-medium text-center flex-1">
            {{ title }}
          </h3>
          <button @click="$emit('close')"
                  class="text-neutral-400 hover:text-white transition-colors">
            <XMarkIcon class="size-5" />
          </button>
        </div>
        <div class="p-4 h-[calc(100%-4rem)] overflow-y-auto">
          <slot />
        </div>
      </div>
</template>

<style scoped>
.transform {
  transform-origin: bottom;
}
</style>