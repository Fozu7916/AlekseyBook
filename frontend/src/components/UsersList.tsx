import React, { useState, useEffect, useRef, useCallback } from 'react';
import { userService } from '../services/userService';
import { UserResponse } from '../types/user';
import { debounce } from 'lodash';
import './UsersList.css';

const UsersList: React.FC = () => {
    const [users, setUsers] = useState<UserResponse[]>([]);
    const [loading, setLoading] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(true);
    const observer = useRef<IntersectionObserver>();
    const lastUserElementRef = useCallback((node: Element | null) => {
        if (loading) return;
        if (observer.current) observer.current.disconnect();
        observer.current = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting && hasMore) {
                setPage(prevPage => prevPage + 1);
            }
        });
        if (node) observer.current.observe(node);
    }, [loading, hasMore]);

    const loadUsers = async (currentPage: number, search: string) => {
        try {
            setLoading(true);
            const response = search
                ? await userService.searchUsers(search, currentPage)
                : await userService.getUsers(currentPage);
            
            setUsers(prevUsers => currentPage === 1 
                ? response 
                : [...prevUsers, ...response]
            );
            setHasMore(response.length === 20);
        } catch (error) {
            console.error('Error loading users:', error);
        } finally {
            setLoading(false);
        }
    };

    const debouncedSearch = useCallback(
        debounce((term: string) => {
            setPage(1);
            loadUsers(1, term);
        }, 500),
        []
    );

    useEffect(() => {
        debouncedSearch(searchTerm);
    }, [searchTerm]);

    useEffect(() => {
        if (page > 1) {
            loadUsers(page, searchTerm);
        }
    }, [page]);

    const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        setSearchTerm(event.target.value);
    };

    return (
        <div className="users-list-container">
            <input
                type="text"
                className="search-input"
                placeholder="Поиск пользователей..."
                value={searchTerm}
                onChange={handleSearchChange}
            />
            <div className="users-list">
                {users.map((user, index) => (
                    <div
                        key={user.id}
                        ref={index === users.length - 1 ? lastUserElementRef : undefined}
                        className="user-item"
                    >
                        <div className="user-avatar">
                            {user.avatarUrl ? (
                                <img src={user.avatarUrl} alt={user.username} />
                            ) : (
                                <div className="avatar-placeholder">
                                    {user.username[0].toUpperCase()}
                                </div>
                            )}
                        </div>
                        <div className="user-info">
                            <div className="user-name">{user.username}</div>
                            <div className="user-status">{user.status || 'Нет статуса'}</div>
                        </div>
                    </div>
                ))}
            </div>
            {loading && (
                <div className="loading-spinner">
                    Загрузка...
                </div>
            )}
        </div>
    );
};

export default UsersList; 