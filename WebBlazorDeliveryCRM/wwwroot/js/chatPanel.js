window.dmsChatPanel = {
    _dotNetRef: null,
    _handler: null,

    attach(dotNetRef) {
        this.detach();
        this._dotNetRef = dotNetRef;
        this._handler = (e) => {
            if (e.key !== 'Escape' && e.key !== 'Esc') return;
            e.preventDefault();
            if (this._dotNetRef) {
                this._dotNetRef.invokeMethodAsync('HandleEscapeKey');
            }
        };
        document.addEventListener('keydown', this._handler, true);
    },

    detach() {
        if (this._handler) {
            document.removeEventListener('keydown', this._handler, true);
        }
        this._handler = null;
        this._dotNetRef = null;
    },

    clampMenuPosition(clientX, clientY, menuWidth, menuHeight) {
        const pad = 8;
        const w = Math.max(120, Number(menuWidth) || 210);
        const h = Math.max(72, Number(menuHeight) || 92);
        const vw = window.innerWidth || document.documentElement.clientWidth;
        const vh = window.innerHeight || document.documentElement.clientHeight;
        let left = Number(clientX) || 0;
        let top = Number(clientY) || 0;
        if (left + w + pad > vw) left = vw - w - pad;
        if (top + h + pad > vh) top = vh - h - pad;
        if (left < pad) left = pad;
        if (top < pad) top = pad;
        return { left: left, top: top };
    }
};
