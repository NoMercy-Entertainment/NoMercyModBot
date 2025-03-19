<script setup lang="ts">
import { type PropType } from "vue";

import { twMerge } from "tailwind-merge";
import { PlayerIcons } from '@Icons/icons';

import icons from '/resources/player-icons.svg';

defineProps({
    icon: {
        type: String as PropType<keyof typeof PlayerIcons>,
        required: true,
    },
    className: {
        type: String,
        required: false,
        default: '',
    },
});

</script>

<template>
    <svg :class="twMerge('w-6 h-auto aspect-square flex-shrink-0 flex', className)">
        <use :xlink:href="`${icons}#${icon}`"></use>
    </svg>
</template>
