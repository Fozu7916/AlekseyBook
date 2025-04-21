/** @jsxImportSource react */
import React from 'react';
import config from '../config';
import styles from './Avatar.module.css';

interface AvatarProps {
  avatarUrl?: string | null;
  className?: string;
  alt?: string;
}

export function Avatar({ avatarUrl, className = '', alt = 'Avatar' }: AvatarProps) {
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
} 