(() => {
    const input = document.getElementById('menu-search-input');
    if (!input) return;
    const cards = Array.from(document.querySelectorAll('#home-menu-grid .menu-card'));
    const count = document.getElementById('menu-result-count');
    const empty = document.getElementById('menu-search-empty');
    const normalize = value => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLocaleLowerCase('vi').trim();
    const items = cards.map(card => ({ card, name: normalize(card.querySelector('h3').textContent) }));
    const filter = () => {
        const query = normalize(input.value);
        let visible = 0;
        items.forEach(({ card, name }) => {
            card.hidden = !name.includes(query);
            if (!card.hidden) visible++;
        });
        count.textContent = query ? `${visible} / ${cards.length} món` : `${cards.length} món`;
        empty.hidden = visible > 0;
    };
    input.closest('.menu-search').hidden = false;
    input.addEventListener('input', filter);
    document.getElementById('menu-search-clear').addEventListener('click', () => {
        input.value = '';
        filter();
        input.focus();
    });
    filter();
})();
