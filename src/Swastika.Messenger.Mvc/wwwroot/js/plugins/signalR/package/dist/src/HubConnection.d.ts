import { ConnectionClosed } from "./Common";
import { IConnection } from "./IConnection";
import { Observable } from "./Observable";
import { IHubConnectionOptions } from "./IHubConnectionOptions";
export { TransportType } from "./Transports";
export { HttpConnection } from "./HttpConnection";
export { JsonHubProtocol } from "./JsonHubProtocol";
export { LogLevel, ILogger } from "./ILogger";
export { ConsoleLogger, NullLogger } from "./Loggers";
export declare class HubConnection {
    private readonly connection;
    private readonly logger;
    private protocol;
    private callbacks;
    private methods;
    private id;
    private closedCallbacks;
    private timeoutHandle;
    private timeoutInMilliseconds;
    constructor(url: string, options?: IHubConnectionOptions);
    constructor(connection: IConnection, options?: IHubConnectionOptions);
    private processIncomingData(data);
    private configureTimeout();
    private serverTimeout();
    private invokeClientMethod(invocationMessage);
    private connectionClosed(error?);
    start(): Promise<void>;
    stop(): Promise<void>;
    stream<T>(methodName: string, ...args: any[]): Observable<T>;
    send(methodName: string, ...args: any[]): Promise<void>;
    invoke(methodName: string, ...args: any[]): Promise<any>;
    on(methodName: string, method: (...args: any[]) => void): void;
    off(methodName: string, method: (...args: any[]) => void): void;
    onclose(callback: ConnectionClosed): void;
    private createInvocation(methodName, args, nonblocking);
    private createStreamInvocation(methodName, args);
    private createCancelInvocation(id);
}
