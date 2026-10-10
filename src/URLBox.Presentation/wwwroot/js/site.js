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
