// Native window metrics are in logical pixels. Unity handles render-pixel resizing.
(function () {
    let current;
    let sizeOverride;
    let lastLog = '';
    function positive(value, fallback) { return Number.isFinite(value) && value > 0 ? value : fallback; }
    function read() {
        const info = wx.getWindowInfo ? wx.getWindowInfo() : wx.getSystemInfoSync();
        const width = positive(sizeOverride && sizeOverride.windowWidth, positive(info.windowWidth, info.screenWidth));
        const height = positive(sizeOverride && sizeOverride.windowHeight, positive(info.windowHeight, info.screenHeight));
        const ratio = positive(info.pixelRatio, 1);
        const safe = info.safeArea || {left: 0, top: 0, right: width, bottom: height};
        let capsuleBottom = 0;
        try {
            const capsule = wx.getMenuButtonBoundingClientRect && wx.getMenuButtonBoundingClientRect();
            if (capsule && capsule.width > 0 && capsule.height > 0 && capsule.bottom < height)
                capsuleBottom = capsule.bottom;
        } catch (_) { /* Native hosts without a capsule use their safe area. */ }
        current = [width, height, safe.left || 0, safe.top || 0,
            Number.isFinite(safe.right) ? safe.right : width,
            Number.isFinite(safe.bottom) ? safe.bottom : height, capsuleBottom, ratio];
        const signature = JSON.stringify(current);
        if (signature !== lastLog) {
            ['innerWidth', 'innerHeight', 'devicePixelRatio'].forEach((key, index) => {
                const value = [width, height, ratio][index];
                try { window[key] = value; } catch (_) { /* Browser-owned dimensions remain native. */ }
                if (typeof GameGlobal !== 'undefined' && GameGlobal !== window) {
                    try { GameGlobal[key] = value; } catch (_) { }
                }
            });
            lastLog = signature;
            console.info('[BreakDrop Viewport] window,safe,capsule,dpr', current);
        }
        return current;
    }
    GameGlobal.breakDropViewport = {
        read,
        // Copy this object from the DevTools console when reporting a device issue.
        inspect: () => ({metrics: read().slice(), canvas: GameGlobal.canvas ?
            [GameGlobal.canvas.width, GameGlobal.canvas.height] : null})
    };
    if (wx.onWindowResize) wx.onWindowResize((size) => { sizeOverride = size; read(); });
    if (wx.onDeviceOrientationChange) wx.onDeviceOrientationChange(() => { sizeOverride = null; read(); });
    if (wx.onShow) wx.onShow(() => { sizeOverride = null; read(); });
    read();
}());
