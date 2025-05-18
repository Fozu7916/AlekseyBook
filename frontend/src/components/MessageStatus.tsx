import React from 'react';

interface MessageStatusProps {
    isRead: boolean;
}

const MessageStatus: React.FC<MessageStatusProps> = ({ isRead }) => {
    return (
        <span style={{ marginLeft: '4px', color: 'inherit', opacity: 0.7 }}>
            {isRead ? (
                <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M18 7l-1.41-1.41-6.34 6.34 1.41 1.41L18 7zm4.24-1.41L11.66 16.17 7.48 12l-1.41 1.41L11.66 19l12-12-1.42-1.41zM.41 13.41L6 19l1.41-1.41L1.83 12 .41 13.41z"/>
                </svg>
            ) : (
                <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z"/>
                </svg>
            )}
        </span>
    );
};

export default MessageStatus; 