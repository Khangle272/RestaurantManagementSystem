(() => {
    const watch = document.getElementById('order-watch');
    if (!watch) return;
    let pending = false;
    const timer = setInterval(async () => {
        if (document.hidden || pending) return;
        pending = true;
        try {
            const response = await fetch(watch.dataset.url, { credentials: 'same-origin', headers: { Accept: 'application/json' } });
            if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) {
                if (response.status === 401 || response.status === 403 || response.redirected) clearInterval(timer);
                return;
            }
            const order = await response.json();
            document.getElementById('order-update-note').textContent = 'Cập nhật lúc ' + new Date().toLocaleTimeString('vi-VN');
            document.getElementById('order-status').textContent = order.status;
            for (const line of order.lines) {
                const cell = document.querySelector(`[data-line-status="${Number(line.id)}"]`);
                if (cell) cell.textContent = line.status;
            }
            if (order.closed) {
                clearInterval(timer);
                document.getElementById('order-closed').hidden = false;
            }
        } catch { document.getElementById('order-update-note').textContent = 'Chưa cập nhật được tiến độ. Đang chờ kết nối để thử lại.'; }
        finally { pending = false; }
    }, 15000);
})();
