"use strict";
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
Object.defineProperty(exports, "__esModule", { value: true });
// Rough polyfill of https://developer.mozilla.org/en-US/docs/Web/API/AbortController
// We don't actually ever use the API being polyfilled, we always use the polyfill because
// it's a very new API right now.
class AbortController {
    constructor() {
        this.isAborted = false;
    }
    abort() {
        if (!this.isAborted) {
            this.isAborted = true;
            if (this.onabort) {
                this.onabort();
            }
        }
    }
    get signal() {
        return this;
    }
    get aborted() {
        return this.isAborted;
    }
}
exports.AbortController = AbortController;
