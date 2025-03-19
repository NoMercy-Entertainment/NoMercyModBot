export interface User {
    accessToken: string;
    refreshToken: string;
    tokenExpiry: string;
    id: string; 
    username: string;
    displayName: string;
    profileImageUrl: string;
    locale: string | null;
}