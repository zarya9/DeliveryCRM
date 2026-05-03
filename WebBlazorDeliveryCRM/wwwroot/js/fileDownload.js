window.fileDownload = {
    saveFromStream: async function (streamRef, fileName, mimeType) {
        if (!streamRef || !streamRef.arrayBuffer) {
            console.error("fileDownload.saveFromStream: invalid stream");
            return;
        }
        const arrayBuffer = await streamRef.arrayBuffer();
        const blob = new Blob([arrayBuffer], { type: mimeType || "application/octet-stream" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName || "download";
        a.click();
        URL.revokeObjectURL(url);
    },
    saveBase64: function (fileName, mimeType, base64) {
        const binary = atob(base64);
        const len = binary.length;
        const bytes = new Uint8Array(len);
        for (let i = 0; i < len; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        const blob = new Blob([bytes], { type: mimeType || "application/octet-stream" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName || "download";
        a.click();
        URL.revokeObjectURL(url);
    },
    saveFromUrl: async function (url, fileName) {
        if (!url) return;
        try {
            const response = await fetch(url, { credentials: "include" });
            if (!response.ok) throw new Error("download failed");
            const blob = await response.blob();
            const objectUrl = URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = objectUrl;
            a.download = fileName || "download";
            a.click();
            URL.revokeObjectURL(objectUrl);
        } catch {
            const a = document.createElement("a");
            a.href = url;
            a.download = fileName || "download";
            a.click();
        }
    }
};
