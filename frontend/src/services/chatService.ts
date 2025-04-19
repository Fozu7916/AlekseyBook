import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { userService } from './userService';
import { Message } from './userService';

export class ChatService {
  private connection: HubConnection | null = null;
  private typingCallbacks: ((userId: string, isTyping: boolean) => void)[] = [];
  private messageCallbacks: ((message: Message) => void)[] = [];

  public async startConnection() {
    try {
      this.connection = new HubConnectionBuilder()
        .withUrl('http://localhost:5038/chatHub', {
          accessTokenFactory: () => localStorage.getItem('token') || ''
        })
        .withAutomaticReconnect()
        .build();

      await this.connection.start();
      console.log('SignalR Connected');

      // После подключения присоединяемся к чату
      const currentUser = await userService.getCurrentUser();
      if (currentUser) {
        await this.connection.invoke('JoinChat', currentUser.id.toString());
        console.log('Joined chat as user:', currentUser.id);
      }

      this.connection.on('ReceiveTypingStatus', (userId: string, isTyping: boolean) => {
        console.log('Получен статус печатания:', userId, isTyping);
        this.typingCallbacks.forEach(callback => callback(userId, isTyping));
      });

      this.connection.on('ReceiveMessage', (message: Message) => {
        console.log('Получено новое сообщение:', message);
        this.messageCallbacks.forEach(callback => callback(message));
      });

    } catch (err) {
      console.error('SignalR Connection Error:', err);
    }
  }

  public async stopConnection() {
    if (this.connection) {
      try {
        const currentUser = await userService.getCurrentUser();
        if (currentUser) {
          await this.connection.invoke('LeaveChat', currentUser.id.toString());
          console.log('Left chat as user:', currentUser.id);
        }
        await this.connection.stop();
        console.log('SignalR Disconnected');
      } catch (err) {
        console.error('SignalR Disconnection Error:', err);
      }
    }
  }

  public async sendTypingStatus(userId: string, isTyping: boolean) {
    if (this.connection?.state === 'Connected') {
      try {
        await this.connection.invoke('SendTypingStatus', userId, isTyping);
        console.log('Отправлен статус печатания:', userId, isTyping);
      } catch (err) {
        console.error('Error sending typing status:', err);
      }
    } else {
      console.warn('SignalR not connected');
    }
  }

  public onTypingStatus(callback: (userId: string, isTyping: boolean) => void) {
    this.typingCallbacks.push(callback);
    return () => {
      this.typingCallbacks = this.typingCallbacks.filter(cb => cb !== callback);
    };
  }

  public onMessage(callback: (message: Message) => void) {
    this.messageCallbacks.push(callback);
    return () => {
      this.messageCallbacks = this.messageCallbacks.filter(cb => cb !== callback);
    };
  }

  public isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }

  public async sendMessage(message: Message) {
    if (this.isConnected()) {
      try {
        await this.connection!.invoke('SendMessage', message);
        console.log('Сообщение отправлено через SignalR:', message);
      } catch (err) {
        console.error('Error sending message through SignalR:', err);
      }
    } else {
      console.warn('SignalR not connected, message will not be sent in real-time');
    }
  }
}

export const chatService = new ChatService(); 