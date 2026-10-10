// Run: node tests/Management.SmokeTests/reservation-payment.check.cjs
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const script = fs.readFileSync(path.join(__dirname, '../../RestaurantManagement.Web/wwwroot/js/reservation-payment.js'), 'utf8');
const view = fs.readFileSync(path.join(__dirname, '../../RestaurantManagement.Web/Views/DatBan/ThanhToanCoc.cshtml'), 'utf8');
const pending = { paymentPending: true, paymentExpired: false, status: 'ChoCoc', depositRemaining: 299000 };
const response = (data = pending) => ({ ok: true, status: 200, headers: { get: () => 'application/json' }, json: async () => data });

function page(fetchResult = async () => response()) {
    const button = { textContent: 'Kiểm tra trạng thái', disabled: false, addEventListener: (event, handler) => { button.click = handler; } };
    const feedback = { textContent: '' };
    const elements = { 'payment-check': button, 'payment-check-feedback': feedback, 'payment-waiting': { dataset: { statusUrl: '/DatBan/CartContext/15' } } };
    const document = { hidden: false, getElementById: id => elements[id] };
    const state = { button, feedback, document, requests: 0, reloads: 0, stopped: false };
    vm.runInNewContext(script, {
        document, navigator: {}, Date, AbortController,
        fetch: async (url, options) => { state.requests++; state.url = url; state.options = options; return fetchResult(); },
        location: { reload: () => { state.reloads++; } },
        setInterval: handler => { state.poll = handler; return 1; },
        clearInterval: () => { state.stopped = true; },
        setTimeout: handler => { state.timeout = handler; return 2; },
        clearTimeout: () => {}
    });
    assert.equal(typeof button.click, 'function', 'Manual status button must check via AJAX');
    return state;
}

(async () => {
    assert.match(view, /id="payment-check"/);
    assert.match(view, /id="payment-check-feedback"[^>]*aria-live="polite"/);
    assert.doesNotMatch(view, /onclick="location\.reload\(\)"/);
    assert.match(view, /else if \(pending && !cancelled && !expired && !confirmed\)/, 'Do not render an inactive check button on cancelled, expired or confirmed bookings');
    const waiting = page();
    await waiting.button.click();
    assert.match(waiting.feedback.textContent, /chưa xác nhận.*Đã kiểm tra lúc/);
    assert.equal(waiting.requests, 1);
    assert.equal(waiting.reloads, 0, 'Pending status must not reload an unchanged page');
    assert.equal(waiting.button.disabled, false);
    assert.equal(waiting.options.cache, 'no-store');
    assert.equal(waiting.options.headers['X-Requested-With'], 'XMLHttpRequest');
    assert.equal(waiting.url, '/DatBan/CartContext/15');

    for (const status of [
        { ...pending, paymentPending: false, status: 'DaXacNhan', depositRemaining: 0 },
        { ...pending, status: 'DaNhanBan', depositRemaining: 0 },
        { ...pending, paymentExpired: true },
        { ...pending, paymentPending: false },
        { ...pending, status: 'DaHuy' }
    ]) {
        const changed = page(async () => response(status));
        await changed.button.click();
        assert.equal(changed.reloads, 1, 'Changed payment state must refresh the server-rendered page');
        assert.equal(changed.stopped, true);
    }

    for (const fetchResult of [
        async () => { throw new TypeError('offline'); },
        async () => response({}),
        async () => ({ ...response(), json: async () => { throw new SyntaxError('invalid JSON'); } }),
        async () => ({ ok: false, status: 500 }),
        async () => ({ ok: false, status: 403 }),
        async () => ({ ok: true, redirected: true, status: 200 }),
        async () => ({ ok: true, status: 200, headers: { get: () => 'text/html' } })
    ]) {
        const failed = page(fetchResult);
        await failed.button.click();
        assert.match(failed.feedback.textContent, /không|Không|đăng nhập/i);
        assert.equal(failed.button.disabled, false, 'Failed checks must allow retry');
        assert.equal(failed.button.textContent, 'Kiểm tra trạng thái');
        assert.equal(failed.reloads, 0);
    }

    let finish;
    let attempts = 0;
    const retry = page(async () => { if (++attempts === 1) throw new TypeError('offline'); return response(); });
    await retry.button.click();
    await retry.button.click();
    assert.equal(retry.requests, 2);
    assert.match(retry.feedback.textContent, /chưa xác nhận/);
    const busy = page(() => new Promise(resolve => { finish = resolve; }));
    const poll = busy.poll();
    await busy.button.click();
    assert.equal(busy.requests, 1, 'Polling and manual checks must not overlap');
    assert.match(busy.feedback.textContent, /Đang kiểm tra/);
    finish(response());
    await poll;
    assert.equal(busy.button.disabled, false);
    busy.document.hidden = true;
    await busy.poll();
    assert.equal(busy.requests, 1, 'Hidden tabs should not poll');
    let abortSignal;
    const timedOut = page(() => new Promise((resolve, reject) => {
        abortSignal = timedOut.options.signal;
        abortSignal.addEventListener('abort', () => reject(Object.assign(new Error('timeout'), { name: 'AbortError' })));
    }));
    const timeoutCheck = timedOut.button.click();
    timedOut.timeout();
    await timeoutCheck;
    assert.equal(abortSignal.aborted, true);
    assert.match(timedOut.feedback.textContent, /quá lâu/);
    assert.equal(timedOut.button.disabled, false);
    console.log('PASS reservation payment status: pending feedback, state changes, errors, retry, overlap and hidden tab');
})().catch(error => { console.error(error); process.exitCode = 1; });
