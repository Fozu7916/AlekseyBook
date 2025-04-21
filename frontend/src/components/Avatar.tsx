import * as React from 'react';
import styles from './Avatar.module.css';
import config from '../config';

interface AvatarProps {
  avatarUrl?: string | null;
  className?: string;
}

export const Avatar = ({ avatarUrl, className }: AvatarProps): React.ReactElement => {
  const defaultAvatar = `${config.baseUrl}/uploads/default-avatar.png`;
  
  const finalUrl = avatarUrl || defaultAvatar;
  
  return (
    <div className={`${styles.avatarContainer} ${className || ''}`}>
      <img 
        src={finalUrl}
        alt="User avatar" 
        className={styles.avatarImage}
      />
    </div>
  );
}; 