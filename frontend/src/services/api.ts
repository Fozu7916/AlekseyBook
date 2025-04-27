import axios from 'axios';

export const login = async (username: string, password: string) => {
  const response = await axios.post('/api/auth/login', { username, password });
  return response.data;
};

export const register = async (username: string, email: string, password: string) => {
  const response = await axios.post('/api/auth/register', { username, email, password });
  return response.data;
};

export const getProfile = async (userId: string) => {
  const response = await axios.get(`/api/users/${userId}`);
  return response.data;
}; 