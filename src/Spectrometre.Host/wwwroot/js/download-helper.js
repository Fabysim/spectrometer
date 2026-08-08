/**
 * Télécharge un fichier via fetch (credentials inclus) sans quitter la page Blazor.
 * Utilisé notamment pour « Rédiger une offre » — le NavigateTo(forceLoad) vers un
 * Content-Disposition:attachment ne remonte pas le composant, d'où un spinner bloqué.
 */
window.downloadFileFromUrl = async function (url, fallbackFileName) {
    const response = await fetch(url, { credentials: 'same-origin' });
    if (!response.ok) {
        let detail = '';
        try { detail = (await response.text()).trim(); } catch { /* ignore */ }
        throw new Error(detail || ('HTTP ' + response.status));
    }

    const blob = await response.blob();
    let fileName = fallbackFileName || 'document.docx';
    const cd = response.headers.get('content-disposition');
    if (cd) {
        const utf = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(cd);
        const plain = /filename\s*=\s*"([^"]+)"|filename\s*=\s*([^;]+)/i.exec(cd);
        if (utf) {
            try { fileName = decodeURIComponent(utf[1].trim()); } catch { /* keep fallback */ }
        } else if (plain) {
            fileName = (plain[1] || plain[2] || fileName).trim().replace(/^"|"$/g, '');
        }
    }

    const objectUrl = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = objectUrl;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(objectUrl);
};
