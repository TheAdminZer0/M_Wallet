window.barcodeScanner = (function () {
    let mediaStream = null;
    let animationFrameId = null;
    let detector = null;
    let fallbackReader = null;
    let usingFallback = false;
    let fallbackLibraryPromise = null;

    async function getDevices() {
        if (!navigator?.mediaDevices?.enumerateDevices) {
            throw new Error("Camera enumeration is not supported in this browser.");
        }

        const devices = await navigator.mediaDevices.enumerateDevices();
        let unnamedIndex = 1;

        return devices
            .filter(device => device.kind === "videoinput")
            .map(device => ({
                deviceId: device.deviceId,
                label: device.label || `Camera ${unnamedIndex++}`
            }));
    }

    async function ensureDetector() {
        if (detector) {
            return detector;
        }

        if ("BarcodeDetector" in window) {
            detector = new BarcodeDetector({ formats: ["code_128", "ean_13", "ean_8", "code_39", "upc_a", "upc_e"] });
            return detector;
        }

        throw new Error("BarcodeDetector API is not supported in this browser.");
    }

    async function loadFallbackLibrary() {
        if (fallbackLibraryPromise) {
            return fallbackLibraryPromise;
        }

        fallbackLibraryPromise = new Promise((resolve, reject) => {
            const existing = document.getElementById("zxing-lib");
            if (existing) {
                resolve();
                return;
            }

            const script = document.createElement("script");
            script.id = "zxing-lib";
            script.src = "https://cdn.jsdelivr.net/npm/@zxing/library@0.20.0/umd/index.min.js";
            script.onload = () => resolve();
            script.onerror = () => reject(new Error("Unable to load barcode fallback library."));
            document.head.appendChild(script);
        });

        return fallbackLibraryPromise;
    }

    async function ensureFallbackReader() {
        if (fallbackReader) {
            return fallbackReader;
        }

        await loadFallbackLibrary();
        if (!window.ZXing || !window.ZXing.BrowserMultiFormatReader) {
            throw new Error("Fallback reader is unavailable.");
        }

        fallbackReader = new window.ZXing.BrowserMultiFormatReader();
        return fallbackReader;
    }

    async function startFallback(videoElementId, video, dotNetRef, options) {
        const reader = await ensureFallbackReader();
        usingFallback = true;

        await reader.decodeFromVideoDevice(options?.deviceId ?? undefined, video, async (result, err) => {
            if (!usingFallback) {
                return;
            }

            if (result && result.getText()) {
                await dotNetRef.invokeMethodAsync("OnBarcodeDetected", result.getText());
                stop(videoElementId);
                return;
            }

            if (err && !(err instanceof ZXing.NotFoundException)) {
                console.error("Fallback barcode detection error", err);
            }
        });

        return { success: true };
    }

    function ensureMediaDevices() {
        const devices = navigator?.mediaDevices;
        if (!devices || typeof devices.getUserMedia !== "function") {
            throw new Error("Camera access is unavailable. Ensure you are using HTTPS and that your browser supports getUserMedia.");
        }

        return devices;
    }

    async function start(videoElementId, dotNetRef, options = {}) {
        stop(videoElementId);
        const video = document.getElementById(videoElementId);
        if (!video) {
            return { success: false, error: "Scanner surface not ready." };
        }

        try {
            const mediaDevices = ensureMediaDevices();
            const videoConstraints = options?.deviceId
                ? { deviceId: { exact: options.deviceId } }
                : { facingMode: "environment" };

            if ("BarcodeDetector" in window) {
                usingFallback = false;
                const detectorInstance = await ensureDetector();
                mediaStream = await mediaDevices.getUserMedia({
                    video: videoConstraints,
                    audio: false
                });

                video.srcObject = mediaStream;
                await video.play();

                const scanLoop = async () => {
                    if (!detectorInstance || !mediaStream) {
                        return;
                    }

                    try {
                        const barcodes = await detectorInstance.detect(video);
                        if (barcodes && barcodes.length > 0) {
                            const code = barcodes[0]?.rawValue;
                            if (code) {
                                await dotNetRef.invokeMethodAsync("OnBarcodeDetected", code);
                                return;
                            }
                        }
                    } catch (err) {
                        console.error("Barcode detection error", err);
                    }

                    animationFrameId = requestAnimationFrame(scanLoop);
                };

                animationFrameId = requestAnimationFrame(scanLoop);
                return { success: true };
            }

            return await startFallback(videoElementId, video, dotNetRef, options);
        } catch (error) {
            console.error("Unable to start barcode scanner", error);
            stop(videoElementId);
            return { success: false, error: error?.message ?? "Unable to access camera." };
        }
    }

    function stop(videoElementId) {
        if (animationFrameId) {
            cancelAnimationFrame(animationFrameId);
            animationFrameId = null;
        }

        if (fallbackReader) {
            try {
                fallbackReader.reset();
            } catch (err) {
                console.warn("Unable to reset fallback reader", err);
            }
        }

        usingFallback = false;

        if (mediaStream) {
            mediaStream.getTracks().forEach(track => track.stop());
            mediaStream = null;
        }

        const video = document.getElementById(videoElementId);
        if (video) {
            video.pause();
            video.srcObject = null;
        }
    }

    return {
        getDevices,
        start,
        stop
    };
})();
