(() => {
    const modal = document.querySelector('[data-team-restore-modal]');
    if (!modal) return;
    const show = () => bootstrap.Modal.getOrCreateInstance(modal).show();
    document.addEventListener('click', event => {
        if (!event.target.closest('[data-team-restore]')) return;
        const menu = document.querySelector('#leftMenu.show');
        if (menu) {
            menu.addEventListener('hidden.bs.offcanvas', show, { once: true });
            bootstrap.Offcanvas.getOrCreateInstance(menu).hide();
        } else show();
    });
    if (modal.dataset.showOnLoad === 'true') show();
})();

(() => {
    const list = document.querySelector('[data-environment-order-list]');
    const save = document.querySelector('[data-environment-save-order]');
    const status = document.querySelector('[data-environment-order-status]');
    if (!list || !save) return;
    const order = () => [...list.querySelectorAll('input[name="environmentNames"]')].map(input => input.value);
    const initialOrder = JSON.stringify(order());

    const rows = () => [...list.querySelectorAll('[data-environment-order-row]')];
    const update = (row) => {
        const changed = JSON.stringify(order()) !== initialOrder;
        save.disabled = !changed;
        const name = row.querySelector('input[name="environmentNames"]').value;
        status.textContent = `${name} moved to position ${rows().indexOf(row) + 1} of ${rows().length}. ${changed ? 'Save order to apply changes.' : 'Original order restored.'}`;
    };
    let drag = null;
    let scrollFrame;
    const place = () => {
        if (!drag) return;
        const next = rows().find(row => row !== drag.row && drag.y < row.getBoundingClientRect().top + row.getBoundingClientRect().height / 2);
        list.insertBefore(drag.row, next || null);
    };
    const autoScroll = () => {
        if (!drag) return;
        const body = list.closest('.modal-body');
        const bounds = body.getBoundingClientRect();
        const delta = drag.y < bounds.top + 40 ? -8 : drag.y > bounds.bottom - 40 ? 8 : 0;
        if (delta) {
            body.scrollTop += delta;
            place();
        }
        scrollFrame = requestAnimationFrame(autoScroll);
    };
    const finish = (cancel = false) => {
        if (!drag) return;
        const current = drag;
        drag = null;
        cancelAnimationFrame(scrollFrame);
        if (cancel) list.append(...current.originalRows);
        current.row.classList.remove('is-dragging');
        list.classList.remove('is-dragging');
        if (list.hasPointerCapture(current.pointerId)) list.releasePointerCapture(current.pointerId);
        current.handle.focus({ preventScroll: true });
        update(current.row);
    };
    list.addEventListener('pointerdown', event => {
        const handle = event.target.closest('[data-environment-drag-handle]');
        if (!handle || handle.disabled || !event.isPrimary || event.button !== 0 || drag) return;
        event.preventDefault();
        handle.focus({ preventScroll: true });
        drag = { row: handle.closest('[data-environment-order-row]'), handle, pointerId: event.pointerId, y: event.clientY, originalRows: rows() };
        drag.row.classList.add('is-dragging');
        list.classList.add('is-dragging');
        list.setPointerCapture(event.pointerId);
        scrollFrame = requestAnimationFrame(autoScroll);
    });
    list.addEventListener('pointermove', event => {
        if (!drag || event.pointerId !== drag.pointerId) return;
        drag.y = event.clientY;
        place();
    });
    list.addEventListener('pointerup', event => {
        if (drag && event.pointerId === drag.pointerId) finish();
    });
    list.addEventListener('pointercancel', () => finish(true));
    list.addEventListener('lostpointercapture', () => finish(true));
    list.closest('.modal').addEventListener('hide.bs.modal', () => finish(true));
    list.addEventListener('keydown', event => {
        if (drag && event.key === 'Escape') {
            event.preventDefault();
            event.stopPropagation();
            finish(true);
            return;
        }
        const handle = event.target.closest('[data-environment-drag-handle]');
        if (!handle || drag || !['ArrowUp', 'ArrowDown'].includes(event.key)) return;
        event.preventDefault();
        const row = handle.closest('[data-environment-order-row]');
        if (event.key === 'ArrowUp' && row.previousElementSibling) list.insertBefore(row, row.previousElementSibling);
        if (event.key === 'ArrowDown' && row.nextElementSibling) list.insertBefore(row.nextElementSibling, row);
        handle.focus();
        update(row);
    });
})();

document.addEventListener('click', async event => {
    const button = event.target.closest('[data-copy-url]');
    if (!button) return;
    try {
        await navigator.clipboard.writeText(button.dataset.copyUrl);
        button.textContent = 'Copied';
        setTimeout(() => button.textContent = 'Copy', 1500);
    } catch { button.textContent = 'Copy failed'; }
});
(() => {
    const selected = new Set();
    const tags = [...document.querySelectorAll('[data-project-filter]')];
    const all = document.querySelector('[data-project-all]');
    if (!all) return;

    const update = () => {
        for (const tag of tags) {
            const active = selected.has(tag.dataset.projectFilter);
            tag.setAttribute('aria-pressed', String(active));
        }
        all.setAttribute('aria-pressed', String(selected.size === 0));
        all.classList.toggle('btn-primary', selected.size === 0);
        all.classList.toggle('btn-outline-primary', selected.size > 0);
        let visible = 0;
        for (const entry of document.querySelectorAll('[data-project-entry]')) {
            const projects = JSON.parse(entry.dataset.projects || '[]');
            entry.hidden = selected.size > 0 && !projects.some(project => selected.has(project));
            if (!entry.hidden && !entry.closest('[data-environment-column]')?.hidden) visible++;
        }
        const empty = document.getElementById('projectFilterEmpty');
        if (empty) empty.hidden = selected.size === 0 || visible > 0;
    };

    for (const tag of tags) {
        tag.addEventListener('click', () => {
            const project = tag.dataset.projectFilter;
            if (selected.has(project)) selected.delete(project);
            else selected.add(project);
            update();
        });
    }
    all.addEventListener('click', () => { selected.clear(); update(); });
    document.addEventListener('environment-page-change', update);
})();
