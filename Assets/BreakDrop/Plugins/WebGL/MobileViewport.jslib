mergeInto(LibraryManager.library, {
    BD_ReadViewport: function (ptr) {
        if (typeof GameGlobal === 'undefined' || !GameGlobal.breakDropViewport) return 0;
        var m = GameGlobal.breakDropViewport.read();
        var start = ptr >> 2;
        for (var i = 0; i < 8; ++i) HEAPF32[start + i] = m[i];
        return 1;
    }
});
