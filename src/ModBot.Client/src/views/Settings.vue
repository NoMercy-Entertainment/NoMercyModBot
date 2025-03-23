<script setup lang="ts">
import { computed, ref } from 'vue';
import { useTranslation } from 'i18next-vue';
import { Dialog, DialogPanel, DialogTitle, TransitionChild, TransitionRoot } from '@headlessui/vue';

import { MagnifyingGlassIcon, UserIcon, BellIcon, ExclamationTriangleIcon } from '@heroicons/vue/20/solid';
import { ChevronDownIcon } from '@heroicons/vue/16/solid';

import { timezones } from '@/config/timezones.ts';
import authService from '@/services/authService.ts';
import { user } from '@/store/user.ts';
import router from '@/router';

const { t } = useTranslation();


const deleteConfirmDialogOpen = ref(false);
const currentTab = ref('Account');

const secondaryNavigation = computed(() => [
  { name: t('settings.tabs.account'), icon: UserIcon },
  { name: t('settings.tabs.notifications'), icon: BellIcon }
]);

const handleDeleteAccount = async () => {
  deleteConfirmDialogOpen.value = true;
};

const confirmDelete = async () => {
  await authService.deleteAccount();
  deleteConfirmDialogOpen.value = false;
  await router.replace({ name: 'Login' });
};

</script>

<template>

  <div
    class="sticky top-0 z-40 flex h-16 shrink-0 items-center gap-x-6 border-b border-white/5 bg-neutral-900/50 px-4 shadow-xs sm:px-6 lg:px-8">
    <div class="flex flex-1 gap-x-4 self-stretch lg:gap-x-6">
      <form class="grid flex-1 grid-cols-1" action="#" method="GET">
        <input type="search" name="search" aria-label="Search"
               class="col-start-1 row-start-1 block size-full bg-transparent pl-8 text-base text-white outline-hidden placeholder:text-neutral-300 sm:text-sm"
               placeholder="Search" />
        <MagnifyingGlassIcon class="pointer-events-none col-start-1 row-start-1 size-5 self-center text-neutral-300"
                             aria-hidden="true" />
      </form>
    </div>
  </div>

  <div class="h-inherit flex flex-col mb-auto">
    <h1 class="sr-only">{{ $t('settings.title') }}</h1>

    <header class="border-b border-white/5">
      <!-- Secondary navigation -->
      <nav class="flex overflow-x-auto py-4 w-full">
        <div role="list"
             class="flex min-w-full flex-none gap-x-6 px-4 text-sm/6 font-semibold text-neutral-400 sm:px-6 lg:px-8">
          <button v-for="item in secondaryNavigation" :key="item.name"
                  @click="currentTab = item.name"
                  :class="[currentTab == item.name 
                ? 'border-theme-500 text-theme-600' 
                : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-400', 
                'group inline-flex items-center border-b-2 px-1 py-4 text-sm font-medium transition-colors duration-100']"
                  :aria-current="currentTab ? 'page' : undefined">

            <component :is="item.icon"
                       :class="[currentTab == item.name 
                          ? 'text-theme-500' 
                          : 'text-gray-500 group-hover:text-gray-400', 
                          'mr-2 -ml-0.5 size-5 transition-colors duration-100'
                       ]"
                       aria-hidden="true" />

            {{ item.name }}
          </button>
        </div>
      </nav>
    </header>

    <template v-if="currentTab == 'Account'">
      <div class="divide-y divide-white/5 h-full">
        <div class="grid max-w-7xl grid-cols-1 gap-x-8 gap-y-10 px-4 py-16 sm:px-6 md:grid-cols-3 lg:px-8">
          <div>
            <h2 class="text-base/7 font-semibold text-white">
              {{ $t('settings.personal.title') }}
            </h2>
            <p class="mt-1 text-sm/6 text-neutral-400">
              {{ $t('settings.personal.subtitle') }}
            </p>
          </div>

          <div class="md:col-span-2">
            <div class="grid grid-cols-1 gap-x-6 gap-y-8 sm:max-w-xl sm:grid-cols-6">
              <div class="col-span-full flex items-center gap-x-8">
                <img :src="user.profile_image_url" alt=""
                     class="size-24 flex-none rounded-lg bg-neutral-800 object-cover" />
              </div>

              <div class="sm:col-span-3">
                <label for="display-name" class="block text-sm/6 font-medium text-white">
                  {{ $t('settings.personal.displayName') }}
                </label>
                <div class="mt-2">
                  <input disabled type="text" name="display-name" id="display-name" autocomplete="display-name"
                         class="block w-full rounded-md bg-white/5 px-3 py-1.5 text-base text-white outline-1 -outline-offset-1 outline-white/10 placeholder:text-neutral-500 focus:outline-2 focus:-outline-offset-2 focus:outline-theme-500 sm:text-sm/6"
                         :value="user.display_name" />
                </div>
              </div>

              <div class="sm:col-span-3">
                <label for="username" class="block text-sm/6 font-medium text-white">
                  {{ $t('settings.personal.username') }}
                </label>
                <div class="mt-2">
                  <div
                    class="flex items-center rounded-md bg-white/5 pl-3 outline-1 -outline-offset-1 outline-white/10 focus-within:outline-2 focus-within:-outline-offset-2 focus-within:outline-theme-500">
                    <div class="shrink-0 text-base text-neutral-500 select-none sm:text-sm/6">twitch.tv/</div>
                    <input disabled type="text" name="username" id="username"
                           class="block min-w-0 grow bg-transparent py-1.5 pr-3 pl-1 text-base text-white placeholder:text-neutral-500 focus:outline-none sm:text-sm/6"
                           :value="user.username" />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="grid max-w-7xl grid-cols-1 gap-x-8 gap-y-10 px-4 py-16 sm:px-6 md:grid-cols-3 lg:px-8">
          <div>
            <h2 class="text-base/7 font-semibold text-white">
              {{ $t('settings.display.title') }}
            </h2>
            <p class="mt-1 text-sm/6 text-neutral-400">
              {{ $t('settings.display.subtitle') }}
            </p>
          </div>

          <form class="md:col-span-2">
            <div class="grid grid-cols-1 gap-x-6 gap-y-8 sm:max-w-xl sm:grid-cols-6">

              <div class="col-span-full">
                <label for="timezone" class="block text-sm/6 font-medium text-white">
                  {{ $t('settings.display.timezone') }}
                </label>
                <div class="mt-2 grid grid-cols-1">
                  <select id="timezone" name="timezone"
                          class="col-start-1 row-start-1 w-full appearance-none rounded-md bg-white/5 py-1.5 pr-8 pl-3 text-base text-white outline-1 -outline-offset-1 outline-white/10 *:bg-neutral-800 focus:outline-2 focus:-outline-offset-2 focus:outline-theme-500 sm:text-sm/6">
                    <template v-for="timezone in timezones" :key="timezone">
                      <option :value="timezone" :selected="timezone.timezone == user.timezone">
                        {{ timezone.name }}
                      </option>
                    </template>

                  </select>
                  <ChevronDownIcon
                    class="pointer-events-none col-start-1 row-start-1 mr-2 size-5 self-center justify-self-end text-neutral-400 sm:size-4"
                    aria-hidden="true" />
                </div>
              </div>
            </div>

            <div class="mt-8 flex">
              <button type="submit"
                      class="rounded-md bg-theme-700 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-theme-600 active:bg-theme-800 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-theme-600">
                {{ $t('settings.display.save') }}
              </button>
            </div>
          </form>
        </div>

        <div class="grid max-w-7xl grid-cols-1 gap-x-8 gap-y-10 px-4 py-16 sm:px-6 md:grid-cols-3 lg:px-8">
          <div>
            <h2 class="text-base/7 font-semibold text-white">
              {{ $t('settings.delete.title') }}
            </h2>
            <p class="mt-1 text-sm/6 text-neutral-400">
              {{ $t('settings.delete.subtitle') }}</p>
          </div>

          <div class="flex items-start md:col-span-2">
            <button @click="handleDeleteAccount"
                    class="rounded-md bg-red-700 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-red-600 active:bg-red-800">
              {{ $t('settings.delete.button') }}
            </button>
          </div>
        </div>
      </div>
    </template>
  </div>

  <TransitionRoot as="template" :show="deleteConfirmDialogOpen">
    <Dialog class="relative z-10" @close="deleteConfirmDialogOpen = false">
      <!-- ... existing transition child ... -->

      <div class="fixed inset-0 z-10 w-screen overflow-y-auto">
        <div class="flex min-h-full items-end justify-center p-4 text-center sm:items-center sm:p-0">
          <TransitionChild as="template" enter="ease-out duration-300"
                           enter-from="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95"
                           enter-to="opacity-100 translate-y-0 sm:scale-100" leave="ease-in duration-200"
                           leave-from="opacity-100 translate-y-0 sm:scale-100"
                           leave-to="opacity-0 translate-y-4 sm:translate-y-0 sm:scale-95">
            <DialogPanel
              class="relative transform overflow-hidden rounded-lg bg-neutral-900 px-4 pb-4 pt-5 text-left shadow-xl transition-all sm:my-8 sm:w-full sm:max-w-lg sm:p-6">
              <div class="sm:flex sm:items-start">
                <div
                  class="mx-auto flex size-12 shrink-0 items-center justify-center rounded-full bg-red-900/20 sm:mx-0 sm:size-10">
                  <ExclamationTriangleIcon class="size-6 text-red-600" aria-hidden="true" />
                </div>
                <div class="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left">
                  <DialogTitle as="h3" class="text-base font-semibold text-white">
                    {{ $t('settings.delete.title') }}
                  </DialogTitle>
                  <div class="mt-2">
                    <p class="text-sm text-neutral-400 whitespace-pre-wrap">
                      {{ $t('settings.dialog.subtitle') }}
                    </p>
                  </div>
                </div>
              </div>
              <div class="mt-5 sm:mt-4 sm:flex sm:flex-row-reverse">
                <button type="button"
                        @click="confirmDelete"
                        class="inline-flex w-full justify-center rounded-md bg-red-700 px-3 py-2 text-sm font-semibold text-white shadow-xs hover:bg-red-600 active:bg-red-800 sm:ml-3 sm:w-auto">
                  {{ $t('settings.delete.button') }}
                </button>
                <button type="button"
                        @click="deleteConfirmDialogOpen = false"
                        class="mt-3 inline-flex w-full justify-center rounded-md bg-white/10 px-3 py-2 text-sm font-semibold text-white shadow-xs ring-1 ring-inset ring-white/10 hover:bg-white/20 sm:mt-0 sm:w-auto">
                  {{ $t('settings.dialog.cancel') }}
                </button>
              </div>
            </DialogPanel>
          </TransitionChild>
        </div>
      </div>
    </Dialog>
  </TransitionRoot>
</template>

<style scoped>

</style>