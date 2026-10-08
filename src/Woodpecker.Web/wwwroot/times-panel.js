// Garde la ligne du puzzle en cours visible dans le tableau des temps : on ne scrolle que le
// conteneur (pas la page) et seulement si la ligne en sort.
window.woodpeckerScrollToCurrent = (container) => {
    const row = container?.querySelector('.time-current');
    if (!row) return;

    const margin = container.querySelector('thead')?.offsetHeight ?? 0;
    if (row.offsetTop < container.scrollTop + margin)
        container.scrollTop = row.offsetTop - margin;
    else if (row.offsetTop + row.offsetHeight > container.scrollTop + container.clientHeight)
        container.scrollTop = row.offsetTop + row.offsetHeight - container.clientHeight;
};
