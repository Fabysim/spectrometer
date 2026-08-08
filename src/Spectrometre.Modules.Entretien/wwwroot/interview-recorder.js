window.interviewRecorder = (function () {
    let mediaRecorder = null;
    let chunks = [];
    let stream = null;
    let intervalId = null;
    let firstTickId = null;
    let dotNetRef = null;
    let busy = false;
    let stopped = false;
    let batchSeq = 0;

    function pickMimeType() {
        const candidates = [
            "audio/webm;codecs=opus",
            "audio/webm",
            "audio/ogg;codecs=opus",
            "audio/mp4"
        ];
        for (const t of candidates) {
            if (MediaRecorder.isTypeSupported(t)) return t;
        }
        return "";
    }

    function waitForNextChunk(timeoutMs) {
        return new Promise((resolve) => {
            let settled = false;
            const done = () => {
                if (settled) return;
                settled = true;
                mediaRecorder.removeEventListener("dataavailable", onData);
                resolve();
            };
            const onData = (e) => {
                if (e.data && e.data.size > 0) {
                    chunks.push(e.data);
                    done();
                }
            };
            mediaRecorder.addEventListener("dataavailable", onData);
            mediaRecorder.requestData();
            setTimeout(done, timeoutMs);
        });
    }

    async function waitForIdle() {
        while (busy) {
            await new Promise(r => setTimeout(r, 50));
        }
    }

    async function sendCurrentAudio(isFinal) {
        if (!mediaRecorder || mediaRecorder.state === "inactive" || !dotNetRef) return;
        if (stopped && !isFinal) return;

        busy = true;
        const seq = ++batchSeq;
        try {
            await waitForNextChunk(500);

            if (chunks.length === 0) {
                console.warn("[interviewRecorder] Aucun chunk audio disponible.");
                return;
            }

            const mimeType = mediaRecorder.mimeType || pickMimeType() || "audio/webm";
            const fullBlob = new Blob(chunks, { type: mimeType });
            if (fullBlob.size === 0) {
                console.warn("[interviewRecorder] Blob audio vide.");
                return;
            }

            const buffer = await fullBlob.arrayBuffer();
            const streamRef = DotNet.createJSStreamReference(buffer);
            await dotNetRef.invokeMethodAsync(
                isFinal ? "OnFinalAudioReady" : "OnAudioSegmentReady",
                streamRef,
                mimeType,
                seq
            );
        } catch (err) {
            if (stopped && !isFinal) return;
            console.error("[interviewRecorder] Erreur envoi audio:", err);
            try {
                await dotNetRef.invokeMethodAsync("OnRecorderError", String(err));
            } catch (_) { /* ignore */ }
        } finally {
            busy = false;
        }
    }

    function releaseMedia() {
        if (mediaRecorder && mediaRecorder.state !== "inactive") {
            mediaRecorder.stop();
        }
        if (stream) {
            stream.getTracks().forEach(t => t.stop());
        }
        mediaRecorder = null;
        stream = null;
    }

    async function start(dotNetReference, intervalSeconds) {
        dotNetRef = dotNetReference;
        chunks = [];
        busy = false;
        stopped = false;
        batchSeq = 0;

        stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const mimeType = pickMimeType();
        mediaRecorder = mimeType
            ? new MediaRecorder(stream, { mimeType })
            : new MediaRecorder(stream);

        mediaRecorder.ondataavailable = (e) => {
            if (e.data && e.data.size > 0) chunks.push(e.data);
        };

        mediaRecorder.start(1000);

        firstTickId = setTimeout(() => {
            if (!stopped && !busy) sendCurrentAudio(false);
        }, 3000);

        intervalId = setInterval(() => {
            if (stopped || busy) return;
            sendCurrentAudio(false);
        }, intervalSeconds * 1000);
    }

    function stop() {
        return new Promise((resolve) => {
            if (!mediaRecorder) { resolve(); return; }

            stopped = true;
            clearInterval(intervalId);
            clearTimeout(firstTickId);
            intervalId = null;
            firstTickId = null;

            (async () => {
                try {
                    await waitForIdle();
                    await sendCurrentAudio(true);
                } finally {
                    releaseMedia();
                    resolve();
                }
            })();
        });
    }

    /** Arrête le micro sans envoyer l'audio final (navigation / dispose). */
    function cancel() {
        stopped = true;
        clearInterval(intervalId);
        clearTimeout(firstTickId);
        intervalId = null;
        firstTickId = null;
        releaseMedia();
    }

    return { start, stop, cancel };
})();
