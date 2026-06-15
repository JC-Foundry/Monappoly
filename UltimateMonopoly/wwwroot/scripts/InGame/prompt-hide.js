// Hidable prompts (phone profile + host play-page drawer).
//
// A pending engine prompt renders as an overlay (.pp-prompt) that covers the profile, so the
// viewer can't browse properties / cards / loans while one is open. This lets them *hide* (NOT
// cancel) the overlay: the prompt stays pending server-side and keeps waiting for an answer —
// only the client-side overlay is hidden. A floating "Open Prompt" bar brings it back.
//
// Two hosts run the same partial, so both get wired:
//   • Phone profile — the [data-player] wrapper (Player/Index); player-state.js swaps its
//     innerHTML on each live frame.
//   • Host drawer — the [data-drawer-content] wrapper (the play page's left pull-out);
//     player-drawer.js swaps its innerHTML on open / step / live frame.
//
// State is a single promptId per host — the prompt the viewer chose to hide. It is keyed off the
// prompt's data-prompt-id so a *different* prompt is never auto-hidden: when a new prompt (new id)
// arrives — including the host stepping to another player — it shows normally. The hide is
// re-applied after every live partial swap (the re-rendered prompt defaults to visible) via a
// MutationObserver on the stable wrapper.
(function () {
    'use strict';

    // Wire one prompt host: a stable wrapper whose innerHTML is live-swapped. The controls live
    // inside the swapped partial, so delegate clicks on the (stable) wrapper and re-apply on swap.
    function wire(root) {
        let hiddenId = null;   // the data-prompt-id hidden in this host, or null

        function promptEl() { return root.querySelector('.pp-prompt'); }
        function reopenBtn() { return root.querySelector('[data-prompt-open]'); }

        // Reconcile the DOM with the hide state. Called on load, after each swap, and on toggle.
        function apply() {
            const prompt = promptEl();
            if (!prompt) {
                // Prompt resolved/gone (or no player loaded yet) — clear stale state.
                hiddenId = null;
                return;
            }
            const id = prompt.getAttribute('data-prompt-id');
            const hide = !!id && id === hiddenId;
            prompt.classList.toggle('pp-prompt--hidden', hide);
            const btn = reopenBtn();
            if (btn) btn.classList.toggle('d-none', !hide);
        }

        root.addEventListener('click', function (e) {
            if (e.target.closest('[data-prompt-hide]')) {
                const prompt = promptEl();
                hiddenId = prompt ? prompt.getAttribute('data-prompt-id') : null;
                apply();
                return;
            }
            if (e.target.closest('[data-prompt-open]')) {
                hiddenId = null;
                apply();
            }
        });

        // Re-apply after each live re-render (the freshly swapped-in prompt defaults to visible).
        new MutationObserver(apply).observe(root, { childList: true });

        apply();
    }

    document.querySelectorAll('[data-player], [data-drawer-content]').forEach(wire);
})();