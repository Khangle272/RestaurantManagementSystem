(() => {
    'use strict';
    const modal = document.getElementById('assignment-modal');
    if (!modal || !window.bootstrap) return;
    const content = document.getElementById('assignment-content');
    const feedback = document.getElementById('reservation-feedback');
    let controller;
    async function load(url) {
        controller?.abort();
        const pending = controller = new AbortController();
        const selected = new Set([...content.querySelectorAll('input[name="banAnIds"]:checked')].map(input => input.value));
        const timeout = setTimeout(() => pending.abort(), 8000);
        content.setAttribute('aria-busy', 'true');
        try {
            const response = await fetch(url, { cache: 'no-store', signal: pending.signal });
            if (!response.ok || response.redirected) throw new Error('Không tải được sơ đồ hoặc phiên đăng nhập đã thay đổi. Đóng hộp và thử lại.');
            const html = await response.text();
            if (pending !== controller) return;
            content.innerHTML = html;
            if (selected.size) content.querySelectorAll('input[name="banAnIds"]').forEach(input => {
                input.checked = selected.has(input.value) && !input.disabled;
            });
            content.dispatchEvent(new CustomEvent('booking:loaded', { bubbles: true }));
        } catch (error) {
            if (pending !== controller) return;
            let warning = content.querySelector('[data-load-error]');
            if (!warning) { warning = document.createElement('p'); warning.dataset.loadError = ''; content.prepend(warning); }
            warning.className = 'alert alert-danger'; warning.setAttribute('role', 'alert');
            warning.textContent = error.name === 'AbortError' ? 'Kết nối chậm. Lựa chọn chưa được lưu; vui lòng thử lại.' : error.message;
        } finally { clearTimeout(timeout); if (pending === controller) content.removeAttribute('aria-busy'); }
    }
    document.addEventListener('click', e => {
        const link = e.target.closest('[data-assign-booking], [data-reschedule-booking]');
        if (!link) return;
        e.preventDefault();
        content.replaceChildren();
        content.textContent = 'Đang tải sơ đồ đúng ngày và ca của khách…';
        document.getElementById('assignment-modal-title').textContent = link.hasAttribute('data-reschedule-booking') ? 'Đổi giờ đặt bàn' : 'Xếp bàn cho yêu cầu đặt chỗ';
        const url = new URL(link.href); url.searchParams.set('dialog', 'true');
        const preferred = new URL(location.href).searchParams.get('preferredTableId');
        if (preferred && /^\d+$/.test(preferred)) url.searchParams.set('preferredTableId', preferred);
        bootstrap.Modal.getOrCreateInstance(modal).show();
        load(url);
    });
    content.addEventListener('submit', e => {
        if (e.target.id !== 'assignment-filters') return;
        e.preventDefault();
        const url = new URL(e.target.action);
        for (const [key, value] of new FormData(e.target)) url.searchParams.set(key, value);
        load(url);
    });
    modal.addEventListener('hidden.bs.modal', () => { controller?.abort(); });
    content.addEventListener('ajax:before', e => {
        if (content.getAttribute('aria-busy') === 'true') e.preventDefault();
    });
    content.addEventListener('ajax:success', e => {
        if (!['assign-booking', 'reschedule-booking'].includes(e.target.id)) return;
        e.preventDefault();
        bootstrap.Modal.getInstance(modal)?.hide();
        feedback.hidden = false; feedback.textContent = e.detail.data.message;
        (async () => {
            try {
                const response = await fetch(location.href, { cache: 'no-store', signal: AbortSignal.timeout(8000) });
                const page = new DOMParser().parseFromString(await response.text(), 'text/html');
                const next = page.getElementById('reservation-list');
                if (!response.ok || response.redirected || !next) throw new Error();
                document.getElementById('reservation-list').replaceWith(next);
            } catch { feedback.textContent += ' Đã lưu nhưng chưa cập nhật được danh sách. Tải lại danh sách để xem, không cần gửi lại xếp bàn.'; }
        })();
    });
    const newlyCreated = new URL(location.href).searchParams.get('xepBanId');
    if (newlyCreated && /^\d+$/.test(newlyCreated)) document.querySelector(`[data-assign-booking="${newlyCreated}"]`)?.click();
})();
