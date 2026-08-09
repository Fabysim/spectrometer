window.spectrometreAdmin = window.spectrometreAdmin || {};

window.spectrometreAdmin.getMultiSelectValues = function (elementId) {
    const el = document.getElementById(elementId);
    if (!el) return [];
    return Array.from(el.selectedOptions).map(function (o) { return o.value; });
};
