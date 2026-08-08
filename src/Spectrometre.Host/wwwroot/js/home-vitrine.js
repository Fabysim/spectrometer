/**
 * Home vitrine : navbar + scroll reveal
 * Stratégie : tout est visible par défaut (CSS html:not([data-scroll-ready])),
 * le JS active les animations progressivement en posant data-scroll-ready sur <html>.
 */
(function () {
  'use strict';

  function initNavbarVitrine() {
    var header = document.querySelector('.navbar-vitrine');
    if (!header) return;
    if (header._onScroll) window.removeEventListener('scroll', header._onScroll);
    function onScroll() {
      header.classList.toggle('navbar-vitrine--scrolled', window.scrollY > 40);
    }
    header._onScroll = onScroll;
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
  }

  function initScrollReveal() {
    var elements = document.querySelectorAll('.scroll-reveal');
    if (!elements.length) return;

    function revealVisibleNow() {
      var vh = window.innerHeight;
      document.querySelectorAll('.scroll-reveal:not(.scroll-reveal--visible)').forEach(function (el) {
        var rect = el.getBoundingClientRect();
        if (rect.top < vh && rect.bottom > 0) {
          el.classList.add('scroll-reveal--visible');
        }
      });
    }

    // Marquer les éléments déjà dans le viewport comme visibles
    // AVANT de poser data-scroll-ready (sinon ils flashent opacity:0→1)
    revealVisibleNow();

    // Activer les animations CSS : à partir d'ici, les éléments non visibles
    // auront opacity:0 et s'animeront à l'entrée dans le viewport
    document.documentElement.setAttribute('data-scroll-ready', '1');

    // IntersectionObserver pour les éléments hors viewport
    var observer = new IntersectionObserver(function (entries) {
      entries.forEach(function (entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add('scroll-reveal--visible');
          observer.unobserve(entry.target);
        }
      });
    }, { rootMargin: '0px', threshold: 0 });

    document.querySelectorAll('.scroll-reveal:not(.scroll-reveal--visible)').forEach(function (el) {
      observer.observe(el);
    });

    window.addEventListener('scroll', revealVisibleNow, { passive: true });

    window.addEventListener('pageshow', revealVisibleNow);
    document.addEventListener('visibilitychange', function () {
      if (document.visibilityState === 'visible') revealVisibleNow();
    });
  }

  function initMobileMenu() {
    var menu = document.getElementById('navbarVitrineMenu');
    if (!menu) return;
    document.querySelectorAll('#navbarVitrineMenu .nav-link-vitrine, #navbarVitrineMenu .btn-demarrer').forEach(function (link) {
      link.addEventListener('click', function () {
        if (menu.classList.contains('show')) {
          menu.classList.remove('show');
          var toggle = document.querySelector('.navbar-vitrine [data-bs-target="#navbarVitrineMenu"]');
          if (toggle) toggle.setAttribute('aria-expanded', 'false');
        }
      });
    });
  }

  window._initHomeVitrine = function () {
    // Retirer data-scroll-ready pour reset les animations si on revient sur la page
    document.documentElement.removeAttribute('data-scroll-ready');

    if (!document.querySelector('.navbar-vitrine') && !document.querySelector('.scroll-reveal')) return;
    initNavbarVitrine();
    initScrollReveal();
    initMobileMenu();
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', window._initHomeVitrine);
  } else {
    window._initHomeVitrine();
  }
})();
