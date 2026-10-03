(() => {
    const board = document.getElementById('table-board');
    const status = document.getElementById('board-refresh');
    const assignment = document.getElementById('assign-booking');
    if (assignment && board) {
        const choices = () => [...board.querySelectorAll('input[name="banAnIds"]')];
        const summary = document.getElementById('table-selection-summary');
        const updateSelection = () => {
            const selected = choices().filter(input => input.checked && !input.disabled);
            const seats = selected.reduce((total, input) => total + Number(input.dataset.seats), 0);
            const sameArea = new Set(selected.map(input => input.dataset.area)).size <= 1;
            summary.textContent = selected.length ? `Đã chọn ${selected.length} bàn · ${seats} chỗ cho ${assignment.dataset.guests} khách${sameArea ? '' : ' · Cần chọn cùng khu vực'}` : 'Chọn bàn trên sơ đồ để xác nhận.';
            assignment.querySelector('button[type="submit"], button:not([type])').disabled = !selected.length || !sameArea || seats < Number(assignment.dataset.guests);
            for (const input of choices()) input.closest('.table-card').classList.toggle('is-selected', input.checked);
        };
        board.addEventListener('change', updateSelection);
        document.getElementById('choose-suggestion')?.addEventListener('click', () => {
            for (const input of choices()) input.checked = !input.disabled && input.dataset.suggested === 'true';
            updateSelection();
            choices().find(input => input.checked)?.closest('.table-card').scrollIntoView({behavior:'smooth',block:'center'});
        });
        updateSelection();
    }
    if (!board || board.dataset.poll !== 'true') { if (status) status.textContent = 'Đang xếp bàn · tạm dừng tự cập nhật'; return; }
    let busy = false;
    setInterval(async () => {
        if (busy || document.hidden || board.contains(document.activeElement)) return;
        busy = true;
        try {
            const url = new URL(location.href); url.searchParams.set('fragment', 'true');
            const response = await fetch(url, { cache: 'no-store', signal: AbortSignal.timeout(8000) });
            if (!response.ok || response.redirected) { status.textContent = 'Phiên đăng nhập hoặc kết nối đã thay đổi. Tải lại trang.'; return; }
            board.innerHTML = await response.text();
            status.textContent = 'Đã cập nhật ' + new Date().toLocaleTimeString('vi-VN');
        } catch { status.textContent = 'Mất kết nối · đang chờ cập nhật lại'; }
        finally { busy = false; }
    }, 10000);
})();
