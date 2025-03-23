<script setup lang="ts">

import AppLogoSquare from '@/components/icons/AppLogoSquare.vue';
import Backdrop from '@/components/Backdrop.vue';

</script>

<template>
  <div class="flex h-screen items-center justify-center relative overflow-hidden isolate">
    <Backdrop />
    <div class="w-available max-w-lg mx-4 z-10 space-y-8 rounded-lg bg-neutral-900 p-8 shadow-lg">
      <div class="text-center flex flex-col gap-6 justify-center items-center">
        <AppLogoSquare class="mx-auto w-24 h-auto" />
        <div class="text-center flex flex-col gap-4 justify-center items-center">
          <slot />
        </div>
      </div>
    </div>
  </div>
</template>