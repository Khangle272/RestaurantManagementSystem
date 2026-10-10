(() => {
    'use strict';
    if (!window.fetch || !window.FormData) return;

    const safeUrl = value => {
        if (!value) return null;
        try {
            const url = new URL(value, window.location.href);
            return url.origin === window.location.origin && /^https?:$/.test(url.protocol) ? url.href : null;
        } catch { return null; }
    };
    const event = (form, name, detail) => form.dispatchEvent(new CustomEvent(name, { bubbles: true, cancelable: true, detail }));
    const messages = errors => {
        if (!errors) return [];
        if (typeof errors === 'string') return [errors];
        return Object.values(errors).flatMap(value => Array.isArray(value) ? value : [value]).filter(value => typeof value === 'string' && value.trim());
    };
    function feedback(form, text, success = false, loginUrl = null) {
        let box = form.querySelector('[data-ajax-feedback]');
        if (!box) {
            box = document.createElement('div');
            box.dataset.ajaxFeedback = '';
            box.setAttribute('role', 'alert');
            box.setAttribute('tabindex', '-1');
            form.prepend(box);
        }
        box.className = `alert ${success ? 'alert-success' : 'alert-danger'} my-2`;
        box.textContent = text;
        if (loginUrl) {
            const link = document.createElement('a');
            link.href = loginUrl;
            link.className = 'alert-link ms-2';
            link.textContent = 'Đăng nhập lại';
            box.append(link);
        }
        box.hidden = false;
        if (!success) box.focus({ preventScroll: true });
    }
    function clearValidation(form) {
        form.querySelector('[data-ajax-feedback]')?.setAttribute('hidden', '');
        form.querySelectorAll('[data-valmsg-for]').forEach(span => {
            span.textContent = '';
            span.classList.remove('field-validation-error');
            span.classList.add('field-validation-valid');
        });
        form.querySelectorAll('[data-valmsg-summary="true"]').forEach(summary => {
            summary.replaceChildren();
            summary.classList.remove('validation-summary-errors');
            summary.classList.add('validation-summary-valid');
        });
        form.querySelectorAll('.input-validation-error').forEach(input => {
            input.classList.remove('input-validation-error');
            input.removeAttribute('aria-invalid');
        });
    }
    function showValidation(form, errors) {
        if (!errors || typeof errors !== 'object' || Array.isArray(errors)) return;
        form.querySelectorAll('[data-valmsg-for]').forEach(span => {
            const fieldMessages = messages(errors[span.dataset.valmsgFor]);
            if (!fieldMessages.length) return;
            span.textContent = fieldMessages.join(' ');
            span.classList.remove('field-validation-valid');
            span.classList.add('field-validation-error');
        });
        Array.from(form.elements).forEach(input => {
            if (messages(errors[input.name]).length) {
                input.classList.add('input-validation-error');
                input.setAttribute('aria-invalid', 'true');
            }
        });
    }
    function fail(form, detail) {
        showValidation(form, detail.errors);
        feedback(form, detail.message || messages(detail.errors).join(' ') || 'Không thể lưu. Vui lòng kiểm tra thông tin và thử lại.', false, detail.loginUrl);
        event(form, 'ajax:error', detail);
    }
    async function send(form, submitter) {
        if (form.dataset.ajaxPending === 'true') return;
        if (!form.reportValidity() || !event(form, 'ajax:before', { submitter })) return;
        const action = safeUrl(submitter?.hasAttribute('formaction') ? submitter.formAction : form.action);
        if (!action) { fail(form, { message: 'Địa chỉ gửi biểu mẫu không hợp lệ.' }); return; }
        const method = (submitter?.hasAttribute('formmethod') ? submitter.formMethod : form.method || 'post').toUpperCase();
        const body = new FormData(form);
        if (submitter?.name) body.append(submitter.name, submitter.value);
        const buttons = Array.from(form.querySelectorAll('button[type="submit"], input[type="submit"], button:not([type])'));
        const disabled = buttons.map(button => button.disabled);
        form.dataset.ajaxPending = 'true';
        form.setAttribute('aria-busy', 'true');
        buttons.forEach(button => { button.disabled = true; });
        clearValidation(form);
        try {
            const url = new URL(action);
            if (method === 'GET') body.forEach((value, key) => url.searchParams.append(key, value));
            const response = await fetch(url.href, {
                method, credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json, text/html' },
                ...(method === 'GET' ? {} : { body })
            });
            const finalUrl = safeUrl(response.url);
            const isJson = (response.headers.get('content-type') || '').includes('json');
            const data = isJson ? await response.json() : await response.text();
            const page = isJson ? null : new DOMParser().parseFromString(data, 'text/html');
            const expired = response.status === 401 || response.status === 403 || (response.redirected && (page?.querySelector('input[type="password"]') || /\/(?:Account\/Login|Identity\/Account\/Login)(?:[/?]|$)/i.test(finalUrl || '')));
            if (expired) {
                const loginUrl = response.status === 401 || response.status === 403 ? safeUrl(data?.loginUrl) : finalUrl;
                fail(form, { data, response, expired: true, loginUrl, message: 'Phiên đăng nhập đã hết hoặc bạn không có quyền thực hiện. Dữ liệu đang nhập vẫn được giữ; vui lòng đăng nhập lại.' });
                return;
            }
            if (isJson) {
                if (!response.ok || data.ok === false || data.success === false || messages(data.errors).length) {
                    fail(form, { data, response, errors: data.errors, message: data.message || messages(data.errors).join(' ') });
                    return;
                }
                const redirectUrl = safeUrl(data.redirectUrl);
                if (event(form, 'ajax:success', { data, redirectUrl }) && redirectUrl) window.location.assign(redirectUrl);
                else if (!redirectUrl) feedback(form, data.message || 'Đã lưu thành công.', true);
                return;
            }
            const errors = {};
            page.querySelectorAll('[data-valmsg-for]').forEach(span => {
                if (span.textContent.trim()) errors[span.dataset.valmsgFor] = [span.textContent.trim()];
            });
            const htmlMessages = Array.from(page.querySelectorAll('.validation-summary-errors li, .alert-danger')).map(node => node.textContent.trim()).filter(Boolean);
            const returnedForm = Array.from(page.forms).find(candidate => candidate.id && candidate.id === form.id || candidate.getAttribute('action') && safeUrl(candidate.getAttribute('action')) === action);
            if (!response.ok || messages(errors).length || htmlMessages.length || (!response.redirected && returnedForm)) {
                const token = returnedForm?.querySelector('input[name="__RequestVerificationToken"]');
                if (token && form.elements.__RequestVerificationToken) form.elements.__RequestVerificationToken.value = token.value;
                fail(form, { data, response, errors, message: [...new Set([...messages(errors), ...htmlMessages])].join(' ') || `Không thể lưu (HTTP ${response.status}). Dữ liệu vẫn được giữ để bạn sửa và gửi lại.` });
                return;
            }
            if (event(form, 'ajax:success', { data, redirectUrl: finalUrl }) && finalUrl) window.location.assign(finalUrl);
        } catch {
            fail(form, { message: 'Không thể kết nối máy chủ. Dữ liệu đang nhập vẫn được giữ; vui lòng thử lại.' });
        } finally {
            delete form.dataset.ajaxPending;
            form.removeAttribute('aria-busy');
            buttons.forEach((button, index) => { button.disabled = disabled[index]; });
            event(form, 'ajax:complete', {});
        }
    }
    document.addEventListener('submit', e => {
        const form = e.target;
        if (!(form instanceof HTMLFormElement) || form.dataset.ajaxForm !== 'true' || e.defaultPrevented) return;
        const action = e.submitter?.hasAttribute('formaction') ? e.submitter.formAction : form.action;
        if (form.querySelector('input[type="password"]') || /\/(?:Login|Logout|Delete|ChangeRole|AssignRole|RoleChange)(?:[/?]|$)/i.test(action)) return;
        e.preventDefault();
        send(form, e.submitter);
    });
})();
