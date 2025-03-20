<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import authService from '@/services/authService';
import { storeTwitchUser, user } from '@/store/user.ts';

import LoadingScreen from '@/components/LoadingScreen.vue';

const router = useRouter();
const route = useRoute();
const isLoading = ref(false);
const errorMessage = ref('');

const startAuth = () => {
  if (isLoading.value) return;
  isLoading.value = true;
  window.location.href = '/api/auth/login';
};

const handleCallback = async () => {
  if (user.value.access_token) return;

  const code = route.query.code as string;
  const error = route.query.error as string;

  if (error) {
    errorMessage.value = error;
    await router.replace({ query: {} });
    return;
  }

  if (!code) return;

  try {
    isLoading.value = true;
    const response = await authService.callback(code);
    if (response.user) {
      storeTwitchUser(response.user);
      await router.replace({ name: 'Home' });
    }
  } catch (error: any) {
    errorMessage.value = error?.response?.data.message || 'Authentication failed';
    await router.replace({ name: 'Login' });
  } finally {
    isLoading.value = false;
  }
};

onMounted(handleCallback);

const isProcessingAuth = computed(() => {
  return route.name === 'Login' && (!!route.query.code || !!route.query.error);
});

</script>

<template>
  <LoadingScreen>
    <h2 class="text-3xl font-bold tracking-tight text-white">
      {{ $t('auth.welcome', { name: 'ModBot' }) }}
    </h2>
    <p class="mt-2 text-sm text-neutral-400">
      {{ $t('auth.subtitle') }}
    </p>
    <p class="mt-2 text-sm text-neutral-400">
      {{ isProcessingAuth
      ? $t('auth.login.processing')
      : $t('auth.login.pleaseSignIn') }}
    </p>

    <div v-if="errorMessage" class="mt-4 text-center">
      <div class="rounded-md bg-red-900/50 p-4">
        <div class="flex">
          <div class="flex-shrink-0">
            <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd"
                    d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.28 7.22a.75.75 0 00-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 101.06 1.06L10 11.06l1.72 1.72a.75.75 0 101.06-1.06L11.06 10l1.72-1.72a.75.75 0 00-1.06-1.06L10 8.94 8.28 7.22z"
                    clip-rule="evenodd" />
            </svg>
          </div>
          <div class="ml-3">
            <p class="text-sm font-medium text-red-400">
              {{ errorMessage }}
            </p>
          </div>
        </div>
      </div>
    </div>

    <div v-if="isLoading" class="mt-8 text-center">
      <div
        class="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-violet-400 border-r-transparent align-[-0.125em] motion-reduce:animate-[spin_1.5s_linear_infinite]"></div>
      <p class="mt-4 text-sm text-neutral-400">
        {{ $t('auth.login.connectingToTwitch') }}...</p>
    </div>

    <div v-if="!isProcessingAuth" class="mt-8 w-full">
      <button
        @click="startAuth"
        :disabled="isProcessingAuth"
        class="group relative flex w-full justify-center rounded-md bg-violet-600 px-4 py-3 text-sm font-semibold text-white hover:bg-violet-500 focus:outline-none focus:ring-2 focus:ring-violet-500 focus:ring-offset-2 disabled:opacity-50"
      >
        <span class="absolute inset-y-0 left-0 flex items-center pl-3">
          <svg class="h-5 w-5" viewBox="0 0 24 24" fill="currentColor">
            <path
              d="M11.571 4.714h1.715v5.143H11.57zm4.715 0H18v5.143h-1.714zM6 0L1.714 4.286v15.428h5.143V24l4.286-4.286h3.428L22.286 12V0zm14.571 11.143l-3.428 3.428h-3.429l-3 3v-3H6.857V1.714h13.714Z" />
          </svg>
        </span>
        <span>{{ $t('auth.login.connectWithTwitch') }}</span>
      </button>
    </div>
  </LoadingScreen>
</template>