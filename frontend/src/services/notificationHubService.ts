import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { Notification } from '../types/notification';
import { API_CONFIG } from '../config/api.config';

type NotificationCallback = (notification: Notification) => void;
type NotificationReadCallback = (notificationId: number) => void;
type AllNotificationsReadCallback = () => void;
type NotificationDeletedCallback = (notificationId: number) => void;

class NotificationHubService {
    private connection: HubConnection | null = null;
    private notificationCallbacks: NotificationCallback[] = [];
    private notificationReadCallbacks: NotificationReadCallback[] = [];
    private allNotificationsReadCallbacks: AllNotificationsReadCallback[] = [];
    private notificationDeletedCallbacks: NotificationDeletedCallback[] = [];

    async connect(): Promise<void> {
        if (this.connection) {
            return;
        }

        const token = localStorage.getItem('token');

        this.connection = new HubConnectionBuilder()
            .withUrl(`${API_CONFIG.API_URL}/hubs/notification`, {
                accessTokenFactory: () => token || ''
            })
            .withAutomaticReconnect()
            .build();

        this.connection.on('ReceiveNotification', (notification: Notification) => {
            this.notificationCallbacks.forEach(callback => callback(notification));
        });

        this.connection.on('NotificationRead', (notificationId: number) => {
            this.notificationReadCallbacks.forEach(callback => callback(notificationId));
        });

        this.connection.on('AllNotificationsRead', () => {
            this.allNotificationsReadCallbacks.forEach(callback => callback());
        });

        this.connection.on('NotificationDeleted', (notificationId: number) => {
            this.notificationDeletedCallbacks.forEach(callback => callback(notificationId));
        });

        try {
            await this.connection.start();
        } catch (err) {
            console.error('Ошибка при подключении к хабу уведомлений:', err);
        }
    }

    onNotification(callback: NotificationCallback): () => void {
        this.notificationCallbacks.push(callback);
        return () => {
            this.notificationCallbacks = this.notificationCallbacks.filter(cb => cb !== callback);
        };
    }

    onNotificationRead(callback: NotificationReadCallback): () => void {
        this.notificationReadCallbacks.push(callback);
        return () => {
            this.notificationReadCallbacks = this.notificationReadCallbacks.filter(cb => cb !== callback);
        };
    }

    onAllNotificationsRead(callback: AllNotificationsReadCallback): () => void {
        this.allNotificationsReadCallbacks.push(callback);
        return () => {
            this.allNotificationsReadCallbacks = this.allNotificationsReadCallbacks.filter(cb => cb !== callback);
        };
    }

    onNotificationDeleted(callback: NotificationDeletedCallback): () => void {
        this.notificationDeletedCallbacks.push(callback);
        return () => {
            this.notificationDeletedCallbacks = this.notificationDeletedCallbacks.filter(cb => cb !== callback);
        };
    }

    async disconnect(): Promise<void> {
        if (this.connection) {
            await this.connection.stop();
            this.connection = null;
        }
    }

    isConnected(): boolean {
        return this.connection?.state === 'Connected';
    }
}

export const notificationHubService = new NotificationHubService(); 