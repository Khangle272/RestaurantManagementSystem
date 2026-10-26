(() => {
    const form = document.getElementById('campaign-form');
    if (!form) return;
    const rows = document.getElementById('voucher-rows');
    let next = rows.querySelectorAll('.voucher-row').length;
    const discardFields = row => {
        row.hidden = true;
        row.querySelectorAll('input, select, textarea').forEach(field => {
            if (field.name === 'Vouchers.Index' || /\.(Id|RowVersion|Xoa)$/.test(field.name)) return;
            field.disabled = true;
        });
    };
    rows.querySelectorAll('.voucher-row').forEach(row => {
        if (row.querySelector('.delete-flag').value.toLowerCase() === 'true') discardFields(row);
    });
    const update = () => {
        const voucherMode = document.getElementById('CanVoucher').value === 'true';
        document.getElementById('automatic-config').hidden = voucherMode;
        document.getElementById('voucher-config').hidden = !voucherMode;
        rows.querySelectorAll('.voucher-row').forEach(row => {
            row.querySelector('.voucher-dishes').hidden = row.querySelector('.voucher-scope').value !== 'MonAn';
            row.querySelector('.voucher-cap').hidden = row.querySelector('.voucher-type').value !== 'PhanTram';
        });
    };
    document.getElementById('add-voucher').addEventListener('click', () => {
        const html = document.getElementById('voucher-template').innerHTML.replaceAll('__index__', String(next++));
        rows.insertAdjacentHTML('beforeend', html);
        const row = rows.lastElementChild;
        row.querySelector('.voucher-start').value = document.getElementById('BatDau').value;
        row.querySelector('.voucher-end').value = document.getElementById('KetThuc').value;
        update(); row.querySelector('input[name$=".Ma"]').focus();
    });
    rows.addEventListener('click', e => {
        const button = e.target.closest('.remove-voucher');
        if (!button) return;
        const row = button.closest('.voucher-row');
        row.querySelector('.delete-flag').value = 'true';
        // Preserve identity and binder indexes while excluding discarded controls from validation.
        discardFields(row);
    });
    form.addEventListener('change', update); update();
})();
