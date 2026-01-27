// Kuestencode Shared UI JavaScript

/**
 * Downloads a file from a byte array.
 * @param {string} fileName - The name of the file to download
 * @param {string} contentType - The MIME type of the file
 * @param {Uint8Array} bytes - The file content as bytes
 */
function downloadFile(fileName, contentType, bytes) {
    const blob = new Blob([bytes], { type: contentType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName ?? '';
    anchor.click();
    URL.revokeObjectURL(url);
}

/**
 * Downloads a file from a base64 string.
 * @param {string} fileName - The name of the file to download
 * @param {string} contentType - The MIME type of the file
 * @param {string} base64 - The file content as base64
 */
function downloadFileFromBase64(fileName, contentType, base64) {
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    downloadFile(fileName, contentType, bytes);
}

/**
 * Opens a URL in a new browser tab/window.
 * @param {string} url - The URL to open
 */
function openInNewTab(url) {
    window.open(url, '_blank');
}

/**
 * Prints the current page.
 */
function printPage() {
    window.print();
}

/**
 * Prints a PDF from a base64 string.
 * @param {string} base64 - PDF content as base64
 */
function printPdfFromBase64(base64) {
    const pdfData = atob(base64);
    const bytes = new Uint8Array(pdfData.length);
    for (let i = 0; i < pdfData.length; i++) {
        bytes[i] = pdfData.charCodeAt(i);
    }
    const blob = new Blob([bytes], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);

    const printWindow = window.open(url, '_blank');
    if (printWindow) {
        printWindow.onload = function () {
            printWindow.print();
        };
    }
}

/**
 * Copies text to clipboard.
 * @param {string} text - The text to copy
 * @returns {boolean} True if successful
 */
async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        return true;
    } catch (err) {
        console.error('Failed to copy to clipboard:', err);
        return false;
    }
}

/**
 * Shows a browser notification (requires permission).
 * @param {string} title - The notification title
 * @param {string} body - The notification body
 * @param {string} icon - Optional icon URL
 */
async function showNotification(title, body, icon) {
    if (!("Notification" in window)) {
        return false;
    }

    if (Notification.permission === "granted") {
        new Notification(title, { body, icon });
        return true;
    }

    if (Notification.permission !== "denied") {
        const permission = await Notification.requestPermission();
        if (permission === "granted") {
            new Notification(title, { body, icon });
            return true;
        }
    }

    return false;
}

/**
 * Scrolls to an element by ID.
 * @param {string} elementId - The ID of the element to scroll to
 * @param {string} behavior - Scroll behavior ('smooth' or 'auto')
 */
function scrollToElement(elementId, behavior = 'smooth') {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior, block: 'start' });
    }
}

/**
 * Gets the current scroll position.
 * @returns {object} Object with x and y scroll positions
 */
function getScrollPosition() {
    return {
        x: window.scrollX || window.pageXOffset,
        y: window.scrollY || window.pageYOffset
    };
}

/**
 * Sets focus to an element by ID.
 * @param {string} elementId - The ID of the element to focus
 */
function focusElement(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.focus();
    }
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        downloadFile,
        downloadFileFromBase64,
        openInNewTab,
        printPage,
        printPdfFromBase64,
        copyToClipboard,
        showNotification,
        scrollToElement,
        getScrollPosition,
        focusElement
    };
}
