export interface UserResponse {
    id: string;
    username: string;
    email: string;
    avatar?: string;
    friendStatus?: 'none' | 'pending' | 'accepted' | 'requested';
}

export interface FriendRequest {
    id: string;
    senderId: string;
    receiverId: string;
    status: 'pending' | 'accepted' | 'rejected';
    createdAt: string;
    sender?: UserResponse;
    receiver?: UserResponse;
} 