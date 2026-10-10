(() => {
    const modal = document.getElementById('editUrlModal');
    if (!modal) return;
    modal.addEventListener('show.bs.modal', event => {
        const button = event.relatedTarget;
        if (!button) return;
        document.getElementById('editUrlId').value = button.dataset.urlId;
        document.getElementById('editUrlTeam').value = button.dataset.urlTeam;
        document.getElementById('editUrlValue').value = button.dataset.urlValue;
        document.getElementById('editUrlDescription').value = button.dataset.urlDescription;
        document.getElementById('editUrlEnvironment').value = button.dataset.urlEnvironment;
        const projects = JSON.parse(button.dataset.urlProjects || '[]');
        for (const option of document.getElementById('editUrlProjects').options) {
            option.selected = projects.includes(option.value);
        }
    });
})();
