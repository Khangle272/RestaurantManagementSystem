// UX dùng chung cho trang khách và khu quản trị.
(() => {
    'use strict';
    const reducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    const closeAlert = alert => {
        if (!document.body.contains(alert)) return;
        if (window.bootstrap?.Alert) window.bootstrap.Alert.getOrCreateInstance(alert).close();
        else alert.remove();
    };

    // Thông báo thành công tự ẩn sau 5 giây (dừng khi rê chuột vào); thông báo lỗi giữ lại tới khi đóng.
    document.querySelectorAll('.flash-stack .alert-success').forEach(alert => {
        if (reducedMotion) { setTimeout(() => closeAlert(alert), 5000); return; }
        const bar = document.createElement('span');
        bar.className = 'flash-progress';
        bar.style.animationDuration = '5s';
        bar.addEventListener('animationend', () => closeAlert(alert));
        alert.append(bar);
    });

    // Hộp xác nhận thay cho window.confirm. Dùng: data-confirm="Nội dung" trên form, nút submit hoặc liên kết;
    // tùy chọn data-confirm-title, data-confirm-ok, data-confirm-cancel, data-confirm-variant (danger | warning | primary).
    let confirmModal;
    function buildConfirmModal() {
        const el = document.createElement('div');
        el.className = 'modal fade ux-confirm';
        el.tabIndex = -1;
        el.setAttribute('aria-hidden', 'true');
        el.setAttribute('aria-labelledby', 'ux-confirm-title');
        el.innerHTML = `
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content">
                    <div class="modal-body">
                        <span class="ux-confirm-icon" aria-hidden="true"><i class="bi bi-exclamation-triangle"></i></span>
                        <div>
                            <h2 class="ux-confirm-title" id="ux-confirm-title"></h2>
                            <p class="ux-confirm-message"></p>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal" data-ux-cancel></button>
                        <button type="button" class="btn" data-ux-ok></button>
                    </div>
                </div>
            </div>`;
        document.body.append(el);
        return el;
    }
    window.uxConfirm = (message, options = {}) => new Promise(resolve => {
        if (!window.bootstrap?.Modal) { resolve(window.confirm(message)); return; }
        const variant = ['danger', 'warning', 'primary'].includes(options.variant) ? options.variant : 'danger';
        confirmModal ??= buildConfirmModal();
        const el = confirmModal;
        const ok = el.querySelector('[data-ux-ok]');
        const cancel = el.querySelector('[data-ux-cancel]');
        el.dataset.variant = variant;
        el.querySelector('.ux-confirm-icon i').className = `bi ${variant === 'primary' ? 'bi-question-circle' : 'bi-exclamation-triangle'}`;
        el.querySelector('.ux-confirm-title').textContent = options.title || 'Xác nhận thao tác';
        el.querySelector('.ux-confirm-message').textContent = message;
        ok.textContent = options.ok || 'Đồng ý';
        ok.className = `btn btn-${variant}`;
        cancel.textContent = options.cancel || 'Hủy';
        let accepted = false;
        const modal = window.bootstrap.Modal.getOrCreateInstance(el);
        ok.onclick = () => { accepted = true; modal.hide(); };
        // Thao tác nguy hiểm: để sẵn tiêu điểm ở nút Hủy, tránh lỡ tay bấm Enter.
        el.addEventListener('shown.bs.modal', () => (variant === 'danger' ? cancel : ok).focus(), { once: true });
        el.addEventListener('hidden.bs.modal', () => resolve(accepted), { once: true });
        modal.show();
    });
    const confirmOptions = source => ({
        title: source.dataset.confirmTitle, ok: source.dataset.confirmOk, cancel: source.dataset.confirmCancel, variant: source.dataset.confirmVariant
    });

    // Bắt ở pha capture để chạy trước các xử lý submit khác (ajax, khóa bấm lặp...).
    document.addEventListener('submit', event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement)) return;
        const submitter = event.submitter;
        const source = submitter?.hasAttribute('data-confirm') ? submitter : form;
        const message = source.dataset.confirm;
        if (!message) return;
        if (form.dataset.confirmed === 'true') { delete form.dataset.confirmed; return; }
        event.preventDefault();
        event.stopImmediatePropagation();
        window.uxConfirm(message, confirmOptions(source)).then(accepted => {
            if (!accepted) return;
            form.dataset.confirmed = 'true';
            if (form.requestSubmit) form.requestSubmit(submitter?.form === form ? submitter : undefined);
            else form.submit();
        });
    }, true);
    document.addEventListener('click', event => {
        const link = event.target.closest?.('a[data-confirm]');
        if (!link || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey) return;
        event.preventDefault();
        window.uxConfirm(link.dataset.confirm, confirmOptions(link)).then(accepted => { if (accepted) window.location.href = link.href; });
    });

    // Chống bấm gửi hai lần ở form POST thường (form ajax tự khóa nút riêng).
    document.addEventListener('submit', event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() !== 'post') return;
        if (form.hasAttribute('data-ajax') || form.dataset.ajaxForm === 'true' || form.hasAttribute('data-pos-form') || form.dataset.noLock !== undefined) return;
        setTimeout(() => {
            if (event.defaultPrevented) return;
            if (form.dataset.submitting === 'true') return;
            form.dataset.submitting = 'true';
            const button = event.submitter || form.querySelector('button[type="submit"]');
            button?.classList.add('is-loading');
            button?.setAttribute('aria-busy', 'true');
            // Mở lại nút khi quay về trang bằng nút Back (bfcache).
            window.addEventListener('pageshow', () => {
                form.dataset.submitting = 'false';
                button?.classList.remove('is-loading');
                button?.removeAttribute('aria-busy');
            }, { once: true });
        }, 0);
        if (form.dataset.submitting === 'true') event.preventDefault();
    });
    document.addEventListener('ajax:before', event => {
        const button = event.detail?.submitter || event.target.querySelector('button[type="submit"]');
        if (!button) return;
        button.classList.add('is-loading');
        event.target.addEventListener('ajax:complete', () => button.classList.remove('is-loading'), { once: true });
    });

    // Cảnh báo khi rời trang mà biểu mẫu nhập liệu (từ 3 ô trở lên) đã sửa nhưng chưa lưu. Chỉ áp dụng khu quản trị.
    const dirtyForms = new Set();
    const editableFields = form => Array.from(form.elements).filter(field =>
        /^(input|select|textarea)$/i.test(field.tagName) && !/^(hidden|submit|button|reset|search)$/i.test(field.type));
    const guarded = form => form instanceof HTMLFormElement && form.method.toLowerCase() === 'post'
        && form.closest('.admin-main') && !form.hasAttribute('data-pos-form') && !form.closest('[data-no-unsaved-guard]')
        && editableFields(form).length >= 3;
    const markDirty = event => {
        const form = event.target.form;
        if (event.isTrusted && guarded(form)) dirtyForms.add(form);
    };
    document.addEventListener('input', markDirty);
    document.addEventListener('change', markDirty);
    document.addEventListener('submit', event => {
        const form = event.target;
        if (!dirtyForms.has(form)) return;
        dirtyForms.delete(form);
        setTimeout(() => { if (event.defaultPrevented) dirtyForms.add(form); }, 0);
    });
    document.addEventListener('ajax:error', event => { if (guarded(event.target)) dirtyForms.add(event.target); });
    document.addEventListener('hidden.bs.modal', event => event.target.querySelectorAll?.('form').forEach(form => dirtyForms.delete(form)));
    const nativeSubmit = HTMLFormElement.prototype.submit;
    HTMLFormElement.prototype.submit = function () { dirtyForms.delete(this); return nativeSubmit.call(this); };
    window.addEventListener('beforeunload', event => {
        dirtyForms.forEach(form => { if (!document.body.contains(form)) dirtyForms.delete(form); });
        if (!dirtyForms.size) return;
        event.preventDefault();
        event.returnValue = '';
    });

    // Nút lên đầu trang cho trang dài.
    const backToTop = document.createElement('button');
    backToTop.type = 'button';
    backToTop.className = 'back-to-top';
    backToTop.setAttribute('aria-label', 'Lên đầu trang');
    backToTop.title = 'Lên đầu trang';
    backToTop.innerHTML = '<i class="bi bi-arrow-up" aria-hidden="true"></i>';
    backToTop.addEventListener('click', () => window.scrollTo({ top: 0, behavior: reducedMotion ? 'auto' : 'smooth' }));
    document.body.append(backToTop);
    const toggleBackToTop = () => backToTop.classList.toggle('show', window.scrollY > 600);
    window.addEventListener('scroll', toggleBackToTop, { passive: true });
    toggleBackToTop();
})();
