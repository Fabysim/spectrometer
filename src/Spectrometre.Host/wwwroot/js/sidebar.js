(function () {
  'use strict';

  window.spectrometreToggleSidebar = function (event) {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    var sidebar = document.getElementById('appSidebar');
    if (!sidebar || window.innerWidth < 768) return;
    sidebar.classList.toggle('collapsed');
  };

  window.spectrometreToggleMobileSidebar = function () {
    var sidebar = document.getElementById('appSidebar');
    var overlay = document.getElementById('sidebarOverlay');
    if (!sidebar) return;
    sidebar.classList.toggle('mobile-open');
    if (overlay) overlay.classList.toggle('active', sidebar.classList.contains('mobile-open'));
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
})();
