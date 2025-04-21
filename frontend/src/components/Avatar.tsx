import React from 'react';
import config from '../config';
import styles from './Avatar.module.css';

interface AvatarProps {
  avatarUrl?: string | null;
  className?: string;
  alt?: string;
}

export const Avatar: React.FC<AvatarProps> = ({ avatarUrl, className = '', alt = 'Avatar' }) => {
  const fullAvatarUrl = avatarUrl 
    ? avatarUrl.startsWith('http') 
      ? avatarUrl 
      : `${config.baseUrl}${avatarUrl}`
    : `${config.baseUrl}/uploads/default-avatar.png`;

  return (
    <div className={`${styles.avatarContainer} ${className}`}>
      <img
        src={fullAvatarUrl}
        alt={alt}
        className={styles.avatar}
      />
    </div>
  );
}; 