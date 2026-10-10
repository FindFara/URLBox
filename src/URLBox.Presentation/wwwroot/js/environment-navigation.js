(() => {
    const previous = document.querySelector('[data-environment-previous]');
    const next = document.querySelector('[data-environment-next]');
    if (!previous || !next) return;
    const columns = [...document.querySelectorAll('[data-environment-column]')];
    const pageSize = 5;
    let page = 0;
    const lastPage = Math.max(0, Math.ceil(columns.length / pageSize) - 1);
    const update = () => {
        columns.forEach((column, index) => {
            column.hidden = index < page * pageSize || index >= (page + 1) * pageSize;
        });
        previous.disabled = page === 0;
        next.disabled = page === lastPage;
        document.dispatchEvent(new Event('environment-page-change'));
    };
    previous.addEventListener('click', () => { page = Math.max(0, page - 1); update(); });
    next.addEventListener('click', () => { page = Math.min(lastPage, page + 1); update(); });
    update();
})();
