(() => {
    'use strict';
    const form = document.querySelector('form[data-slot-url]');
    if (!form) return;
    const day = form.querySelector('#booking-day');
    const select = form.querySelector('#inputThoiGianDen');
    const guests = form.querySelector('[name="SoNguoi"]');
    const area = form.querySelector('[name="MaKhuVuc"], [name="KhuVucId"]');
    const mode = form.querySelector('[name="DatTheoCa"][type="checkbox"]');
    const status = form.querySelector('#slot-status');
    let controller, sequence = 0, ready = false, timer;
    const scheduled = () => !mode || mode.checked;
    async function refresh() {
        const current = ++sequence;
        controller?.abort();
        if (!scheduled()) { ready = true; return; }
        ready = false;
        const previous = select.value;
        select.disabled = true;
        status.textContent = 'Đang kiểm tra ca và số chỗ phù hợp…';
        const pending = controller = new AbortController();
        const timeout = setTimeout(() => pending.abort(), 8000);
        try {
            const url = new URL(form.dataset.slotUrl, location.origin);
            url.searchParams.set('ngay', day.value);
            url.searchParams.set('soNguoi', guests.value);
            if (area?.value) url.searchParams.set('maKhuVuc', area.value);
            const response = await fetch(url, { cache: 'no-store', signal: pending.signal });
            if (response.redirected || !response.headers.get('content-type')?.includes('json')) throw new Error('Phiên đăng nhập hoặc kết nối đã thay đổi. Vui lòng tải lại trang.');
            const data = await response.json();
            if (!response.ok) throw new Error(data.message || 'Không kiểm tra được ca.');
            if (current !== sequence) return;
            select.replaceChildren();
            for (const slot of data.slots) {
                const option = new Option(`${slot.label} — ${slot.reason}`, slot.value);
                option.disabled = !slot.available;
                select.add(option);
            }
            const chosen = [...select.options].find(x => x.value === previous && !x.disabled)
                || [...select.options].find(x => !x.disabled);
            select.value = chosen?.value || '';
            select.disabled = false;
            ready = !!chosen;
            status.textContent = chosen ? 'Còn ca phù hợp. Hệ thống kiểm tra lại trước khi giữ chỗ; chưa xác nhận thanh toán.'
                : 'Ngày/khu vực này không còn ca phù hợp cho số khách đã nhập. Chọn ngày hoặc khu vực khác.';
        } catch (error) {
            if (current !== sequence) return;
            status.textContent = error.name === 'AbortError' ? 'Kết nối chậm. Dữ liệu vẫn được giữ; đổi ngày hoặc thử lại.' : error.message;
        } finally { clearTimeout(timeout); }
    }
    function switchMode() {
        form.querySelectorAll('[data-walkin-fields]').forEach(group => {
            group.hidden = scheduled();
            group.querySelectorAll('input').forEach(input => input.disabled = scheduled());
        });
        form.querySelectorAll('[data-reservation-fields]').forEach(group => group.hidden = !scheduled());
        day.disabled = select.disabled = !scheduled();
        if (!scheduled()) status.textContent = 'Khách vãng lai: kiểm tra cả thời lượng sử dụng trước lịch đặt tiếp theo.';
        refresh();
    }
    [day, area].forEach(input => input?.addEventListener('change', refresh));
    form.querySelector('[data-recheck-slots]')?.addEventListener('click', refresh);
    guests.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(refresh, 250); });
    form.addEventListener('click', e => { if (e.target.closest('.quick-guest')) refresh(); });
    mode?.addEventListener('change', switchMode);
    form.addEventListener('ajax:before', e => {
        if (scheduled() && (!ready || select.selectedIndex < 0 || select.selectedOptions[0].disabled)) {
            e.preventDefault();
            status.textContent = 'Vui lòng chọn ca còn bàn hoặc kiểm tra lại tình trạng ca trước khi gửi.';
        }
    });
    switchMode();
})();
