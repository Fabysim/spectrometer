/* Calendrier Organisation — drag & drop (porté depuis mvp gdt-charts.js / window.gdtOrg) */
(function () {
    'use strict';

    window.gdtOrg = {
        _slotPx: 44,
        _gridStart: 0,
        _gridEnd: 24,
        _dotNet: null,
        _dragActId: null,
        _dragDropInstalled: false,
        _sommeilBlock: null,

        setSommeilBlock: function (cfg) {
            window.gdtOrg._sommeilBlock = cfg || null;
        },

        isSommeilHour: function (jour, hour) {
            var cfg = window.gdtOrg._sommeilBlock;
            if (!cfg || !cfg.jours || cfg.jours.indexOf(jour) < 0) return false;
            var typeStart = cfg.startHour;
            var typeEnd = cfg.endHour;
            if (typeEnd <= typeStart) typeEnd += 24;
            if (hour >= typeStart && hour < typeEnd) return true;
            if (typeEnd > 24) {
                var morningEnd = typeEnd - 24;
                if (hour >= window.gdtOrg._gridStart && hour < morningEnd) return true;
            }
            return false;
        },

        isSommeilOffset: function (jour, offsetY) {
            var g = window.gdtOrg;
            var rawH = offsetY / g._slotPx + g._gridStart;
            return g.isSommeilHour(jour, rawH);
        },

        snapTopPx: function (offsetY) {
            var g = this;
            var rawH = offsetY / g._slotPx + g._gridStart;
            var snapped = Math.round(rawH * 2) / 2;
            snapped = Math.max(g._gridStart, Math.min(g._gridEnd - 0.5, snapped));
            return (snapped - g._gridStart) * g._slotPx;
        },

        hideAllDropIndicators: function () {
            document.querySelectorAll('.org-drop-ind-js').forEach(function (el) {
                el.style.display = 'none';
            });
            document.querySelectorAll('.org-cal-dc--drag').forEach(function (el) {
                el.classList.remove('org-cal-dc--drag');
            });
        },

        updateDropIndicator: function (col, clientY) {
            var g = window.gdtOrg;
            document.querySelectorAll('.org-cal-dc--drag').forEach(function (c) {
                if (c !== col) c.classList.remove('org-cal-dc--drag');
            });
            col.classList.add('org-cal-dc--drag');

            var offsetY = clientY - col.getBoundingClientRect().top;
            var topPx = g.snapTopPx(offsetY);

            document.querySelectorAll('.org-drop-ind-js').forEach(function (el) {
                if (el.parentElement !== col) el.style.display = 'none';
            });

            var ind = col.querySelector('.org-drop-ind-js');
            if (!ind) {
                ind = document.createElement('div');
                ind.className = 'org-drop-ind org-drop-ind-js';
                col.appendChild(ind);
            }
            ind.style.display = 'block';
            ind.style.top = topPx + 'px';
        },

        installDragDrop: function (dotNetRef, slotPx, gridStart, gridEnd) {
            var g = window.gdtOrg;
            g._dotNet = dotNetRef;
            if (slotPx) g._slotPx = slotPx;
            if (gridStart != null) g._gridStart = gridStart;
            if (gridEnd != null) g._gridEnd = gridEnd;

            if (g._dragDropInstalled) return;
            g._dragDropInstalled = true;

            g._onDragStart = function (e) {
                var ab = e.target.closest && e.target.closest('.org-ab');
                if (!ab || !ab.closest('.org-page')) return;
                var id = parseInt(ab.getAttribute('data-act-id'), 10);
                if (isNaN(id)) return;
                g._dragActId = id;
                ab.classList.add('org-ab--drag');
                if (e.dataTransfer) {
                    e.dataTransfer.effectAllowed = 'move';
                    try { e.dataTransfer.setData('text/plain', String(id)); } catch (ex) { }
                }
            };

            g._onDragOver = function (e) {
                if (!g._dragActId) return;
                var col = e.target.closest && e.target.closest('.org-cal-dc');
                if (!col || !col.closest('.org-page')) return;
                var jour = parseInt(col.getAttribute('data-jour'), 10);
                if (isNaN(jour)) return;
                var offsetY = e.clientY - col.getBoundingClientRect().top;
                if (g.isSommeilOffset(jour, offsetY)) {
                    if (e.dataTransfer) e.dataTransfer.dropEffect = 'none';
                    g.hideAllDropIndicators();
                    return;
                }
                e.preventDefault();
                if (e.dataTransfer) e.dataTransfer.dropEffect = 'move';
                g.updateDropIndicator(col, e.clientY);
            };

            g._onDrop = function (e) {
                var col = e.target.closest && e.target.closest('.org-cal-dc');
                if (!col || !col.closest('.org-page')) return;
                e.preventDefault();
                var actId = g._dragActId;
                if (!actId && e.dataTransfer) {
                    actId = parseInt(e.dataTransfer.getData('text/plain'), 10);
                }
                if (!actId) return;
                var jour = parseInt(col.getAttribute('data-jour'), 10);
                if (isNaN(jour)) return;
                var offsetY = e.clientY - col.getBoundingClientRect().top;
                g.hideAllDropIndicators();
                g._dragActId = null;
                if (g.isSommeilOffset(jour, offsetY)) return;
                if (g._dotNet) {
                    g._dotNet.invokeMethodAsync('OnColumnDropFromJs', actId, jour, offsetY);
                }
            };

            g._onDragEnd = function (e) {
                var ab = e.target.closest && e.target.closest('.org-ab');
                if (ab) ab.classList.remove('org-ab--drag');
                g._dragActId = null;
                g.hideAllDropIndicators();
            };

            document.addEventListener('dragstart', g._onDragStart, true);
            document.addEventListener('dragover', g._onDragOver, true);
            document.addEventListener('drop', g._onDrop, true);
            document.addEventListener('dragend', g._onDragEnd, true);
        },

        disposeDragDrop: function () {
            var g = window.gdtOrg;
            if (!g._dragDropInstalled) return;
            document.removeEventListener('dragstart', g._onDragStart, true);
            document.removeEventListener('dragover', g._onDragOver, true);
            document.removeEventListener('drop', g._onDrop, true);
            document.removeEventListener('dragend', g._onDragEnd, true);
            g._dragDropInstalled = false;
            g._dotNet = null;
            g._dragActId = null;
            g.hideAllDropIndicators();
        },

        resolveCalendarClick: function (clientX, clientY) {
            var el = document.elementFromPoint(clientX, clientY);
            if (el && el.closest && el.closest('.org-ab')) return null;
            while (el) {
                if (el.classList && el.classList.contains('org-cal-dc')) {
                    var jour = parseInt(el.getAttribute('data-jour'), 10);
                    if (isNaN(jour)) return null;
                    var rect = el.getBoundingClientRect();
                    return { jour: jour, offsetY: clientY - rect.top };
                }
                if (el.classList && (el.classList.contains('org-cal-body') || el.classList.contains('org-page')))
                    break;
                el = el.parentElement;
            }
            return null;
        }
    };
})();
