export interface User {
  id: string;
  username: string;
  email: string;
  avatar?: string;
  status?: UserStatus;
  createdAt: Date;
  lastSeen?: Date;
  isOnline: boolean;
}

export enum UserStatus {
  ONLINE = 'online',
  OFFLINE = 'offline',
  AWAY = 'away',
  DO_NOT_DISTURB = 'do_not_disturb'
}

export interface UserSettings {
  theme: 'light' | 'dark';
  language: string;
  notifications: boolean;
  sound: boolean;
}

export interface UserProfile extends User {
  settings: UserSettings;
  friends: string[]; // Array of user IDs
  friendRequests: string[]; // Array of user IDs
  blockedUsers: string[]; // Array of user IDs
  bio?: string;
  customStatus?: string;
} 