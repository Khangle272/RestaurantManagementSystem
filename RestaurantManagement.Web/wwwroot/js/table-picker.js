(() => {
    'use strict';
    // Sơ đồ bàn/phòng trống theo ngày + ca: khách hoặc lễ tân tự bấm chọn đúng bàn mình muốn.
    const form = document.querySelector('form[data-table-picker-url]');
    if (!form) return;
    const board = form.querySelector('[data-table-picker]');
    const selected = form.querySelector('input[name="BanAnId"]');
    const arrival = form.querySelector('#inputThoiGianDen');
    const guests = form.querySelector('[name="SoNguoi"]');
    const area = form.querySelector('[name="MaKhuVuc"], [name="KhuVucId"]');
    const summary = form.querySelector('[data-table-picker-summary]');
    const rate = Number(form.dataset.preorderRate || 0.5);
    const money = value => Number(value).toLocaleString('vi-VN') + ' đ';
    let tables = [], controller, sequence = 0;

    const current = () => tables.find(x => String(x.id) === selected.value);
    window.bookingDepositPreview = foodTotal => {
        if (foodTotal > 0) return money(Math.ceil(foodTotal * rate)) + ` (${Math.round(rate * 100)}% tiền món)`;
        const table = current() || tables.find(x => area?.value && String(x.khuVucId) === area.value);
        return table ? money(table.tienCoc) + ' (giữ chỗ ' + table.khuVuc + ')' : 'Chọn bàn để xem tiền đặt cọc';
    };
    const updateDeposit = () => {
        const preview = document.getElementById('booking-deposit-preview');
        const total = Number((document.getElementById('booking-cart-total')?.textContent || '0').replace(/[^\d]/g, ''));
        if (preview) preview.textContent = window.bookingDepositPreview(total);
    };

    function describe() {
        const table = current();
        summary.textContent = table
            ? `Đã chọn ${table.maBan} · ${table.khuVuc} · ${table.soCho} chỗ.`
            : 'Chưa chọn bàn. Bấm vào một bàn/phòng còn trống (màu xanh).';
        summary.classList.toggle('text-success', Boolean(table));
        updateDeposit();
    }

    function render() {
        board.replaceChildren();
        if (!tables.length) {
            board.innerHTML = '<p class="small text-muted mb-0">Không có bàn trong khu vực đã chọn.</p>';
            return;
        }
        const groups = new Map();
        tables.forEach(t => { if (!groups.has(t.khuVucId)) groups.set(t.khuVucId, []); groups.get(t.khuVucId).push(t); });
        for (const list of groups.values()) {
            const free = list.filter(x => x.trangThai === 'trong').length;
            const section = document.createElement('section');
            section.className = 'table-picker-area';
            const title = document.createElement('h3');
            title.className = 'h6 mb-2';
            title.textContent = `${list[0].khuVuc}${list[0].vip && !/vip/i.test(list[0].khuVuc) ? ' · Phòng VIP' : ''} — còn ${free}/${list.length} trống · cọc giữ chỗ ${money(list[0].tienCoc)}`;
            const grid = document.createElement('div');
            grid.className = 'table-picker-grid';
            list.forEach(t => {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = `table-picker-item is-${t.trangThai}` + (String(t.id) === selected.value ? ' is-selected' : '');
                button.disabled = t.trangThai !== 'trong';
                button.setAttribute('aria-pressed', String(String(t.id) === selected.value));
                button.innerHTML = `<strong></strong><span></span><small></small>`;
                button.querySelector('strong').textContent = t.maBan;
                button.querySelector('span').textContent = `${t.soCho} chỗ`;
                button.querySelector('small').textContent = t.ghiChu;
                button.title = `${t.maBan} · ${t.ghiChu}`;
                button.addEventListener('click', () => {
                    selected.value = t.id;
                    if (area && area.value !== String(t.khuVucId) && [...area.options].some(o => o.value === String(t.khuVucId))) area.value = String(t.khuVucId);
                    render(); describe();
                });
                grid.append(button);
            });
            section.append(title, grid);
            board.append(section);
        }
    }

    async function load() {
        const run = ++sequence;
        controller?.abort();
        if (!arrival.value || !guests.value) { board.innerHTML = '<p class="small text-muted mb-0">Chọn ngày, giờ đến và số khách để xem bàn trống.</p>'; return; }
        controller = new AbortController();
        board.setAttribute('aria-busy', 'true');
        try {
            const url = new URL(form.dataset.tablePickerUrl, location.origin);
            url.searchParams.set('gioDen', arrival.value);
            url.searchParams.set('soNguoi', guests.value);
            if (area?.value) url.searchParams.set('maKhuVuc', area.value);
            const response = await fetch(url, { cache: 'no-store', signal: controller.signal });
            const data = await response.json();
            if (run !== sequence) return;
            if (!response.ok) throw new Error(data.message || 'Không tải được sơ đồ bàn.');
            tables = data.tables;
            if (selected.value && !tables.some(x => String(x.id) === selected.value && x.trangThai === 'trong')) selected.value = '';
            render(); describe();
        } catch (error) {
            if (error.name !== 'AbortError' && run === sequence) { tables = []; board.innerHTML = ''; summary.textContent = error.message; }
        } finally { board.removeAttribute('aria-busy'); }
    }

    form.addEventListener('slots:refreshed', load);
    area?.addEventListener('change', () => { selected.value = ''; });
    document.addEventListener('cart:changed', updateDeposit);
    form.addEventListener('ajax:before', e => {
        if (!selected.value) { e.preventDefault(); summary.textContent = 'Vui lòng chọn bàn/phòng trên sơ đồ trước khi gửi.'; summary.classList.add('text-danger'); board.scrollIntoView({ behavior: 'smooth', block: 'center' }); }
    });
    form.addEventListener('submit', e => {
        if (!selected.value && form.dataset.ajaxForm !== 'true') { e.preventDefault(); summary.textContent = 'Vui lòng chọn bàn/phòng trên sơ đồ trước khi gửi.'; }
    });
    load();
})();
