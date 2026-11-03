(() => {
    function initialize(root = document) {
    const board = root.querySelector('#table-board');
    const assignment = root.querySelector('#assign-booking');
    if (assignment && board) {
        if (assignment.dataset.boardInitialized) return;
        assignment.dataset.boardInitialized = 'true';
        const choices = () => [...board.querySelectorAll('input[name="banAnIds"]')];
        const summary = document.getElementById('table-selection-summary');
        const updateSelection = () => {
            const selected = choices().filter(input => input.checked && !input.disabled);
            const seats = selected.reduce((total, input) => total + Number(input.dataset.seats), 0);
            summary.textContent = selected.length === 1 ? `Đã chọn 1 bàn · ${seats} chỗ cho ${assignment.dataset.guests} khách. Chọn bàn khác sẽ thay bàn này.` : 'Chọn một bàn trên sơ đồ để xác nhận.';
            assignment.querySelector('button[type="submit"], button:not([type])').disabled = selected.length !== 1 || seats < Number(assignment.dataset.guests);
            for (const input of choices()) input.closest('.table-card').classList.toggle('is-selected', input.checked);
        };
        board.addEventListener('change', updateSelection);
        root.querySelector('#choose-suggestion')?.addEventListener('click', () => {
            const suggested = choices().find(input => !input.disabled && input.dataset.suggested === 'true');
            for (const input of choices()) input.checked = input === suggested;
            updateSelection();
            choices().find(input => input.checked)?.closest('.table-card').scrollIntoView({behavior:'smooth',block:'center'});
        });
        updateSelection();
    }
    }
    initialize();
    document.addEventListener('booking:loaded', e => initialize(e.target));
    document.addEventListener('click', e => {
        const button = e.target.closest('[data-table-warning]');
        if (!button) return;
        const warning = document.getElementById('assignment-warning');
        if (warning) { warning.textContent = button.dataset.tableWarning; warning.hidden = false; warning.tabIndex = -1; warning.focus(); }
    });
    const blockedModal = document.getElementById('table-blocked-modal');
    const blockedBody = document.getElementById('table-blocked-body');
    const blockedTitle = document.getElementById('table-blocked-title');
    function showBlocked(card) {
        const reason = card.dataset.tableBlocked;
        if (!reason) return;
        const name = card.dataset.tableName;
        if (blockedModal && blockedBody && window.bootstrap) {
            if (blockedTitle) blockedTitle.textContent = name ? `Bàn ${name} không thể chọn` : 'Không thể chọn bàn';
            blockedBody.textContent = reason;
            bootstrap.Modal.getOrCreateInstance(blockedModal).show();
            return;
        }
        const warning = document.getElementById('assignment-warning');
        if (warning) { warning.textContent = reason; warning.hidden = false; warning.tabIndex = -1; warning.focus(); }
    }
    document.addEventListener('click', e => {
        const card = e.target.closest('[data-table-blocked]');
        if (!card) return;
        if (e.target.closest('a, button, input, select, textarea, form, label')) return;
        showBlocked(card);
    });
    document.addEventListener('keydown', e => {
        if (e.key !== 'Enter' && e.key !== ' ') return;
        const card = e.target.closest('[data-table-blocked][role="button"]');
        if (!card) return;
        e.preventDefault();
        showBlocked(card);
    });
    const board = document.getElementById('table-board');
    const status = document.getElementById('board-refresh');
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
