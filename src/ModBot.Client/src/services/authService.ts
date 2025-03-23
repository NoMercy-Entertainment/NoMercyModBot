import axios from 'axios';

import type { User } from '@/types/auth.ts';

import serverClient from '@/lib/clients/serverClient.ts';
import { clearUserSession, storeTwitchUser, user } from '@/store/user.ts';

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
      this.refreshTimer = window.setTimeout(async () => {
        const response = await this.refreshToken();
        
        storeTwitchUser(response.user);
        
        if (response.user.token_expiry) {
          this.scheduleTokenRefresh(response.user.token_expiry);
        }
      }, timeUntilRefresh);
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

    if (response.data.user.token_expiry) {
      this.scheduleTokenRefresh(response.data.user.token_expiry);
    }

    return response.data;
  }

  async validateSession(): Promise<{ user: User }> {
    const response = await serverClient().get<{ user: User }>('auth/validate', {
      headers: {
        Authorization: `Bearer ${user.value.access_token}`
      }
    });

    if (user.value.token_expiry) {
      this.scheduleTokenRefresh(user.value.token_expiry);
    }

    return response.data;
  }

  async refreshToken(): Promise<{ user: User }> {
    const response = await serverClient().post<{ user: User }>('auth/refresh', {
      refresh_token: localStorage.getItem('refresh_token')
    });

    return response.data;
  }

  async logout() {
    clearUserSession();
  }

  async deleteAccount() {
    await serverClient().delete('auth/account');
    clearUserSession();
  }
}

export default new AuthService();