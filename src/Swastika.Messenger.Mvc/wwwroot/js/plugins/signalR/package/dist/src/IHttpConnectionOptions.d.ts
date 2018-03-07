import { HttpClient } from "./HttpClient";
import { TransportType, ITransport } from "./Transports";
import { ILogger, LogLevel } from "./ILogger";
export interface IHttpConnectionOptions {
    httpClient?: HttpClient;
    transport?: TransportType | ITransport;
    logger?: ILogger | LogLevel;
    accessToken?: () => string;
}
