export interface UserResponse {
    id: number;
    username: string;
    email: string;
    avatarUrl?: string;
    status?: string;
    bio?: string;
    isVerified: boolean;
    createdAt: string;
    lastLogin: string;
} 