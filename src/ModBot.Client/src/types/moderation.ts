import type { User } from '@/types/auth.ts';

export interface Channel {
  id: string,
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


export interface ChatMessage {
  id: number;
  channel_id: number;
  user: User;
  colorHex: string;
  displayName: string;
  badges: {key: string; value: string}[];
  message: string;
  emoteSet: ChatEmoteSet;
  timestamp: string;
}

export interface ChatEmoteSet {
  Emotes: ChatEmote[];
  RawEmoteSetString: string;
}

export interface ChatEmote {
  Id: string;
  Name: string;
  StartIndex: number;
  EndIndex: number;
  ImageUrl: string;
}

export interface BadgeSet {
  set_id: string;
  versions: Badge[];
}

export interface Badge {
  id: string;
  image_url_1x: string;
  image_url_2x: string;
  image_url_4x: string;
}

export interface EmoteSet {
  id:        string;
  name:      string;
  images:    Images;
  format:    ['static'];
  scale:    ['1.0', '2.0', '3.0'];
  themeMode: ['light', 'dark'];
  isGlobal:  boolean;
}

export interface Images {
  url_1x: string;
  url_2x: string;
  url_4x: string;
}
