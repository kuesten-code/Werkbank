// Feedback-Formular: Screenshots aus der Zwischenablage einfügen.
// Eingefügte Bilder werden dem <input type="file"> der Seite untergeschoben und ein
// "change"-Event ausgelöst — Blazors InputFile verarbeitet sie damit genauso wie eine
// normale Dateiauswahl (inkl. Streaming, ohne SignalR-Nachrichtenlimit).
window.werkbankFeedback = (function () {
    let pasteHandler = null;

    function extensionFor(type) {
        switch (type) {
            case 'image/jpeg': return 'jpg';
            case 'image/webp': return 'webp';
            default: return 'png';
        }
    }

    return {
        registerPaste: function (inputId) {
            this.unregisterPaste();
            pasteHandler = function (event) {
                const items = (event.clipboardData && event.clipboardData.items) || [];
                const files = [];
                for (const item of items) {
                    if (item.kind !== 'file' || !item.type.startsWith('image/')) continue;
                    const blob = item.getAsFile();
                    if (blob) {
                        const name = 'screenshot-' + new Date().toISOString().replace(/[:.]/g, '-') + '.' + extensionFor(blob.type);
                        files.push(new File([blob], name, { type: blob.type }));
                    }
                }
                if (files.length === 0) return;

                const input = document.getElementById(inputId);
                if (!input) return;

                const transfer = new DataTransfer();
                files.forEach(f => transfer.items.add(f));
                input.files = transfer.files;
                input.dispatchEvent(new Event('change', { bubbles: true }));
                event.preventDefault();
            };
            document.addEventListener('paste', pasteHandler);
        },
        unregisterPaste: function () {
            if (pasteHandler) {
                document.removeEventListener('paste', pasteHandler);
                pasteHandler = null;
            }
        },
        referrer: function () {
            return document.referrer || '';
        }
    };
})();
