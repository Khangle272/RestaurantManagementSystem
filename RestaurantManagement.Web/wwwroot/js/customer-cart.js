(() => {
    'use strict';
    const root = document.getElementById('customer-cart');
    if (!root) return;
    const byId = id => document.getElementById(id);
    const money = value => new Intl.NumberFormat('vi-VN').format(value) + ' ₫';
    const statuses = { ChoCheBien: 'Chờ chế biến', DangCheBien: 'Đang chế biến', SanSang: 'Sẵn sàng', DaPhucVu: 'Đã phục vụ', DaHuy: 'Đã hủy', 0: 'Chờ chế biến', 1: 'Đang chế biến', 2: 'Sẵn sàng', 3: 'Đã phục vụ', 4: 'Đã hủy' };
    const storage = { get(key) { try { return JSON.parse(localStorage.getItem(key)); } catch { return null; } }, set(key, value) { try { localStorage.setItem(key, JSON.stringify(value)); } catch { /* Draft remains available for this page. */ } }, remove(key) { try { localStorage.removeItem(key); } catch { /* Storage may be disabled. */ } } };
    const owner = root.dataset.owner;
    const contextKey = 'customer-cart-context:' + owner;
    const params = new URLSearchParams(location.search);
    let bookingId = Number(params.get('datBanId'));
    if (!Number.isSafeInteger(bookingId) || bookingId < 1) bookingId = 0;
    if (!params.has('datBanId') && params.get('cart') !== 'new') {
        try { bookingId = Number(sessionStorage.getItem(contextKey)) || 0; } catch { /* New booking is the default. */ }
    }
    const key = () => 'customer-cart:v1:' + owner + ':' + (bookingId || 'new');
    const newRequestId = () => crypto.randomUUID();
    const cleanItems = value => (Array.isArray(value) ? value : []).slice(0, 50).filter(x => x && Number.isSafeInteger(x.monAnId) && x.monAnId > 0 && Number.isSafeInteger(x.sizeId) && x.sizeId > 0 && Number.isInteger(x.quantity) && x.quantity > 0 && x.quantity <= 99).map(x => ({ monAnId: x.monAnId, sizeId: x.sizeId, quantity: x.quantity, note: String(x.note || '').slice(0, 500) }));
    let draft = storage.get(key());
    if (!cleanItems(draft?.items).length && !bookingId && owner !== 'guest') {
        const guestDraft = storage.get('customer-cart:v1:guest:new');
        if (cleanItems(guestDraft?.items).length) {
            draft = guestDraft;
            storage.remove('customer-cart:v1:guest:new');
        }
    }
    let items = cleanItems(draft?.items), sent = [], version = '', canEdit = !bookingId;
    let requestId = typeof draft?.requestId === 'string' && /^[a-f0-9-]{36}$/i.test(draft.requestId) ? draft.requestId : newRequestId();
    let prepare = Boolean(draft?.prepare), loading = true, selectedDish = null, conflict = false, submitting = false;
    const dishes = new Map();
    const initial = byId('customer-menu');
    if (initial) { try { JSON.parse(initial.textContent).forEach(x => dishes.set(x.id, x)); } catch { /* Fetch selected dishes below. */ } }

    function persist(changed = false) {
        if (changed) requestId = newRequestId();
        storage.set(key(), { items: cleanItems(items), version, requestId, prepare });
        try { sessionStorage.setItem(contextKey, String(bookingId)); } catch { /* Context remains on this page. */ }
        sync();
    }
    function error(message) {
        byId('cart-errors').textContent = message;
        byId('cart-errors').hidden = !message;
    }
    async function loadDish(id) {
        const response = await fetch(root.dataset.dishUrl + '/' + id, { headers: { Accept: 'application/json' }, cache: 'no-store' });
        if (!response.ok) throw new Error('Món không còn nằm trong thực đơn hiện tại.');
        const dish = await response.json();
        dishes.set(id, dish);
        return dish;
    }
    function details(item) {
        const dish = dishes.get(item.monAnId);
        const size = dish?.sizes?.find(x => x.id === item.sizeId);
        return { name: dish?.name || 'Món #' + item.monAnId, sizeName: size?.name || 'Size không còn phục vụ', price: Number(size?.price) || 0, image: dish?.image, available: Boolean(dish?.canOrder && size) };
    }
    function input(container, name, value) {
        const field = document.createElement('input');
        field.type = 'hidden'; field.name = name; field.value = String(value);
        container.append(field);
    }
    function sync() {
        const total = items.reduce((sum, item) => sum + details(item).price * item.quantity, 0) + sent.reduce((sum, item) => sum + Number(item.price) * Number(item.quantity), 0);
        byId('cart-count').textContent = items.reduce((sum, item) => sum + item.quantity, 0) + sent.reduce((sum, item) => sum + Number(item.quantity), 0);
        byId('cart-total').textContent = money(total);
        if (byId('booking-deposit-preview')) byId('booking-deposit-preview').textContent = loading ? 'Đang tính…' : money(total > 0 ? Math.ceil(total * .5) : 299000);
        byId('cart-version').value = version;
        byId('cart-request-id').value = requestId;
        document.querySelectorAll('[data-cart-inputs]').forEach(container => {
            container.replaceChildren();
            items.forEach((item, index) => {
                input(container, `Items[${index}].MonAnSizeId`, item.sizeId);
                input(container, `Items[${index}].SoLuong`, item.quantity);
                input(container, `Items[${index}].YeuCauCheBien`, item.note);
            });
        });
        const bookingForm = byId('formDatBan');
        if (bookingForm) {
            const field = bookingForm.querySelector('[name="RequestId"]');
            if (field) field.value = requestId;
            const summary = byId('booking-cart-summary');
            if (summary) {
                summary.replaceChildren();
                if (!items.length) summary.textContent = 'Bạn có thể đặt bàn trước và bổ sung món sau.';
                items.forEach(item => { const line = document.createElement('li'); const info = details(item); line.textContent = `${item.quantity} × ${info.name} (${info.sizeName})${item.note ? ' — ' + item.note : ''}${!loading && !info.available ? ' — Không còn phục vụ, vui lòng chọn lại' : ''}`; if (!loading && !info.available) line.className = 'text-danger'; summary.append(line); });
            }
            if (byId('booking-cart-total')) byId('booking-cart-total').textContent = money(total);
            if (!submitting) bookingForm.querySelector('[type="submit"]').disabled = loading || Boolean(bookingId) || items.some(item => !details(item).available);
        }
        if (!submitting) byId('cart-save-button').disabled = loading || conflict || !canEdit || items.some(item => !details(item).available);
        byId('cart-prepare').disabled = !canEdit || loading;
        if (byId('booking-prepare')) byId('booking-prepare').checked = prepare;
    }
    function renderRow(item, index, readonly) {
        const info = readonly ? item : details(item);
        const row = document.createElement('article');
        row.className = 'cart-item' + (readonly ? ' cart-item-readonly' : '');
        const image = document.createElement('img');
        if (info.image && /^(\/[^/]|https?:\/\/)/i.test(info.image)) { image.src = info.image; image.alt = info.name; }
        else row.classList.add('cart-item-no-image');
        if (!row.classList.contains('cart-item-no-image')) row.append(image);
        const body = document.createElement('div'), title = document.createElement('h3'), subtitle = document.createElement('div');
        title.textContent = info.name; subtitle.className = 'small text-muted';
        subtitle.textContent = `${info.sizeName || ''} · ${money(Number(info.price) || 0)}`;
        body.append(title, subtitle);
        if (readonly) {
            const status = document.createElement('p'); status.className = 'small mb-0';
            status.textContent = `${item.quantity} phần · Đã gửi bếp${item.status !== undefined && item.status !== null ? ' · ' + (statuses[item.status] || item.status) : ''}${item.note ? ' · ' + item.note : ''}`;
            body.append(status);
        } else {
            const controls = document.createElement('div'); controls.className = 'cart-controls';
            const qty = document.createElement('input'); qty.type = 'number'; qty.min = '1'; qty.max = '99'; qty.value = item.quantity; qty.className = 'form-control form-control-sm'; qty.setAttribute('aria-label', 'Số lượng ' + info.name); qty.disabled = !canEdit || loading || submitting;
            qty.addEventListener('change', () => { item.quantity = Math.max(1, Math.min(99, Math.trunc(Number(qty.value) || 1))); qty.value = item.quantity; persist(true); });
            const remove = document.createElement('button'); remove.type = 'button'; remove.className = 'btn btn-sm btn-outline-secondary'; remove.textContent = 'Bỏ món'; remove.disabled = !canEdit || loading || submitting;
            remove.addEventListener('click', () => { items.splice(index, 1); persist(true); render(); });
            controls.append(qty, remove); body.append(controls);
            const note = document.createElement('textarea'); note.className = 'form-control form-control-sm'; note.rows = 2; note.maxLength = 500; note.value = item.note; note.placeholder = 'Ghi chú chế biến'; note.setAttribute('aria-label', 'Ghi chú chế biến ' + info.name); note.disabled = !canEdit || loading || submitting;
            note.addEventListener('input', () => { item.note = note.value; persist(true); }); body.append(note);
            if (!info.available) { const warning = document.createElement('p'); warning.className = 'small text-danger mt-2 mb-0'; warning.textContent = 'Món / size hiện không phục vụ. Vui lòng bỏ hoặc chọn lại.'; body.append(warning); }
        }
        row.append(body); return row;
    }
    function render() {
        const list = byId('cart-items'); list.replaceChildren();
        if (!items.length && !sent.length) list.textContent = 'Chưa có món trong giỏ.';
        sent.forEach(item => list.append(renderRow(item, 0, true)));
        items.forEach((item, index) => list.append(renderRow(item, index, false)));
        sync();
    }
    function keepContextLinks() {
        if (!bookingId) return;
        document.querySelectorAll('a[href]').forEach(link => {
            const url = new URL(link.href, location.origin);
            if (url.origin === location.origin && (url.pathname === '/' || /^\/Home(?:\/|$)/i.test(url.pathname)) && !url.searchParams.has('datBanId') && url.searchParams.get('cart') !== 'new') { url.searchParams.set('datBanId', bookingId); link.href = url.href; }
        });
    }
    async function initialize() {
        try {
            if (bookingId) {
                const response = await fetch(root.dataset.contextUrl + '?id=' + bookingId, { headers: { Accept: 'application/json' }, cache: 'no-store' });
                if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error('Không thể mở giỏ của lịch này. Hãy đăng nhập đúng tài khoản và xem lại lịch đặt bàn.');
                const context = await response.json();
                canEdit = Boolean(context.canEdit);
                version = draft?.version || context.version || '';
                sent = (context.items || []).filter(x => x.sent);
                if (!draft) items = cleanItems((context.items || []).filter(x => !x.sent));
                prepare = draft ? prepare : Boolean(context.chuanBiTruoc ?? context.requestedEarly);
                const label = byId('cart-context'); label.hidden = false;
                label.textContent = `Đang chọn cho lịch ${context.code || '#' + bookingId}${context.arrival ? ' · ' + new Date(context.arrival).toLocaleString('vi-VN') : ''}. `;
                const detailLink = document.createElement('a'); detailLink.href = root.dataset.detailUrl + '/' + bookingId; detailLink.textContent = 'Xem chi tiết'; label.append(detailLink);
                byId('cart-description').textContent = canEdit ? 'Bổ sung hoặc sửa món chưa gửi bếp cho lịch này. Món đã gửi bếp được giữ để theo dõi.' : 'Lịch này không còn nhận thay đổi món. Bạn có thể xem chi tiết bên dưới.';
                byId('cart-new-checkout').hidden = true;
                byId('cart-save-form').hidden = false;
                byId('cart-save-form').action = root.dataset.saveUrl + '/' + bookingId;
                byId('cart-details').href = root.dataset.detailUrl + '/' + bookingId;
                byId('cart-switch-new').hidden = false;
                conflict = Boolean(draft?.version && draft.version !== context.version);
                if (conflict) { byId('cart-reload').hidden = false; error('Lịch đã được cập nhật. Giỏ nháp được giữ. Hãy xem chi tiết rồi tải lại món chưa gửi bếp để tránh gửi trùng món.'); }
                if (!canEdit) error('Lịch này không còn nhận thay đổi món.');
            }
            await Promise.all([...new Set(items.map(x => x.monAnId))].map(async id => { try { await loadDish(id); } catch { dishes.delete(id); } }));
        } catch (err) { canEdit = false; error(err.message); }
        loading = false; byId('cart-prepare').checked = prepare; persist(); render(); keepContextLinks();
    }
    document.querySelectorAll('[data-cart-dish]').forEach(button => button.addEventListener('click', async () => {
        if (loading || submitting || !canEdit) { error('Vui lòng đợi giỏ tải / gửi xong hoặc chọn món cho lịch mới.'); bootstrap.Offcanvas.getOrCreateInstance(byId('cartDrawer')).show(); return; }
        selectedDish = null; byId('dish-title').textContent = 'Đang tải món…'; byId('dish-description').textContent = ''; byId('dish-extras').replaceChildren(); byId('dish-size').replaceChildren(); byId('dish-image').hidden = true; byId('dish-error').hidden = true; byId('dish-add').disabled = true; byId('dish-quantity').value = 1; byId('dish-note').value = '';
        bootstrap.Modal.getOrCreateInstance(byId('dishModal')).show();
        try {
            selectedDish = await loadDish(Number(button.dataset.cartDish));
            byId('dish-title').textContent = selectedDish.name; byId('dish-description').textContent = selectedDish.description || '';
            if (selectedDish.image && /^(\/[^/]|https?:\/\/)/i.test(selectedDish.image)) { byId('dish-image').src = selectedDish.image; byId('dish-image').alt = selectedDish.name; byId('dish-image').hidden = false; }
            selectedDish.sizes.forEach(size => { const option = document.createElement('option'); option.value = size.id; option.textContent = size.name + ' — ' + money(size.price); byId('dish-size').append(option); });
            [...(selectedDish.comboItems || []), ...(selectedDish.promotions || []).map(x => x.name + ': ' + x.summary)].forEach(text => { const line = document.createElement('div'); line.textContent = text; byId('dish-extras').append(line); });
            byId('dish-add').disabled = !selectedDish.canOrder || !selectedDish.sizes.length;
            if (!selectedDish.canOrder) { byId('dish-error').textContent = 'Món hiện tạm hết, vui lòng chọn món khác.'; byId('dish-error').hidden = false; }
        } catch (err) { byId('dish-title').textContent = 'Không thể chọn món'; byId('dish-error').textContent = err.message; byId('dish-error').hidden = false; }
    }));
    byId('cart-dish-form').addEventListener('submit', event => {
        event.preventDefault();
        if (!selectedDish?.canOrder || !canEdit || submitting) return;
        const sizeId = Number(byId('dish-size').value), quantity = Number(byId('dish-quantity').value), note = byId('dish-note').value.trim();
        if (!selectedDish.sizes.some(x => x.id === sizeId) || !Number.isInteger(quantity) || quantity < 1 || quantity > 99) return;
        const existing = items.find(x => x.sizeId === sizeId && x.note === note);
        if (existing && existing.quantity + quantity > 99) { byId('dish-error').textContent = 'Mỗi dòng món tối đa 99 phần. Vui lòng điều chỉnh số lượng trong giỏ.'; byId('dish-error').hidden = false; return; }
        if (existing) existing.quantity += quantity;
        else if (items.length < 50) items.push({ monAnId: selectedDish.id, sizeId, quantity, note });
        else { byId('dish-error').textContent = 'Giỏ tối đa 50 dòng món. Vui lòng điều chỉnh số lượng trong giỏ.'; byId('dish-error').hidden = false; return; }
        persist(true); render(); bootstrap.Modal.getOrCreateInstance(byId('dishModal')).hide();
        const feedback = byId('cart-feedback');
        feedback.textContent = `Đã thêm ${quantity} phần ${selectedDish.name} vào giỏ`;
        feedback.classList.add('show');
        clearTimeout(feedback.hideTimer);
        feedback.hideTimer = setTimeout(() => feedback.classList.remove('show'), 2500);
    });
    [byId('cart-prepare'), byId('booking-prepare')].filter(Boolean).forEach(checkbox => checkbox.addEventListener('change', () => { if (submitting) { checkbox.checked = prepare; return; } prepare = checkbox.checked; byId('cart-prepare').checked = prepare; persist(true); }));
    byId('cart-switch-new').addEventListener('click', () => { if (submitting) return; try { sessionStorage.setItem(contextKey, '0'); } catch { /* Query selects the new context. */ } location.href = '/Home?cart=new#thuc-don'; });
    byId('cart-reload').addEventListener('click', async () => {
        if (submitting || loading) return;
        if (!confirm('Thay giỏ nháp bằng danh sách món chưa gửi bếp của lịch hiện tại?')) return;
        draft = null; items = []; sent = []; version = ''; loading = true; conflict = false;
        byId('cart-reload').hidden = true; error(''); requestId = newRequestId();
        await initialize();
    });
    [byId('cart-save-form'), byId('formDatBan')].filter(Boolean).forEach(form => {
        if (form.id === 'formDatBan') form.addEventListener('input', event => { if (!submitting && event.target?.type !== 'hidden' && event.target?.id !== 'booking-prepare') persist(true); });
        form.addEventListener('submit', event => {
            if (loading || submitting || conflict || !canEdit || (form.id === 'formDatBan' && bookingId) || items.some(item => !details(item).available)) { event.preventDefault(); event.stopImmediatePropagation(); error('Giỏ chưa sẵn sàng gửi. Vui lòng kiểm tra món và lịch đặt bàn.'); return; }
            sync(); persist();
        }, true);
        form.addEventListener('ajax:before', () => { submitting = true; render(); });
        form.addEventListener('ajax:success', () => { storage.remove(key()); if (!bookingId) { try { sessionStorage.removeItem(contextKey); } catch { /* New context remains harmless. */ } } });
        form.addEventListener('ajax:error', event => { submitting = false; if (event.detail?.response?.status === 409) { conflict = true; byId('cart-reload').hidden = false; } const messages = event.detail?.errors; error(Array.isArray(messages) ? messages.join(' ') : event.detail?.message || 'Không thể gửi yêu cầu. Giỏ nháp đã được giữ để bạn thử lại.'); render(); });
        form.addEventListener('ajax:complete', () => { submitting = false; render(); });
    });
    render(); initialize();
})();
