(function () {
  'use strict';

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

  window.spectrometreToggleSidebar = function (event) {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    var sidebar = document.getElementById('appSidebar');
    if (!sidebar || window.innerWidth < 768) return;
    sidebar.classList.toggle('collapsed');
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
    }

    group.classList.toggle('open');
    button.setAttribute('aria-expanded', group.classList.contains('open') ? 'true' : 'false');
    hideTooltip();
    syncAllTooltips(group);
  };

  document.addEventListener('click', function (e) {
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
    syncAllTooltips();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', boot);
  } else {
    boot();
  }

  document.addEventListener('enhancedload', boot);

  if (window.MutationObserver) {
    function observeSidebar(sb) {
      if (!sb || sb._tooltipObserver) return;
      sb._tooltipObserver = new MutationObserver(function () {
        syncAllTooltips(sb);
      });
      sb._tooltipObserver.observe(sb, { childList: true, subtree: true, characterData: true });
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
