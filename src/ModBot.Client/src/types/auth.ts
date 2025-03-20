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
}