// Board share page — move friend cards between the "not shared" and "shared"
// columns. A card's hidden input is only submitted while it sits in the
// shared column (disabled inputs are excluded from the POST).

(() => {
    const root = document.querySelector('[data-board-share]');
    if (!root) return;

    const available = root.querySelector('[data-share-column="available"]');
    const shared = root.querySelector('[data-share-column="shared"]');
    if (!available || !shared) return;

    const countAvailable = root.querySelector('[data-count-available]');
    const countShared = root.querySelector('[data-count-shared]');

    const refresh = () => {
        [available, shared].forEach(col => {
            const cards = col.querySelectorAll('[data-friend-card]').length;
            const empty = col.querySelector('[data-empty]');
            if (empty) empty.hidden = cards > 0;
        });
        if (countAvailable) countAvailable.textContent = available.querySelectorAll('[data-friend-card]').length;
        if (countShared) countShared.textContent = shared.querySelectorAll('[data-friend-card]').length;
    };

    const moveCard = (card) => {
        const target = card.parentElement === available ? shared : available;
        const isShared = target === shared;

        target.insertBefore(card, target.querySelector('[data-empty]'));

        const input = card.querySelector('input[name="friendIds"]');
        if (input) input.disabled = !isShared;

        card.querySelector('[data-share-action]')?.classList.toggle('d-none', isShared);
        card.querySelector('[data-revoke-action]')?.classList.toggle('d-none', !isShared);
        card.classList.toggle('border-success', isShared);

        refresh();
    };

    root.querySelectorAll('[data-friend-card]').forEach(card => {
        card.addEventListener('click', () => moveCard(card));
        card.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                moveCard(card);
            }
        });
    });

    refresh();
})();