import axios from 'axios';

import type {User} from "@/types/auth.ts";

import serverClient from '@/lib/clients/serverClient.ts';
import { user } from '@/store/user.ts';

class AuthService {
    private refreshTimer: number | null = null;
    private readonly REFRESH_BUFFER = 5 * 60 * 1000;

    private scheduleTokenRefresh(expiryTime: string) {
        if (this.refreshTimer) {
            window.clearTimeout(this.refreshTimer);
        }

        const expiry = new Date(expiryTime).getTime();
        const now = Date.now();
        const timeUntilRefresh = expiry - now - this.REFRESH_BUFFER;

        if (timeUntilRefresh > 0) {
            this.refreshTimer = window.setTimeout(() => this.refreshToken(), timeUntilRefresh);
        }
    }

    async authorize() {
        const response = await serverClient().get('auth/authorize');
        return response.data;
    }

    async pollToken(deviceCode: string) {
        const response = await axios.post('auth/poll', {
            device_code: deviceCode
        });
        return response.data;
    }

    async callback(code: string): Promise<{ user: User }> {
        const response = await serverClient().get<{ user: User }>('auth/callback', {
            params: { code }
        });

        if (response.data.user.tokenExpiry) {
            this.scheduleTokenRefresh(response.data.user.tokenExpiry);
        }

        return response.data;
    }

    async validateSession(): Promise<{ user: User }> {
        const response = await serverClient().get<{ user: User }>('auth/validate', {
            headers: {
                Authorization: `Bearer ${user.value.accessToken}`
            }
        });

        if (user.value.tokenExpiry) {
            this.scheduleTokenRefresh(user.value.tokenExpiry);
        }

        return response.data;
    }

    async refreshToken(): Promise<{ user: User }> {
        const response = await serverClient().post<{ user: User }>('auth/refresh', {
            refreshToken: localStorage.getItem('refresh_token')
        });

        if (response.data.user.tokenExpiry) {
            this.scheduleTokenRefresh(response.data.user.tokenExpiry);
        }

        return response.data;
    }
}

export default new AuthService();