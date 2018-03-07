export declare class HttpError extends Error {
    statusCode: number;
    constructor(errorMessage: string, statusCode: number);
}
export declare class TimeoutError extends Error {
    constructor(errorMessage?: string);
}
