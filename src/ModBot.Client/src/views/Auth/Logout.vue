<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';

import AppLogoSquare from '@/components/icons/icons/AppLogoSquare.vue';
import authService from '@/services/authService.ts';

const router = useRouter();
const isLoggingOut = ref(false);

const handleLogout = async () => {
  isLoggingOut.value = true;
  await authService.logout();
  await router.push({ name: 'Unauthenticated' });
};

const handleCancel = () => {
  router.back();
};
</script>

<template>
  <div
    class="absolute w-available max-w-lg h-min top-1/2 mx-4 -translate-y-1/2 space-y-8 place-self-center rounded-lg bg-neutral-800 p-8 shadow-lg">
    <div class="text-center flex flex-col gap-6 justify-center items-center">
      <AppLogoSquare class="mx-auto w-24 h-auto" />
      <div class="text-center flex flex-col gap-4 justify-center items-center">
        <h2 class="text-center text-2xl font-bold text-white">
          {{ $t('auth.logout.title') }}
        </h2>
        <p class="text-center text-neutral-400">
          {{ $t('auth.logout.message') }}
        </p>
        <div class="flex gap-4 justify-center">
          <button
            @click="handleCancel"
            class="cursor-pointer rounded-md bg-neutral-700 px-4 py-2 text-sm font-semibold text-white hover:bg-neutral-600"
          >
            {{ $t('auth.logout.cancel') }}
          </button>
          <button
            @click="handleLogout"
            class="cursor-pointer rounded-md bg-red-600 px-4 py-2 text-sm font-semibold text-white hover:bg-red-500"
          >
            {{ $t('auth.logout.confirm') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>

</style>