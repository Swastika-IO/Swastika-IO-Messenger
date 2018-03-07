"use strict";
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
Object.defineProperty(exports, "__esModule", { value: true });
const ILogger_1 = require("./ILogger");
class NullLogger {
    log(logLevel, message) {
    }
}
exports.NullLogger = NullLogger;
class ConsoleLogger {
    constructor(minimumLogLevel) {
        this.minimumLogLevel = minimumLogLevel;
    }
    log(logLevel, message) {
        if (logLevel >= this.minimumLogLevel) {
            switch (logLevel) {
                case ILogger_1.LogLevel.Error:
                    console.error(`${ILogger_1.LogLevel[logLevel]}: ${message}`);
                    break;
                case ILogger_1.LogLevel.Warning:
                    console.warn(`${ILogger_1.LogLevel[logLevel]}: ${message}`);
                    break;
                case ILogger_1.LogLevel.Information:
                    console.info(`${ILogger_1.LogLevel[logLevel]}: ${message}`);
                    break;
                default:
                    console.log(`${ILogger_1.LogLevel[logLevel]}: ${message}`);
                    break;
            }
        }
    }
}
exports.ConsoleLogger = ConsoleLogger;
var LoggerFactory;
(function (LoggerFactory) {
    function createLogger(logging) {
        if (logging === undefined) {
            return new ConsoleLogger(ILogger_1.LogLevel.Information);
        }
        if (logging === null) {
            return new NullLogger();
        }
        if (logging.log) {
            return logging;
        }
        return new ConsoleLogger(logging);
    }
    LoggerFactory.createLogger = createLogger;
})(LoggerFactory = exports.LoggerFactory || (exports.LoggerFactory = {}));
