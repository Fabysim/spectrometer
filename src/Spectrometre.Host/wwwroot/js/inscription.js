// Chargé une seule fois depuis App.razor (voir commentaire à côté de la balise <script src="js/inscription.js">) :
// un <script> placé directement dans une page @page ne s'exécute pas lors d'une navigation enrichie
// (enhanced navigation) de Blazor, seulement lors d'un chargement complet de la page — d'où l'usage
// d'écouteurs délégués sur document plutôt que de câblage fait au chargement du script.

function spectrometreSuggestPassword() {
    const upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    const lower = "abcdefghijkmnpqrstuvwxyz";
    const digits = "23456789";
    const special = "!@#$%^&*-_=+?";
    const all = upper + lower + digits + special;
    const pick = set => set[Math.floor(Math.random() * set.length)];
    const length = 14;
    let chars = [pick(upper), pick(lower), pick(digits), pick(special)];
    for (let i = chars.length; i < length; i++) chars.push(pick(all));
    for (let i = chars.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [chars[i], chars[j]] = [chars[j], chars[i]];
    }
    const password = chars.join('');
    const mdp = document.getElementById('motDePasse');
    const confirmation = document.getElementById('confirmationMotDePasse');
    if (!mdp || !confirmation) return;
    mdp.value = password;
    confirmation.value = password;
    spectrometreUpdateStrength(password);
}

function spectrometreUpdateStrength(password) {
    const el = document.getElementById('forceIndicateur');
    if (!el) return;

    let score = 0;
    if (password.length >= 6) score++;
    if (password.length >= 10) score++;
    if (/[a-z]/.test(password) && /[A-Z]/.test(password)) score++;
    if (/\d/.test(password)) score++;
    if (/[^A-Za-z0-9]/.test(password)) score++;

    // Libellés/préfixe posés en attributs data-* par la page Razor (résolus dans la culture du rendu
    // courant) plutôt qu'injectés en JS ci-dessus : ce script est chargé une seule fois pour toute la
    // session, il ne peut donc pas porter de texte localisé figé au premier chargement.
    const labels = (el.dataset.strengthLabels || '').split('|');
    const prefix = el.dataset.strengthPrefix || '';
    el.textContent = prefix + " " + (password.length === 0 ? "—" : (labels[Math.min(score, labels.length - 1)] || ''));
}

function spectrometreTogglePasswordVisibility(button) {
    const input = document.getElementById(button.getAttribute('data-toggle-password'));
    const icon = button.querySelector('i');
    if (!input) return;

    const showing = input.type === 'text';
    input.type = showing ? 'password' : 'text';

    if (icon) {
        icon.classList.toggle('bi-eye', showing);
        icon.classList.toggle('bi-eye-slash', !showing);
    }

    // Libellé/aria-label posés en attributs data-* par la page Razor (voir spectrometreUpdateStrength
    // ci-dessus pour la même raison) plutôt qu'injectés ici.
    const label = showing ? button.dataset.labelShow : button.dataset.labelHide;
    if (label) {
        button.setAttribute('aria-label', label);
        button.setAttribute('title', label);
    }
}

// Délégation sur document (même raison que pour spectrometreOnProfilChange ci-dessous) : couvre les deux
// boutons œil (mot de passe + confirmation) sans câblage individuel, et reste valide même si la page est
// réémise par le serveur après un échec de validation.
document.addEventListener('click', function (e) {
    const button = e.target.closest && e.target.closest('[data-toggle-password]');
    if (button) spectrometreTogglePasswordVisibility(button);
});

function spectrometreOnProfilChange() {
    const entreprise = document.querySelector('.profil-choix input[type=radio][value=Entreprise]');
    const champ = document.getElementById('champNomEntreprise');
    if (champ) champ.style.display = (entreprise && entreprise.checked) ? '' : 'none';
}

function spectrometreInscriptionValidateStep1() {
    const ids = ['inscription-nom', 'inscription-prenom', 'inscription-email', 'motDePasse', 'confirmationMotDePasse'];
    for (const id of ids) {
        const el = document.getElementById(id);
        if (!el) continue;
        if (typeof el.reportValidity === 'function' && !el.reportValidity()) {
            return false;
        }
        if (!el.value || !String(el.value).trim()) {
            el.focus();
            return false;
        }
    }
    const mdp = document.getElementById('motDePasse');
    const confirmation = document.getElementById('confirmationMotDePasse');
    if (mdp && confirmation && mdp.value !== confirmation.value) {
        confirmation.setCustomValidity(confirmation.dataset.mismatchMessage || 'Mismatch');
        confirmation.reportValidity();
        confirmation.setCustomValidity('');
        return false;
    }
    return true;
}

function spectrometreInscriptionGoToStep(step, options) {
    const wizard = document.getElementById('inscription-wizard');
    if (!wizard) return;

    const skipValidation = options && options.skipValidation;
    if (step === 2 && !skipValidation && !spectrometreInscriptionValidateStep1()) {
        return;
    }

    wizard.setAttribute('data-step', String(step));

    const step1 = document.getElementById('inscription-step-1');
    const step2 = document.getElementById('inscription-step-2');
    if (step1) step1.hidden = step !== 1;
    if (step2) step2.hidden = step !== 2;

    document.querySelectorAll('[data-step-marker]').forEach(function (marker) {
        marker.classList.toggle('active', Number(marker.getAttribute('data-step-marker')) <= step);
    });
    const line = document.querySelector('.inscription-step-line');
    if (line) line.classList.toggle('active', step >= 2);

    const subtitle = document.getElementById('inscription-subtitle');
    if (subtitle) {
        subtitle.textContent = step === 2
            ? (subtitle.dataset.step2 || '')
            : (subtitle.dataset.step1 || '');
    }

    if (step === 2) {
        spectrometreOnProfilChange();
    }

    if (!(options && options.silent)) {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
}

function spectrometreInscriptionInitWizard() {
    const wizard = document.getElementById('inscription-wizard');
    if (!wizard) return;
    const step = Number(wizard.getAttribute('data-step') || '1');
    spectrometreInscriptionGoToStep(step === 2 ? 2 : 1, { skipValidation: true, silent: true });
}

// Délégation sur document (plutôt que d'attacher l'écouteur aux <input> radio trouvés au chargement du
// script) : InputRadio (Blazor) ne relaie pas les attributs HTML bruts comme "onchange" (contrairement à
// InputText), et les nœuds radio peuvent être réémis par un rendu serveur ultérieur (ex. après un échec de
// validation) — un écouteur direct attaché une seule fois cesserait de fonctionner sur les nouveaux nœuds.
document.addEventListener('change', function (e) {
    if (e.target && e.target.matches('.profil-choix input[type=radio]:not([disabled])')) {
        spectrometreOnProfilChange();
    }
});

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', spectrometreInscriptionInitWizard);
} else {
    spectrometreInscriptionInitWizard();
}
document.addEventListener('blazor:navigated', spectrometreInscriptionInitWizard);
