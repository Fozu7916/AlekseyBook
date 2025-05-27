import axios from 'axios';
import { Notification, NOTIFICATION_TYPES } from '../types/notification';
import { API_CONFIG } from '../config/api.config';

const NOTIFICATIONS_URL = `${API_CONFIG.API_URL}/notifications`;

const getAuthToken = () => {
    return localStorage.getItem('token');
};

const axiosConfig = () => ({
    headers: {
        Authorization: `Bearer ${getAuthToken()}`
    }
});

export const notificationService = {
    getNotifications: async (): Promise<Notification[]> => {
        const response = await axios.get(NOTIFICATIONS_URL, axiosConfig());
        return response.data;
    },

    getUnreadCount: async (): Promise<number> => {
        const response = await axios.get(`${NOTIFICATIONS_URL}/unread/count`, axiosConfig());
        return response.data;
    },

    markAsRead: async (id: number): Promise<void> => {
        await axios.post(`${NOTIFICATIONS_URL}/${id}/read`, {}, axiosConfig());
    },

    markAllAsRead: async (): Promise<void> => {
        await axios.post(`${NOTIFICATIONS_URL}/read-all`, {}, axiosConfig());
    },

    deleteNotification: async (id: number): Promise<void> => {
        await axios.delete(`${NOTIFICATIONS_URL}/${id}`, axiosConfig());
    },

    createNotification: async (userId: number, type: string, title: string, text: string, link?: string): Promise<void> => {
        await axios.post(NOTIFICATIONS_URL, {
            userId,
            type,
            title,
            text,
            link
        }, axiosConfig());
    }
}; 