"use strict";
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
const HttpConnection_1 = require("./HttpConnection");
const Observable_1 = require("./Observable");
const JsonHubProtocol_1 = require("./JsonHubProtocol");
const Formatters_1 = require("./Formatters");
const Base64EncodedHubProtocol_1 = require("./Base64EncodedHubProtocol");
const ILogger_1 = require("./ILogger");
const Loggers_1 = require("./Loggers");
var Transports_1 = require("./Transports");
exports.TransportType = Transports_1.TransportType;
var HttpConnection_2 = require("./HttpConnection");
exports.HttpConnection = HttpConnection_2.HttpConnection;
var JsonHubProtocol_2 = require("./JsonHubProtocol");
exports.JsonHubProtocol = JsonHubProtocol_2.JsonHubProtocol;
var ILogger_2 = require("./ILogger");
exports.LogLevel = ILogger_2.LogLevel;
var Loggers_2 = require("./Loggers");
exports.ConsoleLogger = Loggers_2.ConsoleLogger;
exports.NullLogger = Loggers_2.NullLogger;
const DEFAULT_TIMEOUT_IN_MS = 30 * 1000;
class HubConnection {
    constructor(urlOrConnection, options = {}) {
        options = options || {};
        this.timeoutInMilliseconds = options.timeoutInMilliseconds || DEFAULT_TIMEOUT_IN_MS;
        if (typeof urlOrConnection === "string") {
            this.connection = new HttpConnection_1.HttpConnection(urlOrConnection, options);
        }
        else {
            this.connection = urlOrConnection;
        }
        this.logger = Loggers_1.LoggerFactory.createLogger(options.logger);
        this.protocol = options.protocol || new JsonHubProtocol_1.JsonHubProtocol();
        this.connection.onreceive = (data) => this.processIncomingData(data);
        this.connection.onclose = (error) => this.connectionClosed(error);
        this.callbacks = new Map();
        this.methods = new Map();
        this.closedCallbacks = [];
        this.id = 0;
    }
    processIncomingData(data) {
        if (this.timeoutHandle !== undefined) {
            clearTimeout(this.timeoutHandle);
        }
        // Parse the messages
        let messages = this.protocol.parseMessages(data);
        for (var i = 0; i < messages.length; ++i) {
            var message = messages[i];
            switch (message.type) {
                case 1 /* Invocation */:
                    this.invokeClientMethod(message);
                    break;
                case 2 /* StreamItem */:
                case 3 /* Completion */:
                    let callback = this.callbacks.get(message.invocationId);
                    if (callback != null) {
                        if (message.type === 3 /* Completion */) {
                            this.callbacks.delete(message.invocationId);
                        }
                        callback(message);
                    }
                    break;
                case 6 /* Ping */:
                    // Don't care about pings
                    break;
                default:
                    this.logger.log(ILogger_1.LogLevel.Warning, "Invalid message type: " + data);
                    break;
            }
        }
        this.configureTimeout();
    }
    configureTimeout() {
        if (!this.connection.features || !this.connection.features.inherentKeepAlive) {
            // Set the timeout timer
            this.timeoutHandle = setTimeout(() => this.serverTimeout(), this.timeoutInMilliseconds);
        }
    }
    serverTimeout() {
        // The server hasn't talked to us in a while. It doesn't like us anymore ... :(
        // Terminate the connection
        this.connection.stop(new Error("Server timeout elapsed without receiving a message from the server."));
    }
    invokeClientMethod(invocationMessage) {
        let methods = this.methods.get(invocationMessage.target.toLowerCase());
        if (methods) {
            methods.forEach(m => m.apply(this, invocationMessage.arguments));
            if (invocationMessage.invocationId) {
                // This is not supported in v1. So we return an error to avoid blocking the server waiting for the response.
                let message = "Server requested a response, which is not supported in this version of the client.";
                this.logger.log(ILogger_1.LogLevel.Error, message);
                this.connection.stop(new Error(message));
            }
        }
        else {
            this.logger.log(ILogger_1.LogLevel.Warning, `No client method with the name '${invocationMessage.target}' found.`);
        }
    }
    connectionClosed(error) {
        this.callbacks.forEach(callback => {
            callback(undefined, error ? error : new Error("Invocation canceled due to connection being closed."));
        });
        this.callbacks.clear();
        this.closedCallbacks.forEach(c => c.apply(this, [error]));
    }
    start() {
        return __awaiter(this, void 0, void 0, function* () {
            let requestedTransferMode = (this.protocol.type === 2 /* Binary */)
                ? 2 /* Binary */
                : 1 /* Text */;
            this.connection.features.transferMode = requestedTransferMode;
            yield this.connection.start();
            var actualTransferMode = this.connection.features.transferMode;
            yield this.connection.send(Formatters_1.TextMessageFormat.write(JSON.stringify({ protocol: this.protocol.name })));
            this.logger.log(ILogger_1.LogLevel.Information, `Using HubProtocol '${this.protocol.name}'.`);
            if (requestedTransferMode === 2 /* Binary */ && actualTransferMode === 1 /* Text */) {
                this.protocol = new Base64EncodedHubProtocol_1.Base64EncodedHubProtocol(this.protocol);
            }
            this.configureTimeout();
        });
    }
    stop() {
        if (this.timeoutHandle) {
            clearTimeout(this.timeoutHandle);
        }
        return this.connection.stop();
    }
    stream(methodName, ...args) {
        let invocationDescriptor = this.createStreamInvocation(methodName, args);
        let subject = new Observable_1.Subject(() => {
            let cancelInvocation = this.createCancelInvocation(invocationDescriptor.invocationId);
            let message = this.protocol.writeMessage(cancelInvocation);
            this.callbacks.delete(invocationDescriptor.invocationId);
            return this.connection.send(message);
        });
        this.callbacks.set(invocationDescriptor.invocationId, (invocationEvent, error) => {
            if (error) {
                subject.error(error);
                return;
            }
            if (invocationEvent.type === 3 /* Completion */) {
                let completionMessage = invocationEvent;
                if (completionMessage.error) {
                    subject.error(new Error(completionMessage.error));
                }
                else {
                    subject.complete();
                }
            }
            else {
                subject.next(invocationEvent.item);
            }
        });
        let message = this.protocol.writeMessage(invocationDescriptor);
        this.connection.send(message)
            .catch(e => {
            subject.error(e);
            this.callbacks.delete(invocationDescriptor.invocationId);
        });
        return subject;
    }
    send(methodName, ...args) {
        let invocationDescriptor = this.createInvocation(methodName, args, true);
        let message = this.protocol.writeMessage(invocationDescriptor);
        return this.connection.send(message);
    }
    invoke(methodName, ...args) {
        let invocationDescriptor = this.createInvocation(methodName, args, false);
        let p = new Promise((resolve, reject) => {
            this.callbacks.set(invocationDescriptor.invocationId, (invocationEvent, error) => {
                if (error) {
                    reject(error);
                    return;
                }
                if (invocationEvent.type === 3 /* Completion */) {
                    let completionMessage = invocationEvent;
                    if (completionMessage.error) {
                        reject(new Error(completionMessage.error));
                    }
                    else {
                        resolve(completionMessage.result);
                    }
                }
                else {
                    reject(new Error(`Unexpected message type: ${invocationEvent.type}`));
                }
            });
            let message = this.protocol.writeMessage(invocationDescriptor);
            this.connection.send(message)
                .catch(e => {
                reject(e);
                this.callbacks.delete(invocationDescriptor.invocationId);
            });
        });
        return p;
    }
    on(methodName, method) {
        if (!methodName || !method) {
            return;
        }
        methodName = methodName.toLowerCase();
        if (!this.methods.has(methodName)) {
            this.methods.set(methodName, []);
        }
        this.methods.get(methodName).push(method);
    }
    off(methodName, method) {
        if (!methodName || !method) {
            return;
        }
        methodName = methodName.toLowerCase();
        let handlers = this.methods.get(methodName);
        if (!handlers) {
            return;
        }
        var removeIdx = handlers.indexOf(method);
        if (removeIdx != -1) {
            handlers.splice(removeIdx, 1);
        }
    }
    onclose(callback) {
        if (callback) {
            this.closedCallbacks.push(callback);
        }
    }
    createInvocation(methodName, args, nonblocking) {
        if (nonblocking) {
            return {
                type: 1 /* Invocation */,
                target: methodName,
                arguments: args,
            };
        }
        else {
            let id = this.id;
            this.id++;
            return {
                type: 1 /* Invocation */,
                invocationId: id.toString(),
                target: methodName,
                arguments: args,
            };
        }
    }
    createStreamInvocation(methodName, args) {
        let id = this.id;
        this.id++;
        return {
            type: 4 /* StreamInvocation */,
            invocationId: id.toString(),
            target: methodName,
            arguments: args,
        };
    }
    createCancelInvocation(id) {
        return {
            type: 5 /* CancelInvocation */,
            invocationId: id,
        };
    }
}
exports.HubConnection = HubConnection;
