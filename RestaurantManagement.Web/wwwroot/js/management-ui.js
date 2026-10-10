(() => {
    'use strict';
    const toggle = document.getElementById('sidebar-toggle');
    if (toggle) {
        const setSidebar = collapsed => {
            document.body.classList.toggle('sidebar-collapsed', collapsed);
            toggle.setAttribute('aria-expanded', String(!collapsed));
            toggle.setAttribute('aria-label', collapsed ? 'Mở menu chức năng' : 'Ẩn menu chức năng');
        };
        try { setSidebar(localStorage.getItem('management-sidebar-collapsed') === 'true'); } catch { /* Browser storage may be disabled. */ }
        toggle.addEventListener('click', () => {
            const collapsed = !document.body.classList.contains('sidebar-collapsed');
            setSidebar(collapsed);
            try { localStorage.setItem('management-sidebar-collapsed', String(collapsed)); } catch { /* Keep the current page usable. */ }
        });
    }

    const form = document.querySelector('form[data-pos-form]');
    if (!form) return;
    const byId = id => form.querySelector(`#${id}`);
    const isCreate = form.dataset.posForm === 'create';
    const tableId = byId('selected-table-id');
    const bookingId = byId('selected-booking-id');
    const selectedType = () => form.querySelector('input[name="LoaiDonHang"]:checked')?.value || 'TaiBan';
    const tableButtons = Array.from(form.querySelectorAll('[data-table-id]'));
    const menuButtons = Array.from(form.querySelectorAll('.btn-add-dish'));
    const menu = new Map(menuButtons.map(button => [Number(button.dataset.sizeId), {
        dishId: Number(button.dataset.dishId), sizeId: Number(button.dataset.sizeId),
        dishName: button.dataset.dishName, sizeName: button.dataset.sizeName, price: Number(button.dataset.price)
    }]));
    const storagePrefix = `pos-draft:${form.dataset.draftUser || 'session'}:`;
    const carts = new Map();
    const orderNotes = new Map();
    let cart = [];
    let scope = '';
    let sentDraft = null;
    const currency = value => `${value.toLocaleString('vi-VN')} đ`;
    const getScope = () => isCreate
        ? selectedType() === 'TaiBan' ? `table:${tableId.value || 'none'}:booking:${bookingId.value || 'none'}` : `mode:${selectedType()}`
        : `invoice:${form.dataset.invoiceId}`;
    function loadDraft(key) {
        try {
            const saved = JSON.parse(sessionStorage.getItem(storagePrefix + key) || '[]');
            if (!Array.isArray(saved)) return [];
            return saved.filter(item => menu.has(Number(item.sizeId)) && Number.isInteger(item.qty) && item.qty > 0 && item.qty <= 999)
                .map(item => ({ ...menu.get(Number(item.sizeId)), qty: item.qty, note: '' }));
        } catch { return []; }
    }
    function saveDraft(key = scope, items = cart) {
        carts.set(key, items);
        try {
            // Persist only menu identifiers and counts; customer fields and free-text notes stay in memory.
            if (items.length) sessionStorage.setItem(storagePrefix + key, JSON.stringify(items.map(item => ({ sizeId: item.sizeId, qty: item.qty }))));
            else sessionStorage.removeItem(storagePrefix + key);
        } catch { /* In-memory drafts still work when storage is unavailable. */ }
    }
    function changeScope() {
        if (scope) {
            saveDraft();
            if (isCreate) orderNotes.set(scope, form.elements.GhiChuDonHang.value);
        }
        scope = getScope();
        cart = carts.get(scope) || loadDraft(scope);
        if (isCreate) form.elements.GhiChuDonHang.value = orderNotes.get(scope) || '';
        renderCart();
    }
    const node = (tag, className, text) => {
        const result = document.createElement(tag);
        if (className) result.className = className;
        if (text !== undefined) result.textContent = text;
        return result;
    };
    function actionButton(text, label, onClick, className = 'btn btn-sm btn-outline-secondary') {
        const button = node('button', className, text);
        button.type = 'button';
        button.setAttribute('aria-label', label);
        button.addEventListener('click', onClick);
        return button;
    }
    function renderCart() {
        const list = byId('cart-items-list');
        list.replaceChildren();
        byId('empty-cart-message').hidden = cart.length > 0;
        let quantity = 0;
        let total = 0;
        cart.forEach((item, index) => {
            quantity += item.qty;
            total += item.qty * item.price;
            const card = node('div', 'card border p-2 bg-white shadow-none');
            const header = node('div', 'd-flex justify-content-between gap-2');
            const title = node('div', 'pos-item-title');
            title.append(node('div', 'fw-bold small text-dark', item.dishName), node('span', 'badge bg-secondary-subtle text-secondary me-1', item.sizeName), node('span', 'small text-muted', currency(item.price)));
            header.append(title, actionButton('×', `Bỏ ${item.dishName}`, () => { cart = cart.filter(candidate => candidate !== item); renderCart(); }, 'btn btn-sm btn-link text-danger p-0 align-self-start'));
            const row = node('div', 'd-flex justify-content-between align-items-center my-2');
            const quantities = node('div', 'btn-group btn-group-sm');
            quantities.setAttribute('role', 'group');
            quantities.setAttribute('aria-label', `Số lượng ${item.dishName}`);
            const changeQuantity = delta => {
                item.qty = Math.min(999, item.qty + delta);
                if (item.qty <= 0) cart = cart.filter(candidate => candidate !== item);
                renderCart();
            };
            quantities.append(actionButton('−', 'Giảm số lượng', () => changeQuantity(-1)), node('span', 'btn btn-outline-secondary disabled fw-bold', String(item.qty)), actionButton('+', 'Tăng số lượng', () => changeQuantity(1)));
            row.append(quantities, node('strong', 'small', currency(item.qty * item.price)));
            const note = node('input', 'form-control form-control-sm');
            note.type = 'text';
            note.name = `Items[${index}].YeuCauCheBien`;
            note.maxLength = 500;
            note.value = item.note || '';
            note.placeholder = 'Ghi chú chế biến: ít cay, không hành...';
            note.setAttribute('aria-label', `Ghi chú chế biến ${item.dishName}`);
            note.addEventListener('input', () => { item.note = note.value; saveDraft(); });
            card.append(header, row, note);
            for (const [name, value] of Object.entries({ MonAnId: item.dishId, MonAnSizeId: item.sizeId, SoLuong: item.qty })) {
                const hidden = node('input');
                hidden.type = 'hidden';
                hidden.name = `Items[${index}].${name}`;
                hidden.value = String(value);
                card.append(hidden);
            }
            list.append(card);
        });
        byId('cart-item-count').textContent = String(cart.length);
        byId('cart-total-qty').textContent = `${quantity} phần`;
        byId('cart-total-amount').textContent = currency(total);
        byId('btn-submit-order').disabled = !cart.length || form.dataset.ajaxPending === 'true';
        saveDraft();
    }
    function collapseConfiguration(collapse) {
        byId('pos-order-config').hidden = collapse;
        byId('pos-current-target').hidden = !collapse;
        form.classList.toggle('pos-config-collapsed', collapse);
        byId('pos-change-target').setAttribute('aria-expanded', String(!collapse));
    }
    function updateTarget() {
        if (!isCreate) return;
        const type = selectedType();
        const selected = tableButtons.find(button => button.dataset.tableId === tableId.value);
        const target = type === 'TaiBan' ? selected ? `Tại bàn ${selected.dataset.tableCode}` : 'Chưa chọn bàn'
            : type === 'MangDi' ? 'Mang đi' : 'Giao hàng';
        byId('order-target-display').querySelector('span').textContent = target;
        byId('pos-current-table').textContent = target;
        byId('section-dinein').classList.toggle('d-none', type !== 'TaiBan');
        byId('section-external').classList.toggle('d-none', type === 'TaiBan');
        byId('delivery-address-group').classList.toggle('d-none', type !== 'GiaoHang');
        ['TenNguoiNhan', 'SoDienThoaiNhan'].forEach(name => { form.elements[name].required = type !== 'TaiBan'; });
        form.elements.DiaChiGiaoHang.required = type === 'GiaoHang';
        tableButtons.forEach(button => {
            const active = button === selected && type === 'TaiBan';
            button.classList.toggle('border-primary', active);
            button.classList.toggle('bg-primary-subtle', active);
            button.classList.toggle('text-primary', active);
            button.setAttribute('aria-pressed', String(active));
        });
        collapseConfiguration(type === 'TaiBan' && !!selected);
    }
    if (isCreate) {
        const selected = tableButtons.find(button => button.dataset.tableId === tableId.value);
        if (selected) bookingId.value = selected.dataset.bookingId || '';
        else { tableId.value = ''; bookingId.value = ''; }
        tableButtons.forEach(button => button.addEventListener('click', () => {
            tableId.value = button.dataset.tableId;
            bookingId.value = button.dataset.bookingId || '';
            byId('table-validation-error').classList.add('d-none');
            changeScope();
            updateTarget();
        }));
        form.querySelectorAll('input[name="LoaiDonHang"]').forEach(radio => radio.addEventListener('change', () => { changeScope(); updateTarget(); }));
        byId('pos-change-target').addEventListener('click', () => {
            collapseConfiguration(false);
            tableButtons.find(button => button.dataset.tableId === tableId.value)?.focus();
        });
        updateTarget();
    }
    menuButtons.forEach(button => button.addEventListener('click', () => {
        if (isCreate && selectedType() === 'TaiBan' && !tableId.value) {
            byId('table-validation-error').classList.remove('d-none');
            byId('pos-order-config').scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            return;
        }
        const item = menu.get(Number(button.dataset.sizeId));
        const current = cart.find(candidate => candidate.sizeId === item.sizeId);
        if (current) current.qty = Math.min(999, current.qty + 1);
        else cart.push({ ...item, qty: 1, note: '' });
        renderCart();
    }));
    byId('pos-clear-cart').addEventListener('click', () => { cart = []; renderCart(); });
    function filterMenu() {
        const category = byId('dish-category').value;
        const search = byId('dish-search').value.toLocaleLowerCase('vi-VN').trim();
        let count = 0;
        form.querySelectorAll('.dish-card-item').forEach(item => {
            item.hidden = !(category === 'all' || item.dataset.category === category) || !item.dataset.name.includes(search);
            if (!item.hidden) count++;
        });
        byId('pos-no-results').hidden = count > 0 || !menu.size;
    }
    byId('dish-category').addEventListener('change', filterMenu);
    byId('dish-search').addEventListener('input', filterMenu);
    form.addEventListener('ajax:before', e => {
        if (!cart.length || (isCreate && selectedType() === 'TaiBan' && !tableId.value)) {
            e.preventDefault();
            if (isCreate) byId('table-validation-error').classList.remove('d-none');
            return;
        }
        sentDraft = { scope, items: cart.map(item => ({ ...item })) };
    });
    form.addEventListener('ajax:success', e => {
        if (!sentDraft) return;
        const draft = carts.get(sentDraft.scope) || [];
        const remaining = draft.map(item => ({ ...item, qty: item.qty - (sentDraft.items.find(sent => sent.sizeId === item.sizeId)?.qty || 0) })).filter(item => item.qty > 0);
        saveDraft(sentDraft.scope, remaining);
        if (scope === sentDraft.scope) { cart = remaining; renderCart(); }
        if (remaining.length || (scope !== sentDraft.scope && cart.length)) {
            e.preventDefault();
            let notice = form.querySelector('[data-ajax-feedback]');
            if (!notice) { notice = node('div'); notice.dataset.ajaxFeedback = ''; form.prepend(notice); }
            notice.className = 'alert alert-success my-2';
            notice.setAttribute('role', 'status');
            notice.textContent = 'Đã gửi món. Những món vừa chọn thêm vẫn được giữ để gửi tiếp.';
            notice.hidden = false;
            if (e.detail.redirectUrl) {
                const link = node('a', 'alert-link ms-2', 'Xem đơn đã gửi');
                link.href = e.detail.redirectUrl;
                notice.append(link);
            }
        }
        sentDraft = null;
    });
    form.addEventListener('ajax:complete', () => { byId('btn-submit-order').disabled = !cart.length; });
    if (isCreate) orderNotes.set(getScope(), form.elements.GhiChuDonHang.value);
    changeScope();
    try {
        const initial = JSON.parse(byId('pos-initial-items').textContent);
        if (Array.isArray(initial) && initial.length) {
            cart = initial.filter(item => menu.has(item.MonAnSizeId) && item.SoLuong > 0)
                .map(item => ({ ...menu.get(item.MonAnSizeId), qty: Math.min(999, item.SoLuong), note: item.YeuCauCheBien || '' }));
            renderCart();
        }
    } catch { /* Leave the live draft available when no initial items were returned. */ }
})();
