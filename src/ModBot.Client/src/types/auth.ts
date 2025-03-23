import type { ChatMessage } from '@/types/moderation.ts';

export interface User {
  access_token?: string;
  refresh_token?: string;
  token_expiry?: string;
  id: string;
  username: string;
  display_name: string;
  profile_image_url: string;
  offline_image_url: string;
  color: string;
  locale: string | null;
  timezone: string | null;
  link: string;
  enabled: boolean;
  broadcaster_chat_messages: ChatMessage[];
  moderator_chat_messages: ChatMessage[];
}