<script setup lang="ts">
import { defineComponent, type PropType } from 'vue';

import { twMerge } from 'tailwind-merge';
import { MoooomIcons } from '@Icons/icons';

import icons from '/resources/mooooom-icons.svg';

defineProps({
  icon: {
    type: String as PropType<keyof typeof MoooomIcons>,
    required: true
  },
  className: {
    type: String,
    required: false,
    default: ''
  }
});

defineComponent({
  name: 'MoooomIcon',
  props: {
    icon: {
      type: String as PropType<MoooomIcons>,
      required: true
    }
  }
});

</script>

<template>
  <svg :class="twMerge('w-6 h-auto aspect-square flex-shrink-0 flex pointer-events-none', className)">
    <use :xlink:href="`${icons}#${icon}`"></use>
  </svg>
</template>
