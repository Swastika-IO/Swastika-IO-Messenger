"use strict";
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
Object.defineProperty(exports, "__esModule", { value: true });
class Subscription {
    constructor(subject, observer) {
        this.subject = subject;
        this.observer = observer;
    }
    dispose() {
        let index = this.subject.observers.indexOf(this.observer);
        if (index > -1) {
            this.subject.observers.splice(index, 1);
        }
        if (this.subject.observers.length === 0) {
            this.subject.cancelCallback().catch((_) => { });
        }
    }
}
exports.Subscription = Subscription;
class Subject {
    constructor(cancelCallback) {
        this.observers = [];
        this.cancelCallback = cancelCallback;
    }
    next(item) {
        for (let observer of this.observers) {
            observer.next(item);
        }
    }
    error(err) {
        for (let observer of this.observers) {
            if (observer.error) {
                observer.error(err);
            }
        }
    }
    complete() {
        for (let observer of this.observers) {
            if (observer.complete) {
                observer.complete();
            }
        }
    }
    subscribe(observer) {
        this.observers.push(observer);
        return new Subscription(this, observer);
    }
}
exports.Subject = Subject;
