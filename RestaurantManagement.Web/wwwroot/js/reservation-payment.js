(() => {
    const copy = document.getElementById('copy-payment-code');
    copy?.addEventListener('click', async () => {
        try { await navigator.clipboard.writeText(document.getElementById('payment-reference').textContent.trim()); copy.textContent = 'Đã sao chép'; }
        catch { copy.textContent = 'Chọn mã để sao chép thủ công'; }
    });
    const waiting = document.getElementById('payment-waiting');
    if (!waiting) return;
    const button = document.getElementById('payment-check');
    const feedback = document.getElementById('payment-check-feedback');
    let checking = false;
    async function checkStatus(manual = false) {
        if (manual) feedback.textContent = 'Đang kiểm tra trạng thái…';
        if (checking) return;
        checking = true;
        button.disabled = true;
        button.textContent = 'Đang kiểm tra…';
        const controller = new AbortController();
        const timeout = setTimeout(() => controller.abort(), 8000);
        try {
            const response = await fetch(waiting.dataset.statusUrl, {
                cache: 'no-store', signal: controller.signal,
                headers: { Accept: 'application/json', 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (response.redirected || response.status === 401 || response.status === 403)
                throw new Error('Phiên đăng nhập đã thay đổi. Vui lòng đăng nhập đúng tài khoản khách hàng của lịch này rồi kiểm tra lại.');
            if (!response.ok || !response.headers.get('content-type')?.includes('application/json'))
                throw new Error('Không lấy được trạng thái. Vui lòng thử lại; không cần chuyển tiền thêm.');
            const status = await response.json();
            if (typeof status.paymentPending !== 'boolean' || typeof status.paymentExpired !== 'boolean' || typeof status.status !== 'string' || !Number.isFinite(status.depositRemaining))
                throw new Error('Trạng thái trả về không hợp lệ. Vui lòng thử lại.');
            const confirmed = ['DaXacNhan', 'DaNhanBan'].includes(status.status) && status.depositRemaining === 0;
            if (confirmed || status.paymentExpired || !status.paymentPending || ['DaHuy', 'KhongDen'].includes(status.status)) {
                feedback.textContent = confirmed ? 'Nhà hàng đã xác nhận nhận cọc. Đang cập nhật lịch…'
                    : status.paymentExpired ? 'Đã hết hạn giữ chỗ. Đang cập nhật thông tin; không chuyển thêm tiền…'
                    : 'Nhà hàng đã cập nhật thông báo chuyển tiền hoặc lịch đặt bàn. Đang tải thông tin mới…';
                clearInterval(timer);
                location.reload();
                return;
            }
            feedback.textContent = `Nhà hàng chưa xác nhận nhận tiền. Đã kiểm tra lúc ${new Date().toLocaleTimeString('vi-VN')}. Bạn không cần chuyển lại.`;
        } catch (error) {
            feedback.textContent = error.name === 'AbortError' ? 'Kiểm tra quá lâu. Vui lòng thử lại; không cần chuyển tiền thêm.'
                : error.name === 'TypeError' || error.name === 'SyntaxError' ? 'Không lấy được trạng thái. Kiểm tra mạng rồi thử lại; không cần chuyển tiền thêm.' : error.message;
        } finally {
            clearTimeout(timeout);
            checking = false;
            button.disabled = false;
            button.textContent = 'Kiểm tra trạng thái';
        }
    }
    button.addEventListener('click', () => checkStatus(true));
    const timer = setInterval(() => { if (!document.hidden) return checkStatus(); }, 10000);
})();
