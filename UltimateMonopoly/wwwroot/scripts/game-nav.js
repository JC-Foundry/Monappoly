(() => {
    'use strict';

    // ---- Floating status alert: auto-dismiss after 5s ----
    const floatingAlert = document.querySelector('[data-floating-alert] .alert');
    if (floatingAlert) {
        setTimeout(() => {
            bootstrap.Alert.getOrCreateInstance(floatingAlert).close();
        }, 5000);
    }

    const nav = document.querySelector('[data-game-nav]');
    const modalEl = document.getElementById('navConfirmModal');
    if (!nav || !modalEl) return;

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    let pendingNav = null;

    // Any nav-bar link or the Go Back button is gated by a leave-confirmation.
    nav.querySelectorAll('a[href], [data-nav-back]').forEach((el) => {
        el.addEventListener('click', (e) => {
            e.preventDefault();
            pendingNav = el.hasAttribute('data-nav-back')
                ? () => history.back()
                : () => { window.location.href = el.getAttribute('href'); };
            modal.show();
        });
    });

    modalEl.querySelector('[data-nav-confirm-go]').addEventListener('click', () => {
        modal.hide();
        if (pendingNav) pendingNav();
    });
})();