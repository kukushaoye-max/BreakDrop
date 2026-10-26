/* eslint-disable */
(function () {
    // Compatibility fallbacks for APIs absent in older hosts.
    // Real device APIs must remain authoritative for viewport size and safe area.
    const nativeApis = {};
    ['getSystemInfoSync', 'getSystemInfo', 'getSystemInfoAsync', 'getWindowInfo', 'getDeviceInfo', 'getAppBaseInfo', 'getNetworkType', 'getPerformance', 'getRealtimeLogManager', 'getLogManager'].forEach(function (name) {
        if (typeof wx[name] === 'function') nativeApis[name] = wx[name];
    });
    const defaults = {
        brand: 'unknown',
        model: 'unknown',
        pixelRatio: 2,
        screenWidth: 390,
        screenHeight: 844,
        windowWidth: 390,
        windowHeight: 844,
        statusBarHeight: 20,
        language: 'zh_CN',
        version: '8.0.50',
        system: typeof window !== 'undefined' ? 'Windows 10' : 'Android 12',
        platform: typeof window !== 'undefined' ? 'devtools' : 'android',
        fontSizeSetting: 16,
        SDKVersion: '3.15.2',
        benchmarkLevel: 1,
        safeArea: { left: 0, top: 0, right: 390, bottom: 844, width: 390, height: 844 },
        renderer: '',
    };

    // Read real dimensions before installing compatibility fallbacks.
    ['getDeviceInfo', 'getAppBaseInfo', 'getWindowInfo'].forEach(function (name) {
        try { if (nativeApis[name]) Object.assign(defaults, nativeApis[name].call(wx)); }
        catch (_) { /* A failing legacy API must not replace working native APIs. */ }
    });

    function cloneInfo() {
        return Object.assign({}, defaults);
    }

    function windowInfoFrom(info) {
        return {
            screenWidth: info.screenWidth,
            screenHeight: info.screenHeight,
            pixelRatio: info.pixelRatio,
            windowWidth: info.windowWidth || info.screenWidth,
            windowHeight: info.windowHeight || info.screenHeight,
            statusBarHeight: info.statusBarHeight,
            safeArea: info.safeArea,
        };
    }

    function invokeAsync(opt, payload) {
        if (!opt)
            return;
        if (typeof opt.success === 'function')
            opt.success(payload);
        if (typeof opt.complete === 'function')
            opt.complete(payload);
    }

    wx.getPerformance = function () {
        return {
            now: function () {
                return Date.now() * 1000;
            },
        };
    };

    wx.getSystemInfoSync = function () {
        return cloneInfo();
    };

    wx.getSystemInfo = function (opt) {
        const payload = Object.assign({ errMsg: 'getSystemInfo:ok' }, cloneInfo());
        invokeAsync(opt, payload);
        return payload;
    };

    if (typeof wx.getSystemInfoAsync === 'function' || !wx.getSystemInfoAsync) {
        wx.getSystemInfoAsync = function (opt) {
            const payload = Object.assign({ errMsg: 'getSystemInfo:ok' }, cloneInfo());
            invokeAsync(opt, payload);
            return Promise.resolve(payload);
        };
    }

    wx.getWindowInfo = function () {
        return windowInfoFrom(cloneInfo());
    };

    wx.getDeviceInfo = function () {
        const info = cloneInfo();
        return {
            brand: info.brand,
            model: info.model,
            system: info.system,
            platform: info.platform,
            benchmarkLevel: info.benchmarkLevel,
            abi: info.abi,
        };
    };

    wx.getAppBaseInfo = function () {
        const info = cloneInfo();
        return {
            version: info.version,
            SDKVersion: info.SDKVersion,
            language: info.language,
            theme: info.theme,
            host: info.host,
        };
    };

    wx.getNetworkType = function (opt) {
        const payload = { networkType: 'wifi', errMsg: 'getNetworkType:ok' };
        invokeAsync(opt, payload);
        return payload;
    };

    (function patchTouchHooks() {
        const names = ['onTouchStart', 'onTouchMove', 'onTouchEnd', 'onTouchCancel'];
        names.forEach((name) => {
            const nativeFn = wx[name];
            wx[name] = function (callback) {
                if (typeof callback !== 'function')
                    return;
                try {
                    if (typeof nativeFn === 'function')
                        return nativeFn.call(wx, callback);
                }
                catch (err) {
                    console.warn(`[FindDogs] ${name} unavailable`, err);
                }
                wx[`__findDogs${name}`] = callback;
            };
        });
    }());

    wx.getRealtimeLogManager = function () {
        return {
            info: function (...args) {
                console.log.apply(console, args);
            },
            warn: function (...args) {
                console.warn.apply(console, args);
            },
            error: function (...args) {
                console.error.apply(console, args);
            },
            setFilterMsg: function () { },
            addFilterMsg: function () { },
        };
    };

    wx.getLogManager = function () {
        return {
            info: function (...args) {
                console.log.apply(console, args);
            },
            warn: function (...args) {
                console.warn.apply(console, args);
            },
            log: function (...args) {
                console.log.apply(console, args);
            },
            debug: function (...args) {
                console.log.apply(console, args);
            },
        };
    };
    // Preserve native device, window, network and async semantics when available.
    Object.keys(nativeApis).forEach(function (name) { wx[name] = nativeApis[name]; });
}());
