(() => {
    const root = document.querySelector('[data-bom-root]');
    if (!root) return;

    const baseName = root.dataset.baseName || 'DinhMucItems';
    const sizeName = root.dataset.sizeName || 'Sizes';
    const extraName = root.dataset.extraName || 'DinhMucItems';
    const sizeIdName = root.dataset.sizeIdName || 'Id';

    const sizeList = root.querySelector('[data-size-list]');
    const baseRows = root.querySelector('[data-bom-base-rows]');
    const compareHead = root.querySelector('[data-bom-compare-head]');
    const compareBody = root.querySelector('[data-bom-compare-body]');

    let ingredients = [];
    try {
        ingredients = JSON.parse(document.querySelector('[data-bom-ingredients]')?.textContent || '[]');
    } catch { ingredients = []; }

    const esc = (value) => String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const num = (value) => { const n = parseFloat(String(value ?? '').replace(',', '.')); return Number.isFinite(n) ? n : 0; };
    const fmt = (n) => String(Math.round(n * 10000) / 10000);

    function notify(message, isSuccess = true) {
        const toastEl = document.getElementById('quickToast');
        const toastMsg = document.getElementById('quickToastMessage');
        if (toastEl && toastMsg && window.bootstrap?.Toast) {
            toastMsg.textContent = message;
            toastEl.className = `toast align-items-center text-white border-0 shadow ${isSuccess ? 'bg-success' : 'bg-danger'}`;
            bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 3500 }).show();
            return;
        }
        alert(message);
    }

    function pulse(el) {
        if (!el) return;
        el.classList.add('pulse-green');
        setTimeout(() => el.classList.remove('pulse-green'), 1500);
    }

    function optionsHtml(selectedId) {
        return '<option value="">-- Chọn nguyên liệu --</option>' + ingredients.map(ing =>
            `<option value="${ing.Id}" data-dvt="${esc(ing.DonViTinh)}"${String(ing.Id) === String(selectedId) ? ' selected' : ''}>${esc(ing.TenNguyenLieu)} (${esc(ing.DonViTinh)})</option>`
        ).join('');
    }

    function makeRow(selectedId, qty) {
        const tr = document.createElement('tr');
        tr.setAttribute('data-bom-row', '');
        tr.innerHTML = `<td><select class="form-select form-select-sm select-nguyenlieu" data-field="NguyenLieuId" required>${optionsHtml(selectedId)}</select></td>
            <td><input type="number" min="0.000001" step="0.000001" class="form-control form-control-sm" data-field="SoLuong" value="${qty ?? ''}" required /></td>
            <td class="text-center"><button type="button" class="btn btn-sm btn-outline-danger btn-remove-recipe" title="Xóa nguyên liệu"><i class="bi bi-x-lg"></i></button></td>`;
        return tr;
    }

    function rowInfo(row) {
        const sel = row.querySelector('[data-field="NguyenLieuId"]');
        const opt = sel?.selectedOptions?.[0];
        const selected = !!sel?.value;
        return {
            selected,
            ten: selected ? (opt?.textContent ?? '').trim() : '',
            dvt: selected ? (opt?.dataset.dvt ?? '') : '',
            qty: num(row.querySelector('[data-field="SoLuong"]')?.value)
        };
    }

    function indexAll() {
        baseRows?.querySelectorAll('tr[data-bom-row]').forEach((row, i) => {
            row.querySelectorAll('[data-field]').forEach(el => { el.name = `${baseName}[${i}].${el.dataset.field}`; });
        });

        sizeList?.querySelectorAll('[data-size-card]').forEach((card, k) => {
            card.dataset.sizeIndex = String(k);
            card.querySelectorAll('.size-head [data-field]').forEach(el => { el.name = `${sizeName}[${k}].${el.dataset.field}`; });

            const ratio = card.querySelector('[data-bom-ratio]');
            if (ratio) {
                ratio.readOnly = k === 0;
                if (k === 0) { ratio.value = '1'; ratio.dataset.tyLeGoc = ratio.dataset.tyLeGoc || ratio.value; }
            }

            card.querySelectorAll('[data-bom-extra-rows] tr[data-bom-row]').forEach((row, j) => {
                row.querySelectorAll('[data-field]').forEach(el => { el.name = `${sizeName}[${k}].${extraName}[${j}].${el.dataset.field}`; });
            });

            const note = card.querySelector('[data-bom-extra-collapse] [data-field="GhiChuThanhPhanKhacBiet"]');
            if (note) note.name = `${sizeName}[${k}].GhiChuThanhPhanKhacBiet`;
        });
    }

    function renderCompare() {
        if (!compareHead || !compareBody) return;

        const sizes = [...(sizeList?.querySelectorAll('[data-size-card]') ?? [])].map(card => ({
            ten: (card.querySelector('[data-field="TenSize"]')?.value ?? '').trim() || 'Size',
            heSo: num(card.querySelector('[data-bom-ratio]')?.value) || 1,
            phu: [...card.querySelectorAll('[data-bom-extra-rows] tr[data-bom-row]')].map(rowInfo).filter(x => x.selected),
            ghiChu: (card.querySelector('[data-bom-extra-collapse] [data-field="GhiChuThanhPhanKhacBiet"]')?.value ?? '').trim()
        }));
        const items = [...(baseRows?.querySelectorAll('tr[data-bom-row]') ?? [])].map(rowInfo).filter(x => x.selected);

        const label = root.querySelector('[data-bom-base-label]');
        if (label && sizes.length > 0) label.textContent = sizes[0].ten;

        compareHead.innerHTML = '<th>Nguyên liệu</th><th>ĐVT</th>' +
            sizes.map(s => `<th class="text-end">${esc(s.ten)}<div class="small text-muted fw-normal">x${fmt(s.heSo)}</div></th>`).join('') +
            '<th>Nguyên liệu phụ &amp; Ghi chú</th>';

        const columns = sizes.length + 3;
        if (items.length === 0) {
            compareBody.innerHTML = `<tr><td colspan="${columns}" class="text-center text-muted small py-3">Chưa có định lượng gốc.</td></tr>`;
            return;
        }

        compareBody.innerHTML = items.map(item => {
            const cells = sizes.map(s => `<td class="text-end fw-semibold">${fmt(item.qty * s.heSo)}</td>`).join('');
            const phu = sizes.map(s => {
                const dsPhu = s.phu.map(x => `${esc(x.ten)}: ${fmt(x.qty)}`).join('; ');
                if (!dsPhu && !s.ghiChu) return '';
                return `<div class="small">• <strong>${esc(s.ten)}</strong>${dsPhu ? ' — ' + dsPhu : ''}${s.ghiChu ? ` <em>(${esc(s.ghiChu)})</em>` : ''}</div>`;
            }).join('');
            return `<tr><td>${esc(item.ten)}</td><td>${esc(item.dvt)}</td>${cells}<td>${phu || '<span class="text-muted small">—</span>'}</td></tr>`;
        }).join('');
    }

    function sizeSelects() {
        return [...(sizeList?.querySelectorAll('select[data-field="TenSize"]') ?? [])];
    }

    function addSizeOption(name) {
        sizeSelects().forEach(sel => {
            if (![...sel.options].some(o => o.value.toLowerCase() === name.toLowerCase()))
                sel.add(new Option(name, name));
        });
    }

    function firstUnusedSize(card) {
        const sel = card.querySelector('select[data-field="TenSize"]');
        if (!sel) return '';
        const used = new Set([...sizeList.querySelectorAll('[data-size-card]')]
            .filter(c => c !== card)
            .map(c => (c.querySelector('select[data-field="TenSize"]')?.value ?? '').toLowerCase())
            .filter(Boolean));
        return [...sel.options].map(o => o.value).find(v => v && !used.has(v.toLowerCase()))
            ?? sel.options[0]?.value ?? '';
    }

    function clearSizeCard(card) {
        const id = card.querySelector(`[data-field="${sizeIdName}"]`);
        if (id) id.value = '0';
        const name = card.querySelector('[data-field="TenSize"]');
        if (name) name.value = firstUnusedSize(card);
        const price = card.querySelector('[data-field="GiaBan"]');
        if (price) price.value = '0';
        const ratio = card.querySelector('[data-bom-ratio]');
        if (ratio) ratio.value = '1';
        card.querySelectorAll('[data-bom-extra-rows]').forEach(body => body.replaceChildren());
        const note = card.querySelector('[data-bom-extra-collapse] [data-field="GhiChuThanhPhanKhacBiet"]');
        if (note) note.value = '';
        card.querySelector('[data-bom-extra-collapse]')?.classList.add('d-none');
    }

    root.addEventListener('click', (e) => {
        if (e.target.closest('.btn-add-base-recipe')) {
            const row = makeRow('', '');
            baseRows.appendChild(row);
            indexAll(); renderCompare();
            row.querySelector('[data-field="NguyenLieuId"]')?.focus();
            return;
        }

        if (e.target.closest('.btn-add-extra')) {
            const card = e.target.closest('[data-size-card]');
            const body = card?.querySelector('[data-bom-extra-rows]');
            if (!body) return;
            const row = makeRow('', '');
            body.appendChild(row);
            card.querySelector('[data-bom-extra-collapse]')?.classList.remove('d-none');
            indexAll(); renderCompare();
            row.querySelector('[data-field="NguyenLieuId"]')?.focus();
            return;
        }

        if (e.target.closest('.btn-toggle-extra')) {
            const card = e.target.closest('[data-size-card]');
            card?.querySelector('[data-bom-extra-collapse]')?.classList.toggle('d-none');
            return;
        }

        if (e.target.closest('.btn-remove-recipe')) {
            e.target.closest('tr')?.remove();
            indexAll(); renderCompare();
            return;
        }

        if (e.target.closest('.btn-remove-size')) {
            const card = e.target.closest('[data-size-card]');
            if (sizeList.querySelectorAll('[data-size-card]').length <= 1) {
                notify('Món ăn cần ít nhất 1 kích cỡ. Không thể xóa kích cỡ duy nhất!', false);
                return;
            }
            if (confirm('Bạn có chắc muốn xóa kích cỡ này cùng các định lượng bên trong?')) {
                card?.remove();
                indexAll(); renderCompare();
            }
            return;
        }

        if (e.target.closest('.btn-add-size-name')) {
            const card = e.target.closest('[data-size-card]');
            const sel = card?.querySelector('select[data-field="TenSize"]');
            const raw = window.prompt('Nhập tên size mới (tối đa 50 ký tự):', '');
            if (raw === null) return;
            const name = raw.trim();
            if (!name) { notify('Tên size không được để trống.', false); return; }
            if (name.length > 50) { notify('Tên size tối đa 50 ký tự.', false); return; }
            const existing = sizeSelects().flatMap(x => [...x.options])
                .find(o => o.value.toLowerCase() === name.toLowerCase());
            if (existing) {
                if (sel) sel.value = existing.value;
                notify(`Size "${existing.value}" đã có trong danh sách.`, false);
                renderCompare();
                return;
            }
            addSizeOption(name);
            if (sel) sel.value = name;
            renderCompare();
            pulse(sel);
            return;
        }

        if (e.target.closest('#btn-add-size')) {
            const cards = sizeList.querySelectorAll('[data-size-card]');
            if (cards.length === 0) return;
            const clone = cards[cards.length - 1].cloneNode(true);
            clearSizeCard(clone);
            sizeList.appendChild(clone);
            indexAll(); renderCompare();
            pulse(clone);
            clone.querySelector('.input-size-name')?.focus();
        }
    });

    root.addEventListener('input', (e) => {
        if (e.target.closest('[data-bom-root]')) renderCompare();
    });
    root.addEventListener('change', () => renderCompare());

    root.closest('form')?.addEventListener('submit', () => { indexAll(); renderCompare(); });

    // Thức uống chỉ cần giá và kích cỡ: ẩn phần định mức khi danh mục là thức uống.
    const cboDanhMuc = document.querySelector('#cboDanhMuc') || document.querySelector('select[name="DanhMucId"]');
    function applyLoaiMon() {
        if (root.dataset.combo === 'true') return;
        const opt = cboDanhMuc?.selectedOptions?.[0];
        const laThucUong = opt ? opt.dataset.thucUong === 'true' : root.dataset.thucUong === 'true';
        root.classList.toggle('bom-thuc-uong', laThucUong);
    }
    if (cboDanhMuc) {
        cboDanhMuc.addEventListener('change', applyLoaiMon);
        applyLoaiMon();
    }

    // Cho phép module thêm nhanh nguyên liệu bổ sung lựa chọn cho các dòng tạo mới.
    window.dishBom = {
        addIngredient(ing) {
            ingredients.push(ing);
            const text = `${ing.TenNguyenLieu} (${ing.DonViTinh})`;
            root.querySelectorAll('select.select-nguyenlieu').forEach(sel => sel.add(new Option(text, ing.Id)));
        }
    };

    indexAll();
    renderCompare();
})();
