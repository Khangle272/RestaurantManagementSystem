(() => {
    'use strict';

    // Success alerts disappear on their own; errors stay until dismissed.
    document.querySelectorAll('.admin-main .alert-success.alert-dismissible').forEach(alert => {
        setTimeout(() => {
            if (!document.body.contains(alert)) return;
            if (window.bootstrap?.Alert) window.bootstrap.Alert.getOrCreateInstance(alert).close();
            else alert.remove();
        }, 5000);
    });

    // Prevent double submits on regular (non-ajax) POST forms.
    document.addEventListener('submit', event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() !== 'post') return;
        if (form.hasAttribute('data-ajax') || form.hasAttribute('data-pos-form') || form.dataset.noLock !== undefined) return;
        setTimeout(() => {
            if (event.defaultPrevented) return;
            if (form.dataset.submitting === 'true') return;
            form.dataset.submitting = 'true';
            const button = event.submitter || form.querySelector('button[type="submit"]');
            if (button) {
                button.classList.add('is-loading');
                button.setAttribute('aria-busy', 'true');
            }
            // Re-enable when returning via the back/forward cache.
            window.addEventListener('pageshow', () => {
                form.dataset.submitting = 'false';
                button?.classList.remove('is-loading');
                button?.removeAttribute('aria-busy');
            }, { once: true });
        }, 0);
        if (form.dataset.submitting === 'true') event.preventDefault();
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

    // Close the mobile menu after choosing a link.
    const menu = document.getElementById('management-menu');
    menu?.addEventListener('click', event => {
        if (!event.target.closest('a.nav-link') || window.innerWidth >= 768) return;
        window.bootstrap?.Collapse.getInstance(menu)?.hide();
    });
})();
