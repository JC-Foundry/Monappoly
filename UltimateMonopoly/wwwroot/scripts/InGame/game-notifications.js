// Engine notifications — lightweight, non-pausing toasts.
//
// The engine fires IEngineNotifier.Notify(gameId, targetPlayerId, message) as money
// moves (a GO collection, a card charge, …). It's broadcast to the whole game-play
// group as a "Notification" event; this module shows it ONLY on the target player's
// profile surface and auto-dismisses it after 5s (or on manual close).
//
// Targeting (PlayerId == the AppUser id both surfaces key on):
//   • Phone profile ([data-player]) — the page IS one player; show only their own.
//   • Host drawer ([data-player-drawer]) — show only while the drawer is open on the
//     target (player-drawer.js publishes the open player via data-open-user-id).
//
// The toast mounts OUTSIDE the StateChanged-swapped region (the phone body / the drawer
// shell, never the swapped profile partial) so a live re-render can't wipe it mid-life.
(function () {
    'use strict';

    if (!window.GamePlayHub) return;

    const AUTO_DISMISS_MS = 5000;

    // Resolve the mount for a notification aimed at targetPlayerId, or null if this
    // surface isn't currently showing that player (so it's silently ignored).
    function resolveMount(targetPlayerId) {
        const phone = document.querySelector('[data-player]');
        if (phone) {
            if (phone.dataset.userId !== targetPlayerId) return null;
            return ensureStack(document.body, 'engine-noti-stack-fixed');
        }

        const drawer = document.querySelector('[data-player-drawer]');
        if (drawer) {
            if (!drawer.classList.contains('open')) return null;
            if (drawer.dataset.openUserId !== targetPlayerId) return null;
            // Mount in the scrollable drawer body (not the swapped [data-drawer-content] inside it),
            // so it sticks to the top of the profile and survives each StateChanged re-render.
            const body = drawer.querySelector('.player-drawer-body') || drawer;
            return ensureStack(body, 'engine-noti-stack-drawer');
        }

        return null;
    }

    // One persistent stack container per surface, created on first use as the parent's FIRST
    // child (so the drawer's sticky stack pins to the top, and the fixed phone stack is fine
    // either way). Survives profile swaps — it's a sibling of the swapped region, not inside it.
    function ensureStack(parent, cls) {
        let stack = parent.querySelector(':scope > .' + cls);
        if (!stack) {
            stack = document.createElement('div');
            stack.className = 'engine-noti-stack ' + cls;
            parent.prepend(stack);
        }
        return stack;
    }

    function show(message, stack) {
        const alert = document.createElement('div');
        alert.className = 'alert alert-success alert-dismissible fade show engine-noti shadow-sm';
        alert.setAttribute('role', 'alert');
        const text = document.createElement('span');
        text.textContent = message;
        const close = document.createElement('button');
        close.type = 'button';
        close.className = 'btn-close';
        close.setAttribute('data-bs-dismiss', 'alert');
        close.setAttribute('aria-label', 'Close');
        alert.appendChild(text);
        alert.appendChild(close);
        stack.appendChild(alert);

        // Auto-dismiss after 5s via the Bootstrap Alert (fades, then removes itself).
        // Clear the timer if the user closes it first so we never double-close.
        let timer = null;
        const dismiss = () => {
            if (typeof bootstrap !== 'undefined') {
                bootstrap.Alert.getOrCreateInstance(alert).close();
            } else {
                alert.remove();
            }
        };
        timer = setTimeout(dismiss, AUTO_DISMISS_MS);
        alert.addEventListener('closed.bs.alert', () => { if (timer) clearTimeout(timer); });
    }

    GamePlayHub.on('Notification', function (msg) {
        if (!msg || !msg.message || !msg.targetPlayerId) return;
        const stack = resolveMount(msg.targetPlayerId);
        if (stack) show(msg.message, stack);
    });
})();