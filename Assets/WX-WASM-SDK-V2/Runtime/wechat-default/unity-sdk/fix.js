/* eslint-disable prefer-spread */
/* eslint-disable prefer-rest-params */

export default {
    init() {
        this.fixTimer();
    },
    
    fixTimer() {
        // Check the whole family before installing any wrappers. A mixed native/wrapped
        // family would pass synthetic IDs to native cancellation functions.
        const timers = ['setTimeout', 'clearTimeout', 'setInterval', 'clearInterval',
            'requestAnimationFrame', 'cancelAnimationFrame'];
        const canReplace = timers.every((name) => {
            if (typeof window[name] !== 'function') return false;
            let owner = window;
            let descriptor;
            while (owner && !descriptor) {
                descriptor = Object.getOwnPropertyDescriptor(owner, name);
                owner = Object.getPrototypeOf(owner);
            }
            // Do not call host setters or force changes to read-only host properties.
            return (!descriptor || descriptor.writable === true)
                && (Object.prototype.hasOwnProperty.call(window, name) || Object.isExtensible(window));
        });
        if (!canReplace) return;
        const wm = {};
        const privateSetTimeout = window.setTimeout;
        let id = 0;
        const getId = function () {
            id += 1;
            if (id > 100000000) {
                id = 0;
            }
            return id;
        };
        // @ts-ignore
        window.setTimeout = function (vCallback, nDelay) {
            const aArgs = Array.prototype.slice.call(arguments, 2);
            const id = getId();
            const t = privateSetTimeout(vCallback instanceof Function
                ? () => {
                    // @ts-ignore
                    vCallback.apply(null, aArgs);
                    delete wm[id];
                }
                : vCallback, nDelay);
            wm[id] = t;
            return id;
        };
        const privateClearTimeout = window.clearTimeout;
        // @ts-ignore
        window.clearTimeout = function (id) {
            if (id) {
                const t = wm[id];
                if (t) {
                    privateClearTimeout(t);
                    delete wm[id];
                }
            }
        };
        const privateSetInterval = window.setInterval;
        // @ts-ignore
        window.setInterval = function (vCallback, nDelay) {
            const aArgs = Array.prototype.slice.call(arguments, 2);
            const id = getId();
            const t = privateSetInterval(vCallback instanceof Function
                ? () => {
                    // @ts-ignore
                    vCallback.apply(null, aArgs);
                }
                : vCallback, nDelay);
            wm[id] = t;
            return id;
        };
        const privateClearInterval = window.clearInterval;
        // @ts-ignore
        window.clearInterval = function (id) {
            if (id) {
                const t = wm[id];
                if (t) {
                    privateClearInterval(t);
                    delete wm[id];
                }
            }
        };
        const privateRequestAnimationFrame = window.requestAnimationFrame;
        window.requestAnimationFrame = function (vCallback) {
            const id = getId();
            const t = privateRequestAnimationFrame(() => {
                vCallback(0);
                delete wm[id];
            });
            wm[id] = t;
            return id;
        };
        const privateCancelAnimationFrame = window.cancelAnimationFrame;
        window.cancelAnimationFrame = function (id) {
            const t = wm[id];
            if (t) {
                privateCancelAnimationFrame(t);
                delete wm[id];
            }
        };
    },
};
