(function () {
  'use strict';

  var STORAGE_COLLAPSED = 'spectrometre.sidebarCollapsed';
  var STORAGE_OPEN = 'spectrometre.navOpenGroups';
  var restoring = false;

  function getTooltipEl() {
    var el = document.getElementById('sidebarNavTooltip');
    if (el) return el;
    el = document.createElement('div');
    el.id = 'sidebarNavTooltip';
    el.className = 'sidebar-nav-tooltip';
    el.setAttribute('role', 'tooltip');
    el.hidden = true;
    document.body.appendChild(el);
    return el;
  }

  function labelOf(anchor) {
    if (!anchor) return '';
    var cached = anchor.getAttribute('data-tooltip');
    if (cached) return cached.trim();
    var span = anchor.querySelector('span:not(.nav-icon)');
    var text = span ? span.textContent.trim() : (anchor.getAttribute('aria-label') || '');
    if (text) anchor.setAttribute('data-tooltip', text);
    return text;
  }

  function isCollapsedDesktop() {
    var sidebar = document.getElementById('appSidebar');
    return !!(sidebar && sidebar.classList.contains('collapsed') && window.innerWidth >= 768);
  }

  function hideTooltip() {
    var el = document.getElementById('sidebarNavTooltip');
    if (!el) return;
    el.hidden = true;
    el.classList.remove('is-visible');
    el.textContent = '';
  }

  function showTooltip(anchor) {
    if (!isCollapsedDesktop() || !anchor) {
      hideTooltip();
      return;
    }
    var text = labelOf(anchor);
    if (!text) {
      hideTooltip();
      return;
    }

    var tip = getTooltipEl();
    tip.textContent = text;
    tip.hidden = false;
    tip.classList.add('is-visible');

    var rect = anchor.getBoundingClientRect();
    var tipRect = tip.getBoundingClientRect();
    var top = rect.top + (rect.height - tipRect.height) / 2;
    var left = rect.right + 10;
    var maxTop = window.innerHeight - tipRect.height - 8;
    tip.style.top = Math.max(8, Math.min(top, maxTop)) + 'px';
    tip.style.left = left + 'px';
  }

  function syncAllTooltips(root) {
    var scope = root || document.getElementById('appSidebar') || document;
    scope.querySelectorAll('.nav-item-auth').forEach(function (a) {
      labelOf(a);
    });
  }

  function readOpenGroups() {
    try {
      var raw = sessionStorage.getItem(STORAGE_OPEN);
      var parsed = raw ? JSON.parse(raw) : [];
      return Array.isArray(parsed) ? parsed.filter(function (x) { return typeof x === 'string' && x; }) : [];
    } catch (e) {
      return [];
    }
  }

  function writeOpenGroups(ids) {
    try {
      sessionStorage.setItem(STORAGE_OPEN, JSON.stringify(ids));
    } catch (e) { /* ignore quota / private mode */ }
  }

  /** Snapshot des groupes actuellement ouverts (DOM) → sessionStorage. */
  function saveOpenGroupsFromDom() {
    var opens = [];
    document.querySelectorAll('#appSidebar .nav-group.open[data-nav-group]').forEach(function (g) {
      var id = g.getAttribute('data-nav-group');
      if (id && opens.indexOf(id) < 0) opens.push(id);
    });
    writeOpenGroups(opens);
  }

  /**
   * Après un re-rendu Blazor, le markup ne remet `.open` que via IsGroupActive (URL courante).
   * On réapplique les groupes que l'utilisateur avait ouverts (ex. menu replié avec plusieurs
   * sous-menus visibles) pour qu'un clic sur un sous-item ne fasse pas disparaître les autres.
   */
  function restoreOpenGroups() {
    if (restoring) return;
    var sidebar = document.getElementById('appSidebar');
    if (!sidebar) return;

    restoring = true;
    try {
      var opens = readOpenGroups();

      // Fusionne ce que Blazor vient de marquer open (groupe de la page courante)
      sidebar.querySelectorAll('.nav-group.open[data-nav-group]').forEach(function (g) {
        var id = g.getAttribute('data-nav-group');
        if (id && opens.indexOf(id) < 0) opens.push(id);
      });
      writeOpenGroups(opens);

      opens.forEach(function (id) {
        var group = sidebar.querySelector('.nav-group[data-nav-group="' + id + '"]');
        if (!group) return;
        group.classList.add('open');
        var caret = group.querySelector('.nav-group-caret');
        if (caret) caret.setAttribute('aria-expanded', 'true');
      });
    } finally {
      restoring = false;
    }
  }

  function restoreCollapsed() {
    var sidebar = document.getElementById('appSidebar');
    if (!sidebar || window.innerWidth < 768) return;
    var v = null;
    try {
      v = sessionStorage.getItem(STORAGE_COLLAPSED);
    } catch (e) {
      return;
    }
    if (v === null) return;
    if (v === '1') sidebar.classList.add('collapsed');
    else sidebar.classList.remove('collapsed');
  }

  function persistCollapsed(sidebar) {
    try {
      sessionStorage.setItem(STORAGE_COLLAPSED, sidebar.classList.contains('collapsed') ? '1' : '0');
    } catch (e) { /* ignore */ }
  }

  window.spectrometreToggleSidebar = function (event) {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    var sidebar = document.getElementById('appSidebar');
    if (!sidebar || window.innerWidth < 768) return;
    sidebar.classList.toggle('collapsed');
    persistCollapsed(sidebar);
    // En repliant, on mémorise les sous-menus encore ouverts pour les garder visibles en mode icônes.
    saveOpenGroupsFromDom();
    hideTooltip();
  };

  window.spectrometreToggleMobileSidebar = function () {
    var sidebar = document.getElementById('appSidebar');
    var overlay = document.getElementById('sidebarOverlay');
    if (!sidebar) return;
    sidebar.classList.toggle('mobile-open');
    if (overlay) overlay.classList.toggle('active', sidebar.classList.contains('mobile-open'));
    hideTooltip();
  };

  window.spectrometreToggleNavGroup = function (button) {
    if (!button) return;
    var group = button.closest('.nav-group');
    if (!group) return;

    var sidebar = document.getElementById('appSidebar');
    if (sidebar && sidebar.classList.contains('collapsed') && window.innerWidth >= 768) {
      sidebar.classList.remove('collapsed');
      persistCollapsed(sidebar);
    }

    group.classList.toggle('open');
    button.setAttribute('aria-expanded', group.classList.contains('open') ? 'true' : 'false');

    var id = group.getAttribute('data-nav-group');
    if (id) {
      var opens = readOpenGroups();
      var idx = opens.indexOf(id);
      if (group.classList.contains('open')) {
        if (idx < 0) opens.push(id);
      } else if (idx >= 0) {
        opens.splice(idx, 1);
      }
      writeOpenGroups(opens);
    }

    hideTooltip();
    syncAllTooltips(group);
  };

  // Avant navigation enhanced : figer les groupes ouverts (sinon le prochain rendu Blazor les perd).
  document.addEventListener('click', function (e) {
    var subLink = e.target.closest && e.target.closest('#appSidebar .nav-submenu a');
    if (subLink) {
      saveOpenGroupsFromDom();
      return;
    }

    var sidebar = document.getElementById('appSidebar');
    if (!sidebar || !sidebar.classList.contains('mobile-open')) return;
    var link = e.target.closest && e.target.closest('.sidebar-nav a, .sidebar-footer a');
    if (link && window.innerWidth < 768) {
      sidebar.classList.remove('mobile-open');
      var overlay = document.getElementById('sidebarOverlay');
      if (overlay) overlay.classList.remove('active');
    }
  });

  document.addEventListener('mouseover', function (e) {
    var anchor = e.target.closest && e.target.closest('#appSidebar .nav-item-auth');
    if (!anchor) return;
    showTooltip(anchor);
  });

  document.addEventListener('mouseout', function (e) {
    var anchor = e.target.closest && e.target.closest('#appSidebar .nav-item-auth');
    if (!anchor) return;
    var related = e.relatedTarget;
    if (related && anchor.contains(related)) return;
    hideTooltip();
  });

  document.addEventListener('focusin', function (e) {
    var anchor = e.target.closest && e.target.closest('#appSidebar .nav-item-auth');
    if (anchor) showTooltip(anchor);
  });

  document.addEventListener('focusout', function () {
    hideTooltip();
  });

  document.addEventListener('scroll', hideTooltip, true);
  window.addEventListener('resize', hideTooltip);

  function boot() {
    restoreCollapsed();
    restoreOpenGroups();
    syncAllTooltips();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }

  document.addEventListener('enhancedload', boot);

  if (window.MutationObserver) {
    var restoreTimer = null;
    function observeSidebar(sb) {
      if (!sb || sb._tooltipObserver) return;
      sb._tooltipObserver = new MutationObserver(function () {
        if (restoring) return;
        syncAllTooltips(sb);
        // Debounce : Blazor peut patcher le DOM en plusieurs micro-étapes.
        if (restoreTimer) clearTimeout(restoreTimer);
        restoreTimer = setTimeout(function () {
          restoreCollapsed();
          restoreOpenGroups();
        }, 0);
      });
      sb._tooltipObserver.observe(sb, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ['class'] });
    }

    var sidebar = document.getElementById('appSidebar');
    if (sidebar) {
      observeSidebar(sidebar);
    } else {
      document.addEventListener('DOMContentLoaded', function () {
        observeSidebar(document.getElementById('appSidebar'));
      });
    }
  }
})();
