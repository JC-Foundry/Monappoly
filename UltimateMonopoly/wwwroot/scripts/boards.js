// Boards page — view toggle persisted to localStorage.

(() => {
    const cardsView = document.querySelector('[data-boards-view="cards"]');
    const listView = document.querySelector('[data-boards-view="list"]');
    const cardsRadio = document.getElementById('view-cards');
    const listRadio = document.getElementById('view-list');
    if (!cardsView || !listView || !cardsRadio || !listRadio) return;

    const KEY = 'boards.view';
    const apply = (mode) => {
        const useList = mode === 'list';
        cardsView.hidden = useList;
        listView.hidden = !useList;
        cardsRadio.checked = !useList;
        listRadio.checked = useList;
    };

    apply(localStorage.getItem(KEY) || 'cards');
    cardsRadio.addEventListener('change', () => { if (cardsRadio.checked) { apply('cards'); localStorage.setItem(KEY, 'cards'); } });
    listRadio.addEventListener('change', () => { if (listRadio.checked) { apply('list'); localStorage.setItem(KEY, 'list'); } });
})();
