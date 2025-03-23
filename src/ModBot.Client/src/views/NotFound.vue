<script setup lang="ts">
import { RouterLink } from 'vue-router';
import { routePaths } from '@/router/routes.ts';

</script>

<template>
  <main class="grid min-h-full place-items-center bg-white dark:bg-neutral-950 px-6 py-24 sm:py-32 lg:px-8">
    <div class="text-center">
      <p class="text-3xl font-semibold text-theme-600">404</p>
      <h1 class="mt-4 text-5xl font-semibold tracking-tight text-balance text-gray-900 dark:text-gray-100 sm:text-7xl">
        {{ $t('notFound.title') }}
      </h1>
      <p class="mt-6 text-lg font-medium text-pretty text-gray-500 dark:text-gray-400 sm:text-xl/8">
        {{ $t('notFound.message') }}
      </p>
      <div class="mt-10 flex items-center justify-center gap-x-6">
        <RouterLink :to="routePaths.home"
                    class="rounded-md bg-theme-600 px-3.5 py-2.5 text-sm font-semibold text-white shadow-xs hover:bg-indigo-500 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-indigo-600">
          {{ $t('notFound.goHome') }}
        </RouterLink>
        <RouterLink to="#" class="text-sm font-semibold text-gray-900 dark:text-gray-100">
          {{ $t('notFound.contactSupport') }}
          <span aria-hidden="true">&rarr;</span></RouterLink>
      </div>
    </div>
  </main>
</template>