import React from 'react';
import config from '../config';
import styles from './Avatar.module.css';

interface AvatarProps {
  avatarUrl?: string | null;
  className?: string;
  alt?: string;
}

export const Avatar = ({ avatarUrl, className = '', alt = 'Avatar' }: AvatarProps): React.ReactElement => {
  const fullAvatarUrl = avatarUrl 
    ? avatarUrl.startsWith('http') 
      ? avatarUrl 
      : `${config.baseUrl}/api/files/${avatarUrl}`
    : `${config.baseUrl}/api/files/default-avatar.svg`;

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