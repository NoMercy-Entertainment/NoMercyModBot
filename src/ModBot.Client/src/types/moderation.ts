import type { User } from '@/types/auth.ts';

export interface Channel {
  id: number,
  broadcaster_login: string,
  broadcaster_name: string,
  broadcaster_id: string,
  moderator_id: string,
  enabled: boolean,
  is_live: boolean,
  link: string,
  broadcaster: User,
  moderator: User
}