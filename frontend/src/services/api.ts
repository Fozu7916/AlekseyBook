import axios from 'axios';
import { API_CONFIG } from '../config/api.config';

export const api = axios.create({
    baseURL: API_CONFIG.API_URL,
    headers: {
        'Content-Type': 'application/json'
    }
});

api.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

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