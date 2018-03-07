import { DataReceived, ConnectionClosed } from "./Common";
export interface IConnection {
    readonly features: any;
    start(): Promise<void>;
    send(data: any): Promise<void>;
    stop(error?: Error): Promise<void>;
    onreceive: DataReceived;
    onclose: ConnectionClosed;
}
