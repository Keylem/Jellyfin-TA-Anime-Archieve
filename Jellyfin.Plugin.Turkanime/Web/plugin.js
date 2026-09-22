(() => {
  const pageUrl = '/web/pluginturkanime/turkanime.html';
  const navText = 'TürkAnime';

  const injectNav = () => {
    const navContainers = document.querySelectorAll('header nav, .mainDrawer .itemsContainer');

    navContainers.forEach((container) => {
      if (container.querySelector('[data-turkanime-link="1"]')) {
        return;
      }

      const anchor = document.createElement('a');
      anchor.textContent = navText;
      anchor.href = pageUrl;
      anchor.dataset.turkanimeLink = '1';
      anchor.className = 'navMenuOption lnk';
      container.appendChild(anchor);
    });
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', injectNav);
  } else {
    injectNav();
  }
})();
