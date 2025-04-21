import React from 'react';
import config from '../config';

interface AvatarProps {
  avatarUrl?: string | null;
  className?: string;
  alt?: string;
}

export const Avatar: React.FC<AvatarProps> = ({ avatarUrl, className = '', alt = 'Avatar' }) => {
  const fullAvatarUrl = avatarUrl 
    ? `${config.apiUrl.replace('/api', '')}${avatarUrl}` 
    : '/images/default-avatar.svg';

  return (
    <img
      src={fullAvatarUrl}
      alt={alt}
      className={className}
    />
  );
}; 