import {
    HttpTransportType,
    HubConnection,
    HubConnectionBuilder,
    LogLevel,
    HubConnectionState
} from '@microsoft/signalr';
import { onConnect, connect, onDisconnect } from '@/lib/clients/socketClient/events.ts';
import { deviceInfo } from '@/lib/clients/socketClient/device.ts';

export class SocketClient {
    public connection: HubConnection | null = null;
    private accessToken: string;
    private baseUrl: string;
    private deviceInfo: ReturnType<typeof deviceInfo> = deviceInfo();
    private readonly keepAliveInterval: number = 10;
    private readonly endpoint: string;
    private reconnectAttempts: number = 0;
    private readonly maxReconnectAttempts: number = 500;

    constructor(baseUrl: string, accessToken: string, endpoint: string = 'socket') {
        this.baseUrl = baseUrl;
        this.accessToken = accessToken;
        this.endpoint = endpoint;

        this.connection = this.connectionBuilder();
    }

    public isConnected(): boolean {
        return this.connection?.state === HubConnectionState.Connected;
    }

    public dispose = async (): Promise<void> => {
        if (!this.connection) return;

        try {
            await this.connection.stop();
            this.connection = null;
        } catch (error) {
            console.error('Error stopping connection:', error);
        }
    }

    public setup = async (): Promise<void> => {
        if (!this.connection) return;

        try {
            this.setupEventHandlers();
            await this.connection.start();
            this.reconnectAttempts = 0;
            onConnect(this.connection);
            connect(this.connection);
        } catch (error) {
            console.error('Error setting up connection:', error);
            await this.handleConnectionError();
        }
    };

    private setupEventHandlers(): void {
        if (!this.connection) return;

        this.connection.onreconnecting((error?: Error) => {
            console.log('SignalR Disconnected.', error?.message);
            onDisconnect(this.connection!);
        });

        this.connection.onreconnected(() => {
            console.log('SignalR Reconnected.');
            this.reconnectAttempts = 0;
            onConnect(this.connection!);
        });

        this.connection.onclose(async () => {
            console.log('SignalR Closed.');
            onDisconnect(this.connection!);
            await this.handleConnectionError();
        });
    }

    private async handleConnectionError(): Promise<void> {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.error('Max reconnection attempts reached');
            return;
        }

        this.reconnectAttempts++;
        if (this.connection?.state === HubConnectionState.Disconnected) {
            try {
                await this.connection.start();
                onConnect(this.connection);
                connect(this.connection);
            } catch (error) {
                console.error('Error reconnecting:', error);
                setTimeout(() => this.handleConnectionError(), 5000);
            }
        }
    }

    private connectionBuilder(): HubConnection {
        const urlString = this.urlBuilder();

        return new HubConnectionBuilder()
          .withUrl(`${this.baseUrl}/${this.endpoint}`, {
              accessTokenFactory: () => this.accessToken + urlString,
              skipNegotiation: true,
              transport: HttpTransportType.WebSockets,
          })
          .withKeepAliveInterval(this.keepAliveInterval * 1000)
          .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
          .configureLogging(LogLevel.Error)
          .build();
    }

    private urlBuilder(): string {
        const urlParams = new URLSearchParams([
            ['client_id', this.deviceInfo.id],
            ['client_name', this.deviceInfo.name],
            ['client_type', this.deviceInfo.type ?? 'web'],
            ['client_version', this.deviceInfo.version],
            ['client_os', this.deviceInfo.os],
            ['client_browser', this.deviceInfo.browser],
            ['client_device', this.deviceInfo.device],
        ]);

        return `&${urlParams.toString()}`;
    }
}

export default SocketClient;