import React from 'react';

interface MessageStatusProps {
  isRead: boolean;
}

const MessageStatus: React.FC<MessageStatusProps> = ({ isRead }) => {
  return (
    <div className="message-status">
      {isRead ? (
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M2.5 8L6 11.5L13.5 4" stroke="#8e9297" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
          <path d="M6.5 8L10 11.5L17.5 4" stroke="#8e9297" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      ) : (
        <svg width="16" height="16" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
          <path d="M2.5 8L6 11.5L13.5 4" stroke="#8e9297" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
        </svg>
      )}
    </div>
  );
};

export default MessageStatus; 