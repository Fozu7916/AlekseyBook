class Logger {
    error(message: string, error?: unknown) {
        const timestamp = new Date().toISOString();
        const errorMessage = error instanceof Error ? error.message : String(error);
        console.error(`[${timestamp}] ERROR: ${message}${error ? ` - ${errorMessage}` : ''}`);
    }
}

export const logger = new Logger(); 