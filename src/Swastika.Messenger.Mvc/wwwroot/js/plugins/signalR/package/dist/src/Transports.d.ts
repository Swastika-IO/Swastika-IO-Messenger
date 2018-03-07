import { DataReceived, TransportClosed } from "./Common";
import { HttpClient } from "./HttpClient";
import { ILogger } from "./ILogger";
import { IConnection } from "./IConnection";
export declare enum TransportType {
    WebSockets = 0,
    ServerSentEvents = 1,
    LongPolling = 2,
}
export declare const enum TransferMode {
    Text = 1,
    Binary = 2,
}
export interface ITransport {
    connect(url: string, requestedTransferMode: TransferMode, connection: IConnection): Promise<TransferMode>;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    onreceive: DataReceived;
    onclose: TransportClosed;
}
export declare class WebSocketTransport implements ITransport {
    private readonly logger;
    private readonly accessToken;
    private webSocket;
    constructor(accessToken: () => string, logger: ILogger);
    connect(url: string, requestedTransferMode: TransferMode, connection: IConnection): Promise<TransferMode>;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    onreceive: DataReceived;
    onclose: TransportClosed;
}
export declare class ServerSentEventsTransport implements ITransport {
    private readonly httpClient;
    private readonly accessToken;
    private readonly logger;
    private eventSource;
    private url;
    constructor(httpClient: HttpClient, accessToken: () => string, logger: ILogger);
    connect(url: string, requestedTransferMode: TransferMode, connection: IConnection): Promise<TransferMode>;
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    onreceive: DataReceived;
    onclose: TransportClosed;
}
export declare class LongPollingTransport implements ITransport {
    private readonly httpClient;
    private readonly accessToken;
    private readonly logger;
    private url;
    private pollXhr;
    private pollAbort;
    constructor(httpClient: HttpClient, accessToken: () => string, logger: ILogger);
    connect(url: string, requestedTransferMode: TransferMode, connection: IConnection): Promise<TransferMode>;
    private poll(url, transferMode);
    send(data: any): Promise<void>;
    stop(): Promise<void>;
    onreceive: DataReceived;
    onclose: TransportClosed;
}
