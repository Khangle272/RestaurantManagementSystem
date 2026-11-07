(() => {
    'use strict';

    // Thông báo thành công nằm trong trang (không phải thông báo nổi) tự ẩn sau 5 giây; lỗi giữ tới khi đóng.
    // Chống bấm gửi hai lần và thông báo nổi được xử lý chung trong ux.js.
    document.querySelectorAll('.admin-main .alert-success.alert-dismissible').forEach(alert => {
        if (alert.closest('.flash-stack')) return;
        setTimeout(() => {
            if (!document.body.contains(alert)) return;
            if (window.bootstrap?.Alert) window.bootstrap.Alert.getOrCreateInstance(alert).close();
            else alert.remove();
        }, 5000);
    });

    // Press "/" to jump to the first search box of the page.
    document.addEventListener('keydown', event => {
        if (event.key !== '/' || event.ctrlKey || event.metaKey || event.altKey) return;
        const target = event.target;
        if (target instanceof HTMLElement && (target.isContentEditable || /^(input|textarea|select)$/i.test(target.tagName))) return;
        const search = document.querySelector('.admin-main input[name="search"], .admin-main input[type="search"]');
        if (!search) return;
        event.preventDefault();
        search.focus();
        search.select?.();
    });

    // Gợi ý phím tắt "/" ngay trong ô tìm kiếm.
    const search = document.querySelector('.admin-main input[name="search"], .admin-main input[type="search"]');
    if (search && search.placeholder && !search.placeholder.includes('(/)') && window.matchMedia('(hover: hover)').matches) {
        search.placeholder = `${search.placeholder} (/)`;
        search.title ||= 'Nhấn phím / để tìm nhanh';
    }

    // Esc trong ô tìm kiếm có nội dung: xóa nhanh từ khóa.
    document.addEventListener('keydown', event => {
        const input = event.target;
        if (event.key !== 'Escape' || !(input instanceof HTMLInputElement) || input.name !== 'search' || !input.value) return;
        input.value = '';
        event.preventDefault();
    });
})();

// Bộ lọc dạng GET: đổi lựa chọn trong danh sách thả xuống là áp dụng luôn, ô chữ và ngày vẫn bấm nút.
(() => {
    'use strict';

    const filterForms = Array.from(document.querySelectorAll('.admin-main form')).filter(form => form.method.toLowerCase() === 'get');
    const controlsOf = form => Array.from(form.elements).filter(field =>
        /^(input|select|textarea)$/i.test(field.tagName) && !/^(hidden|submit|button|reset)$/i.test(field.type));

    document.addEventListener('change', event => {
        const select = event.target;
        if (!(select instanceof HTMLSelectElement) || select.hasAttribute('onchange')) return;
        if (filterForms.includes(select.form)) select.form.requestSubmit();
    });

    // Điện thoại: bộ lọc từ 3 ô trở lên thu vào một nút để nội dung chính hiện ngay đầu trang.
    const query = new URLSearchParams(location.search);
    filterForms.filter(form => controlsOf(form).length >= 3).forEach((form, index) => {
        const panel = form.parentElement?.matches('.card-body') ? form.closest('.card') : form;
        const active = controlsOf(form).filter(field => field.name && query.get(field.name)).length;
        const toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.className = 'btn btn-outline-secondary filter-toggle';
        toggle.setAttribute('aria-expanded', 'false');
        panel.id ||= `filter-panel-${index}`;
        toggle.setAttribute('aria-controls', panel.id);
        toggle.innerHTML = '<i class="bi bi-funnel" aria-hidden="true"></i> Bộ lọc';
        if (active) {
            const count = document.createElement('span');
            count.className = 'filter-toggle-count';
            count.textContent = active;
            toggle.append(count);
        }
        panel.classList.add('filter-collapsible');
        panel.before(toggle);
        toggle.addEventListener('click', () => {
            const open = panel.classList.toggle('is-open');
            toggle.setAttribute('aria-expanded', String(open));
        });
    });

    // Rê chuột vào nội dung bị cắt ngắn để đọc đủ.
    document.querySelectorAll('.admin-main .table-note, .admin-main .cell-note').forEach(note => {
        if (!note.title) note.title = note.textContent.trim();
    });

    // Biểu mẫu: có lỗi thì đưa con trỏ tới ô lỗi đầu tiên; trang tạo mới thì tới ô trống đầu tiên.
    const firstError = document.querySelector('.admin-main .input-validation-error');
    if (firstError) {
        firstError.focus({ preventScroll: true });
        firstError.scrollIntoView({ block: 'center' });
    } else if (/\/Create\/?$/i.test(location.pathname) && window.matchMedia('(hover: hover)').matches) {
        const form = Array.from(document.querySelectorAll('.admin-main form'))
            .find(candidate => candidate.method.toLowerCase() === 'post' && !candidate.hasAttribute('data-pos-form'));
        const first = form && controlsOf(form).find(field => !field.disabled && !field.value && field.type !== 'file' && field.offsetParent !== null);
        first?.focus({ preventScroll: true });
    }
})();

// Khoảng ngày: chặn "từ ngày" sau "đến ngày" ngay trên form; báo cáo có nút chọn nhanh.
(() => {
    'use strict';

    const pad = value => String(value).padStart(2, '0');
    const iso = date => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
    // "Hôm nay" theo giờ Việt Nam, không theo múi giờ của máy người xem.
    const today = window.vnTime.parts();
    const [year, month, day] = [today.y, today.month, today.day];
    const now = new Date(year, month, day);
    const presets = [
        ['Hôm nay', new Date(year, month, day), now],
        ['7 ngày qua', new Date(year, month, day - 6), now],
        ['30 ngày qua', new Date(year, month, day - 30), now],
        ['Tháng này', new Date(year, month, 1), now],
        ['Tháng trước', new Date(year, month - 1, 1), new Date(year, month, 0)]
    ];

    document.querySelectorAll('.admin-main form').forEach(form => {
        if (form.method.toLowerCase() !== 'get') return;
        const from = form.querySelector('input[type="date"][name="tuNgay"], input[type="date"][name="fromDate"]');
        const to = form.querySelector('input[type="date"][name="denNgay"], input[type="date"][name="toDate"]');
        if (!from || !to) return;

        const link = () => {
            if (from.value) to.min = from.value; else to.removeAttribute('min');
            to.setCustomValidity(from.value && to.value && to.value < from.value ? 'Đến ngày không được trước từ ngày.' : '');
        };
        from.addEventListener('change', link);
        to.addEventListener('change', link);
        link();

        if (!form.classList.contains('filter-bar') || !location.pathname.toLowerCase().startsWith('/baocao')) return;
        const group = document.createElement('div');
        group.className = 'date-presets';
        group.setAttribute('role', 'group');
        group.setAttribute('aria-label', 'Chọn nhanh khoảng ngày');
        presets.forEach(([label, start, end]) => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'btn btn-sm btn-outline-secondary';
            button.textContent = label;
            if (from.value === iso(start) && to.value === iso(end)) button.classList.add('active');
            button.addEventListener('click', () => {
                from.value = iso(start);
                to.value = iso(end);
                link();
                form.requestSubmit();
            });
            group.append(button);
        });
        form.prepend(group);
    });
})();

// Quay lại danh sách vẫn giữ bộ lọc và trang đang xem.
(() => {
    'use strict';

    const key = path => `admin-list:${path.toLowerCase().replace(/\/index\/?$/, '').replace(/\/$/, '')}`;
    const read = name => { try { return sessionStorage.getItem(name); } catch { return null; } };
    const write = (name, value) => {
        try {
            if (value) sessionStorage.setItem(name, value); else sessionStorage.removeItem(name);
        } catch { /* Trình duyệt chặn lưu tạm: bỏ qua, liên kết vẫn hoạt động. */ }
    };

    const main = document.querySelector('.admin-main');
    if (!main) return;
    const isList = Array.from(main.querySelectorAll('form')).some(form => form.method.toLowerCase() === 'get') || main.querySelector('.pagination');
    if (isList) write(key(location.pathname), location.search);

    main.querySelectorAll('a.page-back, a.btn[href]').forEach(link => {
        if (link.search || !link.pathname || !link.classList.contains('page-back') && !/^(Quay lại|Danh sách|Hủy)/.test(link.textContent.trim())) return;
        const saved = read(key(link.pathname));
        if (saved) link.search = saved;
    });

    // Lưu xong quay về danh sách cũng giữ bộ lọc; thêm mới thì không, để thấy ngay dòng vừa tạo.
    const listQuery = isList ? location.search : read(key(`/${location.pathname.split('/')[1]}`));
    if (!listQuery || /\/Create\/?$/i.test(location.pathname)) return;
    main.querySelectorAll('form').forEach(form => {
        if (form.method.toLowerCase() !== 'post') return;
        const field = document.createElement('input');
        field.type = 'hidden';
        field.name = '__listQuery';
        field.value = listQuery;
        form.append(field);
    });
})();
