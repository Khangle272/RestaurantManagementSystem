(() => {
    'use strict';
    const form = document.querySelector('form[data-slot-url]');
    if (!form) return;
    const day = form.querySelector('#booking-day'), arrival = form.querySelector('#inputThoiGianDen');
    const guests = form.querySelector('[name="SoNguoi"]'), area = form.querySelector('[name="MaKhuVuc"], [name="KhuVucId"]');
    const status = form.querySelector('#slot-status'), suggestions = form.querySelector('[data-time-suggestions]');
    const timeError = form.querySelector('[data-time-error]');
    const serverNow = Date.parse(form.dataset.serverNow), started = Date.now(), lead = Number(form.dataset.leadMinutes);
    const localValue = ms => new Date(ms + 7 * 3600000).toISOString().slice(0, 16);
    function validateTime() {
        const now = serverNow + Date.now() - started;
        arrival.min = localValue(Math.ceil((now + lead * 60000) / 60000) * 60000);
        arrival.max = localValue(now + 180 * 86400000);
        day.min = localValue(now).slice(0, 10); day.max = arrival.max.slice(0, 10);
        const value = arrival.value, clock = value.split('T')[1];
        const error = !value ? '' : value < arrival.min
            ? (lead > 0 ? 'Giờ này đã qua hoặc chưa đủ 30 phút đặt trước. Vui lòng chọn giờ khác.' : 'Giờ nhận khách đã quá 15 phút. Vui lòng chọn lại.')
            : value > arrival.max ? 'Chỉ nhận đặt bàn trong 180 ngày tới.'
            : clock < form.dataset.openTime || clock >= form.dataset.closeTime ? `Chọn giờ trong ${form.dataset.openTime}–${form.dataset.closeTime}, trước giờ đóng cửa.`
            : value.slice(0, 10) !== day.value ? 'Ngày đặt và giờ đến chưa khớp.' : '';
        arrival.setCustomValidity(error); arrival.setAttribute('aria-invalid', String(Boolean(error)));
        arrival.classList.toggle('is-invalid', Boolean(error));
        timeError.textContent = error; timeError.hidden = !error;
        return !error;
    }
    let controller, sequence = 0, ready = false, timer;
    async function refresh() {
        const current = ++sequence;
        controller?.abort(); ready = false;
        validateTime();
        suggestions.replaceChildren();
        const pending = controller = new AbortController(), timeout = setTimeout(() => pending.abort(), 8000);
        status.textContent = 'Đang kiểm tra bàn cho giờ đến đã chọn…';
        try {
            const url = new URL(form.dataset.slotUrl, location.origin);
            url.searchParams.set('ngay', day.value); url.searchParams.set('gioDen', arrival.value);
            url.searchParams.set('soNguoi', guests.value);
            if (area?.value) url.searchParams.set('maKhuVuc', area.value);
            const response = await fetch(url, { cache: 'no-store', signal: pending.signal });
            if (response.redirected || !response.headers.get('content-type')?.includes('json')) throw new Error('Phiên đăng nhập thay đổi. Vui lòng tải lại trang.');
            const data = await response.json();
            if (!response.ok) throw new Error(data.message || 'Không kiểm tra được bàn.');
            if (current !== sequence) return;
            suggestions.replaceChildren();
            for (const slot of data.slots) {
                const button = document.createElement('button'); button.type = 'button';
                const selected = slot.value === arrival.value;
                button.className = 'btn btn-sm ' + (selected ? 'btn-success' : 'btn-outline-success');
                button.textContent = slot.label + (slot.available ? '' : ' · Hết bàn');
                button.setAttribute('aria-pressed', String(selected));
                button.setAttribute('aria-label', slot.label + ' · ' + slot.reason);
                button.disabled = !slot.available; button.title = slot.reason;
                button.addEventListener('click', () => { arrival.value = slot.value; refresh(); });
                suggestions.append(button);
            }
            if (!data.slots.length) {
                const note = document.createElement('span'); note.className = 'small text-muted';
                note.textContent = 'Ngày này không còn ca nhận khách phù hợp với thời gian hiện tại. Chọn ngày khác.';
                suggestions.append(note);
            }
            ready = data.available && validateTime(); status.textContent = data.message;
        } catch (error) {
            if (current === sequence) status.textContent = error.name === 'AbortError' ? 'Kết nối chậm. Thông tin vẫn được giữ; bấm kiểm tra lại.' : error.message;
        } finally { clearTimeout(timeout); }
    }
    day.addEventListener('change', () => { if (arrival.value) arrival.value = day.value + 'T' + arrival.value.split('T')[1]; refresh(); });
    arrival.addEventListener('input', () => { ready = false; sequence++; controller?.abort(); if (arrival.value) day.value = arrival.value.split('T')[0]; validateTime(); });
    arrival.addEventListener('change', refresh);
    arrival.addEventListener('focus', validateTime);
    area?.addEventListener('change', refresh);
    guests.addEventListener('input', () => { ready = false; sequence++; controller?.abort(); clearTimeout(timer); timer = setTimeout(refresh, 250); });
    form.querySelector('[data-recheck-slots]')?.addEventListener('click', refresh);
    form.addEventListener('click', e => { if (e.target.closest('.quick-guest')) setTimeout(refresh, 0); });
    form.addEventListener('submit', e => { if (!validateTime()) { e.preventDefault(); arrival.reportValidity(); } }, true);
    form.addEventListener('ajax:before', e => { if (!ready || !validateTime()) { e.preventDefault(); status.textContent = timeError.textContent || 'Kiểm tra và chọn giờ còn bàn trước khi gửi.'; } });
    refresh();
})();
