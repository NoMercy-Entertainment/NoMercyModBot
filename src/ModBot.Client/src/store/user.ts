import { ref, watch } from 'vue';

import authService from '@/services/authService';
import router from '@/router';

import type { User } from '@/types/auth';

export const user = ref<User>(<User>{
  access_token: localStorage.getItem('access_token') || '',
  refresh_token: localStorage.getItem('refresh_token') || '',
  token_expiry: localStorage.getItem('tokenExpiry') || '',
  username: localStorage.getItem('username') || '',
  display_name: localStorage.getItem('display_name') || '',
  profile_image_url: localStorage.getItem('profile_image_url') || '',
  timezone: localStorage.getItem('timezone') || Intl.DateTimeFormat().resolvedOptions().timeZone,
  locale: localStorage.getItem('locale') || Intl.DateTimeFormat().resolvedOptions().locale
});

export const isInitialized = ref(false);

watch(user, (newUser) => {
  if (!newUser.access_token) return;

  localStorage.setItem('access_token', newUser.access_token);
  localStorage.setItem('refresh_token', newUser.refresh_token!);
  localStorage.setItem('tokenExpiry', newUser.token_expiry!);
  localStorage.setItem('username', newUser.username);
  localStorage.setItem('display_name', newUser.display_name);
  localStorage.setItem('profile_image_url', newUser.profile_image_url);
  localStorage.setItem('timezone', newUser.timezone || Intl.DateTimeFormat().resolvedOptions().timeZone);
  localStorage.setItem('locale', newUser.locale || Intl.DateTimeFormat().resolvedOptions().locale);
});

export const storeTwitchUser = (twitchUser: User) => {
  user.value = twitchUser;
};

export const clearUserSession = () => {
  user.value = {} as User;
  localStorage.removeItem('access_token');
  localStorage.removeItem('refresh_token');
  localStorage.removeItem('tokenExpiry');
  localStorage.removeItem('username');
  localStorage.removeItem('display_name');
  localStorage.removeItem('profile_image_url');
  localStorage.removeItem('timezone');
  localStorage.removeItem('locale');
};

export const initializeUserSession = async () => {
  try {
    if (!user.value.access_token) {
      return;
    }

    await authService.validateSession();
  } catch (error) {
    console.error(error);
    clearUserSession();
    await router.push({ name: 'Unauthenticated' });
  } finally {
    isInitialized.value = true;
  }
};