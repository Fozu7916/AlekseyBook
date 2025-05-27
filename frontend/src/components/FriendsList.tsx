import React, { useState, useEffect } from 'react';
import { userService } from '../services/userService';
import { User } from '../services/userService';
import './FriendsList.css';

const FriendsList: React.FC = () => {
    const [friends, setFriends] = useState<User[]>([]);
    const [pendingRequests, setPendingRequests] = useState<User[]>([]);
    const [sentRequests, setSentRequests] = useState<User[]>([]);

    useEffect(() => {
        loadFriends();
    }, []);

    const loadFriends = async () => {
        try {
            const response = await userService.getFriendsList();
            setFriends(response.friends);
            setPendingRequests(response.pendingRequests);
            setSentRequests(response.sentRequests);
        } catch (error) {
            console.error('Error loading friends:', error);
        }
    };

    const handleAcceptRequest = async (userId: number) => {
        try {
            await userService.acceptFriendRequest(userId);
            loadFriends();
        } catch (error) {
            console.error('Error accepting friend request:', error);
        }
    };

    const handleDeclineRequest = async (userId: number) => {
        try {
            await userService.declineFriendRequest(userId);
            loadFriends();
        } catch (error) {
            console.error('Error declining friend request:', error);
        }
    };

    const handleRemoveFriend = async (userId: number) => {
        try {
            await userService.removeFriend(userId);
            loadFriends();
        } catch (error) {
            console.error('Error removing friend:', error);
        }
    };

    return (
        <div className="friends-list-container">
            {pendingRequests.length > 0 && (
                <div className="friends-section">
                    <h2 className="section-title">Входящие заявки в друзья</h2>
                    <div className="friends-list">
                        {pendingRequests.map((user) => (
                            <div key={user.id} className="friend-item">
                                <div className="friend-avatar">
                                    {user.avatarUrl ? (
                                        <img src={user.avatarUrl} alt={user.username} />
                                    ) : (
                                        <div className="avatar-placeholder">
                                            {user.username[0].toUpperCase()}
                                        </div>
                                    )}
                                </div>
                                <div className="friend-info">
                                    <div className="friend-name">{user.username}</div>
                                    <div className="friend-status">{user.status || 'Нет статуса'}</div>
                                </div>
                                <div className="friend-actions">
                                    <button
                                        className="button accept-button"
                                        onClick={() => handleAcceptRequest(user.id)}
                                    >
                                        Принять
                                    </button>
                                    <button
                                        className="button decline-button"
                                        onClick={() => handleDeclineRequest(user.id)}
                                    >
                                        Отклонить
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {friends.length > 0 && (
                <div className="friends-section">
                    <h2 className="section-title">Друзья</h2>
                    <div className="friends-list">
                        {friends.map((user) => (
                            <div key={user.id} className="friend-item">
                                <div className="friend-avatar">
                                    {user.avatarUrl ? (
                                        <img src={user.avatarUrl} alt={user.username} />
                                    ) : (
                                        <div className="avatar-placeholder">
                                            {user.username[0].toUpperCase()}
                                        </div>
                                    )}
                                </div>
                                <div className="friend-info">
                                    <div className="friend-name">{user.username}</div>
                                    <div className="friend-status">{user.status || 'Нет статуса'}</div>
                                </div>
                                <div className="friend-actions">
                                    <button
                                        className="button remove-button"
                                        onClick={() => handleRemoveFriend(user.id)}
                                    >
                                        Удалить из друзей
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {sentRequests.length > 0 && (
                <div className="friends-section">
                    <h2 className="section-title">Исходящие заявки в друзья</h2>
                    <div className="friends-list">
                        {sentRequests.map((user) => (
                            <div key={user.id} className="friend-item">
                                <div className="friend-avatar">
                                    {user.avatarUrl ? (
                                        <img src={user.avatarUrl} alt={user.username} />
                                    ) : (
                                        <div className="avatar-placeholder">
                                            {user.username[0].toUpperCase()}
                                        </div>
                                    )}
                                </div>
                                <div className="friend-info">
                                    <div className="friend-name">{user.username}</div>
                                    <div className="friend-status">{user.status || 'Нет статуса'}</div>
                                </div>
                                <div className="friend-actions">
                                    <button
                                        className="button decline-button"
                                        onClick={() => handleDeclineRequest(user.id)}
                                    >
                                        Отменить заявку
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {friends.length === 0 && pendingRequests.length === 0 && sentRequests.length === 0 && (
                <div className="empty-state">
                    У вас пока нет друзей и заявок в друзья
                </div>
            )}
        </div>
    );
};

export default FriendsList; 