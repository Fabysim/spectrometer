// Safari detection + custom time picker + Blazor bridge for profil sommeil fields
(function () {
    var ua = navigator.userAgent;
    var isSafari = /^((?!chrome|android|crios|fxios|edg).)*safari/i.test(ua);
    if (isSafari) {
        document.documentElement.classList.add('gdt-safari');
    }
})();

function gdtPad2(n) {
    return String(n).padStart(2, '0');
}

function gdtParseTime24(value) {
    var parts = (value || '08:00').split(':');
    var h = Math.min(23, Math.max(0, parseInt(parts[0], 10) || 0));
    var m = Math.min(59, Math.max(0, parseInt(parts[1], 10) || 0));
    var ampm = h >= 12 ? 'PM' : 'AM';
    var h12 = h % 12;
    if (h12 === 0) h12 = 12;
    return { h24: h, m: m, h12: h12, ampm: ampm };
}

function gdtTo24h(h12, minute, ampm) {
    var h = parseInt(h12, 10) || 12;
    if (ampm === 'PM' && h !== 12) h += 12;
    if (ampm === 'AM' && h === 12) h = 0;
    return gdtPad2(h) + ':' + gdtPad2(minute);
}

var gdtActiveTimePopover = null;
var gdtTimePopoverListeners = null;
var gdtClockClickBound = false;

function gdtNeedsCustomTimePicker() {
    if (document.documentElement.classList.contains('gdt-safari')) return true;
    var ua = navigator.userAgent;
    if (!/AppleWebKit/i.test(ua)) return false;
    if (/Chrome|Chromium|CriOS|Edg/i.test(ua)) return false;
    return true;
}

function gdtNeedsWebKitFallback() {
    return gdtNeedsCustomTimePicker();
}

function gdtCloseTimePopover() {
    if (gdtActiveTimePopover) {
        gdtActiveTimePopover.remove();
        gdtActiveTimePopover = null;
    }
    if (gdtTimePopoverListeners) {
        document.removeEventListener('click', gdtTimePopoverListeners.onDocClick, true);
        document.removeEventListener('keydown', gdtTimePopoverListeners.onKeyDown, true);
        gdtTimePopoverListeners = null;
    }
}

function gdtSetInputValue(input, value) {
    var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value');
    if (setter && setter.set) {
        setter.set.call(input, value);
    } else {
        input.value = value;
    }
}

function gdtApplyTimeToInput(input, hourSel, minSel, ampmSel) {
    var value = gdtTo24h(hourSel.value, minSel.value, ampmSel.value);
    gdtSetInputValue(input, value);
    gdtNotifyTimeFromInput(input);
}

function gdtShowTimePopover(input) {
    gdtCloseTimePopover();

    var wrap = input.closest('.gdt-time-wrap') || input.parentElement;
    if (!wrap) return;

    var parsed = gdtParseTime24(input.value);
    var pop = document.createElement('div');
    pop.className = 'gdt-time-picker-popover';
    pop.setAttribute('role', 'dialog');
    pop.setAttribute('aria-label', 'Choisir une heure');

    var hourSel = document.createElement('select');
    hourSel.className = 'gdt-time-picker-part';
    for (var h = 1; h <= 12; h++) {
        var oh = document.createElement('option');
        oh.value = String(h);
        oh.textContent = String(h);
        if (h === parsed.h12) oh.selected = true;
        hourSel.appendChild(oh);
    }

    var sep = document.createElement('span');
    sep.className = 'gdt-time-picker-sep';
    sep.textContent = ':';

    var minSel = document.createElement('select');
    minSel.className = 'gdt-time-picker-part';
    for (var m = 0; m < 60; m += 5) {
        var om = document.createElement('option');
        om.value = gdtPad2(m);
        om.textContent = gdtPad2(m);
        if (m === Math.round(parsed.m / 5) * 5 % 60 || (parsed.m < 3 && m === 0)) {
            if (Math.abs(m - parsed.m) <= 2 || (m === 0 && parsed.m < 3)) om.selected = true;
        }
        minSel.appendChild(om);
    }
    // select closest 5-min
    var closest = Math.round(parsed.m / 5) * 5;
    if (closest === 60) closest = 55;
    minSel.value = gdtPad2(closest);

    var ampmSel = document.createElement('select');
    ampmSel.className = 'gdt-time-picker-part gdt-time-picker-ampm';
    ['AM', 'PM'].forEach(function (a) {
        var oa = document.createElement('option');
        oa.value = a;
        oa.textContent = a;
        if (a === parsed.ampm) oa.selected = true;
        ampmSel.appendChild(oa);
    });

    function onChange() { gdtApplyTimeToInput(input, hourSel, minSel, ampmSel); }
    hourSel.addEventListener('change', onChange);
    minSel.addEventListener('change', onChange);
    ampmSel.addEventListener('change', onChange);

    pop.appendChild(hourSel);
    pop.appendChild(sep);
    pop.appendChild(minSel);
    pop.appendChild(ampmSel);

    document.body.appendChild(pop);
    gdtActiveTimePopover = pop;

    var rect = (wrap.getBoundingClientRect ? wrap : input).getBoundingClientRect();
    pop.style.left = Math.max(8, rect.left) + 'px';
    pop.style.top = (rect.bottom + 6) + 'px';

    pop.dataset.gdtJustOpened = '1';
    setTimeout(function () { delete pop.dataset.gdtJustOpened; }, 200);

    function onDocClick(e) {
        if (pop.dataset.gdtJustOpened === '1') return;
        if (pop.contains(e.target)) return;
        if (input.contains(e.target)) return;
        var icon = e.target.closest && e.target.closest('.gdt-time-icon');
        if (icon && wrap.contains(icon)) return;
        gdtCloseTimePopover();
    }

    function onKeyDown(e) {
        if (e.key === 'Escape') gdtCloseTimePopover();
    }

    gdtTimePopoverListeners = { onDocClick: onDocClick, onKeyDown: onKeyDown };
    setTimeout(function () {
        document.addEventListener('click', onDocClick, true);
        document.addEventListener('keydown', onKeyDown, true);
        hourSel.focus();
    }, 0);
}

window.gdtOpenTimePicker = function (id) {
    var input = document.getElementById(id);
    if (!input) return;

    if (gdtNeedsCustomTimePicker()) {
        gdtShowTimePopover(input);
        return;
    }

    if (typeof input.showPicker === 'function') {
        try {
            input.showPicker();
            return;
        } catch (e) { /* no-op */ }
    }

    input.focus();
};

function gdtOnClockIconClick(e) {
    var btn = e.target.closest && e.target.closest('.gdt-time-icon[data-gdt-time-for]');
    if (!btn) return;
    var id = btn.getAttribute('data-gdt-time-for');
    if (!id) return;
    e.preventDefault();
    e.stopPropagation();
    window.gdtOpenTimePicker(id);
}

function gdtBindClockIconClicks() {
    if (gdtClockClickBound) return;
    document.addEventListener('click', gdtOnClockIconClick, true);
    gdtClockClickBound = true;
}

gdtBindClockIconClicks();

var gdtProfilBridge = null;
var gdtProfilBridgeBound = false;

function gdtNotifyTimeFromInput(input) {
    if (!gdtProfilBridge || !input) return;
    var sommeilField = input.getAttribute('data-gdt-sommeil-time');
    if (sommeilField) {
        gdtProfilBridge.invokeMethodAsync('GdtUpdateSommeilTimeFromJs', sommeilField, input.value || '');
    }
}

function gdtIsProfilTimeInput(input) {
    if (!input || !input.matches) return false;
    return input.matches('.gdt-fi--time[data-gdt-sommeil-time]');
}

function gdtOnDelegatedInput(ev) {
    if (!gdtNeedsWebKitFallback()) return;
    var input = ev.target;
    if (!gdtIsProfilTimeInput(input)) return;
    gdtNotifyTimeFromInput(input);
}

function gdtBindProfilBridgeListeners() {
    if (gdtProfilBridgeBound) return;
    document.addEventListener('input', gdtOnDelegatedInput, true);
    gdtProfilBridgeBound = true;
}

window.gdtInitProfilPsychosocialBridge = function (dotNetRef) {
    gdtProfilBridge = dotNetRef;
    gdtBindProfilBridgeListeners();
};

window.gdtDisposeProfilPsychosocialBridge = function () {
    gdtProfilBridge = null;
};
