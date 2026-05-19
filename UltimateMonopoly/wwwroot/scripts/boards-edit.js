// Boards Edit page — populate the create/edit and delete modals from the
// trigger button's data-* attributes on show.bs.modal.

(() => {
    const editModal = document.getElementById('spaceEditModal');
    if (editModal) {
        const indexInput   = editModal.querySelector('[data-modal-index]');
        const spaceIdInput = editModal.querySelector('[data-modal-space-id]');
        const defaultNameEl = editModal.querySelector('[data-modal-default-name]');
        const nameInput    = editModal.querySelector('[data-modal-name-input]');
        const titleEl      = editModal.querySelector('[data-modal-title]');
        const form         = editModal.querySelector('form');

        editModal.addEventListener('show.bs.modal', (e) => {
            const t = e.relatedTarget;
            form?.reset();

            const isEdit = Boolean(t?.dataset.spaceCustomId);
            if (titleEl)       titleEl.textContent = isEdit ? 'Edit custom space' : 'Customise space';
            if (indexInput)    indexInput.value    = t?.dataset.spaceIndex ?? '';
            if (spaceIdInput)  spaceIdInput.value  = t?.dataset.spaceCustomId ?? '';
            if (defaultNameEl) defaultNameEl.textContent = t?.dataset.spaceDefaultName ?? '';
            if (nameInput) {
                nameInput.value = t?.dataset.spaceCurrentName ?? '';
                // Focus + select once the modal has finished animating in.
                editModal.addEventListener('shown.bs.modal', () => nameInput.focus(), { once: true });
            }
        });
    }

    const deleteModal = document.getElementById('spaceDeleteModal');
    if (deleteModal) {
        const spaceIdInput  = deleteModal.querySelector('[data-modal-space-id]');
        const currentNameEl = deleteModal.querySelector('[data-modal-current-name]');
        const defaultNameEl = deleteModal.querySelector('[data-modal-default-name]');

        deleteModal.addEventListener('show.bs.modal', (e) => {
            const t = e.relatedTarget;
            if (spaceIdInput)   spaceIdInput.value  = t?.dataset.spaceCustomId ?? '';
            if (currentNameEl)  currentNameEl.textContent = t?.dataset.spaceCurrentName ?? '';
            if (defaultNameEl)  defaultNameEl.textContent = t?.dataset.spaceDefaultName ?? '';
        });
    }
})();
