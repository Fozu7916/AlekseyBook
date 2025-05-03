export class LoggerService {
    private static instance: LoggerService;
    private isDevelopment: boolean;

    private constructor() {
        this.isDevelopment = process.env.NODE_ENV === 'development';
    }

    public static getInstance(): LoggerService {
        if (!LoggerService.instance) {
            LoggerService.instance = new LoggerService();
        }
        return LoggerService.instance;
    }

    public error(message: string, error?: any, context?: any): void {
        if (this.isDevelopment) {
            console.error(`[Ошибка] ${message}`, error ? { error, context } : context);
        }
    }

    public warn(message: string, context?: any): void {
        if (this.isDevelopment) {
            console.warn(`[Предупреждение] ${message}`, context || '');
        }
    }

    public info(message: string, context?: any): void {
        if (this.isDevelopment) {
            console.log(`[Инфо] ${message}`, context || '');
        }
    }
}

export const logger = LoggerService.getInstance(); 