(() => {
    const form = document.getElementById('dish-form');
    if (!form) return;
    function indexRows(body) {
        body.querySelectorAll('tr').forEach((row, index) => {
            row.querySelectorAll('[data-field]').forEach(input => {
                input.name = body.dataset.prefix + '[' + index + '].' + input.dataset.field;
                // Indexed labels are not used; aria-label is stable after reordering.
                input.removeAttribute('id');
            });
        });
    }
    document.querySelectorAll('[data-add]').forEach(button => {
        button.addEventListener('click', () => {
            const body = document.getElementById(button.dataset.add);
            body.append(document.getElementById(button.dataset.template).content.cloneNode(true));
            indexRows(body);
        });
    });
    form.addEventListener('click', event => {
        const button = event.target.closest('[data-remove]');
        if (!button) return;
        const body = button.closest('tbody');
        button.closest('tr').remove();
        indexRows(body);
    });
    form.addEventListener('submit', () => form.querySelectorAll('tbody[data-prefix]').forEach(indexRows));
})();
