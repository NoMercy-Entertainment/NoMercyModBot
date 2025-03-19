import { ref, watch } from 'vue';

import authService from '@/services/authService';
import router from "@/router";

import type { User } from '@/types/auth';

export const user = ref<User>(<User>{
    accessToken: localStorage.getItem('access_token') || '',
    refreshToken: localStorage.getItem('refresh_token') || '',
    tokenExpiry: localStorage.getItem('tokenExpiry') || '',
    username: localStorage.getItem('username') || '',
    displayName: localStorage.getItem('display_name') || '',
    profileImageUrl: localStorage.getItem('profile_image_url') || ''
});

export const isInitialized = ref(false);

watch(user, (newUser) => {
    if (!newUser.accessToken) return;

    localStorage.setItem('access_token', newUser.accessToken);
    localStorage.setItem('refresh_token', newUser.refreshToken);
    localStorage.setItem('tokenExpiry', newUser.tokenExpiry);
    localStorage.setItem('username', newUser.username);
    localStorage.setItem('display_name', newUser.displayName);
    localStorage.setItem('profile_image_url', newUser.profileImageUrl);
});

export const storeTwitchUser = (twitchUser: User) => {
    user.value = twitchUser;
}

export const clearUserSession = () => {
    user.value = {} as User;
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('tokenExpiry');
    localStorage.removeItem('username');
    localStorage.removeItem('display_name');
    localStorage.removeItem('profile_image_url');
}

export const initializeUserSession = async () => {
    try {
        if (!user.value.accessToken) {
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
}