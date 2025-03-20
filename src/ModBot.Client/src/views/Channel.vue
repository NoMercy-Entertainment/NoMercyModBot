<script setup lang="ts">
import useServerClient from '@/lib/clients/useServerClient.ts';
import type { Channel } from '@/types/moderation.ts';

const { data, error, isLoading } = useServerClient<Channel>();

</script>

<template>
  <div class="sticky top-0 text-neutral-900 dark:text-white h-min bg-neutral-100 dark:bg-neutral-800 pb-4"
       :style="`background-color: ${data?.broadcaster.color}`"
  >
    <template v-if="!isLoading && data">
      <div>
        <img class="h-32 w-full object-cover lg:h-48" v-if="data.broadcaster.offline_image_url" :src="data.broadcaster.offline_image_url" alt="" />
        <div class="h-32 w-full object-cover lg:h-48" v-else ></div>
      </div>
      <div class="mx-auto max-w-5xl px-4 sm:px-6 lg:px-8">
        <div class="-mt-12 sm:-mt-16 sm:flex sm:items-end sm:space-x-5">
          <div class="flex">
            <img class="size-24 rounded-full ring-4 ring-neutral-100 dark:ring-neutral-400 sm:size-32" :src="data.broadcaster.profile_image_url" alt="" />
          </div>
          <div class="mt-6 sm:flex sm:min-w-0 sm:flex-1 sm:items-center sm:justify-end sm:space-x-6 sm:pb-1">
            <div class="mt-6 min-w-0 flex-1 sm:hidden md:block">
              <h1 class="truncate text-3xl font-bold pl-1"
                  :style="`text-shadow: 
                     -1px -1px 1px hsl(from ${data.broadcaster.color} h s calc(l * 0.5)), 1px -1px 1px hsl(from ${data.broadcaster.color} h s calc(l * 0.5)), -1px 1px 1px hsl(from ${data.broadcaster.color} h s calc(l * 0.5)), 1px 1px 1px hsl(from ${data.broadcaster.color} h s calc(l * 0.5)),
                     -2px -2px 2px hsl(from ${data.broadcaster.color} h s calc(l * 0.5)), 2px -2px 2px hsl(from ${data.broadcaster.color} h s calc(l * 0.5)), -2px 2px 2px hsl(from ${data.broadcaster.color} h s calc(l * 0.5)), 2px 2px 2px hsl(from ${data.broadcaster.color} h s calc(l * 0.5))
                  `"
              >
                {{ data.broadcaster_name }}
              </h1>
            </div>
            <div class="mt-6 flex flex-col justify-stretch space-y-3 sm:flex-row sm:space-y-0 sm:space-x-4">
<!--              <button type="button" class="inline-flex justify-center rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 ring-1 shadow-xs ring-gray-300 ring-inset hover:bg-gray-50">-->
<!--                <EnvelopeIcon class="mr-1.5 -ml-0.5 size-5 text-gray-400" aria-hidden="true" />-->
<!--                <span>Message</span>-->
<!--              </button>-->
<!--              <button type="button" class="inline-flex justify-center rounded-md bg-white px-3 py-2 text-sm font-semibold text-gray-900 ring-1 shadow-xs ring-gray-300 ring-inset hover:bg-gray-50">-->
<!--                <PhoneIcon class="mr-1.5 -ml-0.5 size-5 text-gray-400" aria-hidden="true" />-->
<!--                <span>Call</span>-->
<!--              </button>-->
            </div>
          </div>
        </div>
        <div class="mt-6 hidden min-w-0 flex-1 sm:block md:hidden">
          <h1 class="truncate text-2xl font-bold">
            {{ data.broadcaster_name }}
          </h1>
        </div>
      </div>
    </template>
  </div>
</template>
