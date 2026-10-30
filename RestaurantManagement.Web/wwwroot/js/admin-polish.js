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
