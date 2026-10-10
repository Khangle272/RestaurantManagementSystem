// Run from the repository root: node tests/Management.SmokeTests/StaffPosUiSmoke.cjs
// This checks client state and event contracts with a tiny DOM stub; browser layout still needs visual QA.
const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');

class Element {
    constructor(tag = 'div') {
        this.tagName = tag;
        this.children = [];
        this.listeners = {};
        this.attributes = {};
        this.dataset = {};
        this.value = '';
        this.textContent = '';
        this.hidden = false;
        this.disabled = false;
        const tokens = new Set();
        this.classList = {
            add: (...values) => values.forEach(value => tokens.add(value)),
            remove: (...values) => values.forEach(value => tokens.delete(value)),
            contains: value => tokens.has(value),
            toggle: (value, force) => {
                const active = force === undefined ? !tokens.has(value) : force;
                active ? tokens.add(value) : tokens.delete(value);
                return active;
            }
        };
    }
    append(...children) { this.children.push(...children); }
    prepend(child) { this.children.unshift(child); }
    replaceChildren(...children) { this.children = children; }
    setAttribute(key, value) { this.attributes[key] = String(value); }
    removeAttribute(key) { delete this.attributes[key]; }
    hasAttribute(key) { return key in this.attributes; }
    addEventListener(type, fn) { (this.listeners[type] ||= []).push(fn); }
    dispatchEvent(event) {
        for (const fn of this.listeners[event.type] || []) fn(event);
        return !event.defaultPrevented;
    }
    click() { this.dispatchEvent({ type: 'click' }); }
    focus() {}
    scrollIntoView() {}
}
const clientEvent = (type, detail = {}) => ({
    type, detail, defaultPrevented: false,
    preventDefault() { this.defaultPrevented = true; }
});
const flatten = node => [node, ...node.children.flatMap(flatten)];

function checkStaffPos() {
    const form = new Element('form');
    form.dataset = { posForm: 'create', draftUser: '123' };
    const ids = {};
    for (const id of ['selected-table-id', 'selected-booking-id', 'pos-order-config', 'pos-current-target', 'pos-current-table', 'pos-change-target', 'order-target-display', 'section-dinein', 'section-external', 'delivery-address-group', 'table-validation-error', 'cart-items-list', 'empty-cart-message', 'cart-item-count', 'cart-total-qty', 'cart-total-amount', 'btn-submit-order', 'pos-clear-cart', 'dish-category', 'dish-search', 'pos-no-results', 'pos-initial-items']) ids[id] = new Element();
    ids['selected-table-id'].value = '1';
    ids['dish-category'].value = 'all';
    ids['pos-initial-items'].textContent = '[]';
    const targetLabel = new Element('span');
    ids['order-target-display'].querySelector = () => targetLabel;
    const radios = ['TaiBan', 'MangDi', 'GiaoHang'].map(value => Object.assign(new Element('input'), { value, checked: value === 'TaiBan' }));
    const tables = [1, 2].map(id => {
        const button = new Element('button');
        button.dataset = { tableId: String(id), bookingId: String(id * 10 + 1), tableCode: String.fromCharCode(64 + id) };
        return button;
    });
    const menuButton = new Element('button');
    menuButton.dataset = { dishId: '7', sizeId: '9', dishName: 'Món " <img src=x onerror=alert(1)>', sizeName: 'Size " L', price: '10000.5' };
    form.elements = {
        GhiChuDonHang: Object.assign(new Element('input'), { value: 'Ghi chú A' }),
        TenNguoiNhan: new Element('input'), SoDienThoaiNhan: new Element('input'), DiaChiGiaoHang: new Element('input')
    };
    form.querySelector = query => query.startsWith('#') ? ids[query.slice(1)] : query.includes(':checked') ? radios.find(radio => radio.checked) : form.children.find(node => node.dataset.ajaxFeedback !== undefined) || null;
    form.querySelectorAll = query => query === '[data-table-id]' ? tables : query === '.btn-add-dish' ? [menuButton] : query.includes('LoaiDonHang') ? radios : [];
    const stored = new Map();
    const sessionStorage = { getItem: key => stored.get(key) || null, setItem: (key, value) => stored.set(key, value), removeItem: key => stored.delete(key) };
    const document = { getElementById: () => null, querySelector: () => form, createElement: tag => new Element(tag) };
    vm.runInNewContext(fs.readFileSync('RestaurantManagement.Web/wwwroot/js/management-ui.js', 'utf8'), { document, sessionStorage, localStorage: sessionStorage, Map, Number, JSON });
    const count = () => Number(ids['cart-total-qty'].textContent.split(' ')[0]);

    assert.equal(ids['selected-booking-id'].value, '11');
    assert.equal(ids['pos-order-config'].hidden, true);
    assert.equal(form.elements.GhiChuDonHang.value, 'Ghi chú A');
    menuButton.click();
    assert.equal(count(), 1);
    const nodes = flatten(ids['cart-items-list']);
    assert(nodes.some(node => node.textContent === menuButton.dataset.dishName));
    assert(!nodes.some(node => node.tagName === 'img'));
    const note = nodes.find(node => node.name === 'Items[0].YeuCauCheBien');
    assert(note, 'The edited note must be the actual posted field.');
    note.value = 'ít cay " <script>alert(1)</script>';
    note.dispatchEvent(clientEvent('input'));
    assert.deepEqual(JSON.parse(stored.get('pos-draft:123:table:1:booking:11')), [{ sizeId: 9, qty: 1 }]);
    ids['pos-change-target'].click();
    assert.equal(ids['pos-order-config'].hidden, false);
    tables[1].click();
    assert.equal(ids['selected-booking-id'].value, '21');
    assert.equal(count(), 0);
    assert.equal(form.elements.GhiChuDonHang.value, '');
    menuButton.click();
    menuButton.click();
    assert.equal(count(), 2);
    tables[0].click();
    assert.equal(count(), 1);
    assert.equal(form.elements.GhiChuDonHang.value, 'Ghi chú A');
    assert.equal(flatten(ids['cart-items-list']).find(node => node.name === 'Items[0].YeuCauCheBien').value, note.value);
    form.dispatchEvent(clientEvent('ajax:error'));
    assert.equal(count(), 1);
    form.dispatchEvent(clientEvent('ajax:before'));
    menuButton.click();
    const success = clientEvent('ajax:success', { redirectUrl: '/DonHang/Details/1' });
    form.dispatchEvent(success);
    assert.equal(count(), 1);
    assert.equal(success.defaultPrevented, true);
    assert.equal(JSON.parse(stored.get('pos-draft:123:table:1:booking:11'))[0].qty, 1);
    assert.equal(JSON.parse(stored.get('pos-draft:123:table:2:booking:21'))[0].qty, 2);
    console.log('PASS staff POS: booking, table collapse/change, isolated drafts, quoted note binding, DOM escaping, safe storage, failure preservation, unsent additions.');
}

async function checkAjax() {
    class Form extends Element {
        constructor() {
            super('form');
            this.id = 'edit-form';
            this.dataset.ajaxForm = 'true';
            this.action = 'http://localhost/DonHang/Create';
            this.method = 'post';
            const field = Object.assign(new Element('input'), { name: 'GhiChu', value: 'Giữ " ghi chú' });
            const token = Object.assign(new Element('input'), { name: '__RequestVerificationToken', value: 'token' });
            this.elements = [field, token];
            this.elements.__RequestVerificationToken = token;
            this.span = new Element('span');
            this.span.dataset.valmsgFor = 'GhiChu';
            this.button = new Element('button');
            this.password = null;
        }
        querySelector(query) { return query === 'input[type="password"]' ? this.password : this.children.find(node => node.dataset.ajaxFeedback !== undefined) || null; }
        querySelectorAll(query) { return query === '[data-valmsg-for]' ? [this.span] : query.startsWith('button[') ? [this.button] : []; }
        reportValidity() { return true; }
    }
    const file = { name: 'bill.png', type: 'image/png' };
    class FormData {
        constructor(form) { this.entries = form.elements.map(field => [field.name, field.value]); this.entries.push(['Attachment', file]); }
        append(key, value) { this.entries.push([key, value]); }
        forEach(fn) { this.entries.forEach(([key, value]) => fn(value, key)); }
    }
    class CustomEvent {
        constructor(type, config) { this.type = type; Object.assign(this, config); this.defaultPrevented = false; }
        preventDefault() { this.defaultPrevented = true; }
    }
    let listener, next, request, parsedPage;
    const navigations = [];
    const document = { addEventListener: (_, fn) => { listener = fn; }, createElement: tag => new Element(tag) };
    const window = { fetch() {}, FormData, location: { href: 'http://localhost/DonHang/Create', origin: 'http://localhost', assign: url => navigations.push(url) } };
    const fetch = async (url, options) => { request = { url, options }; if (next instanceof Error) throw next; return next; };
    class DOMParser { parseFromString() { return parsedPage; } }
    vm.runInNewContext(fs.readFileSync('RestaurantManagement.Web/wwwroot/js/ajax-forms.js', 'utf8'), { window, document, HTMLFormElement: Form, CustomEvent, FormData, DOMParser, URL, fetch });
    const response = (data, ok = true, status = 200) => ({ url: 'http://localhost/DonHang/Create', ok, status, redirected: false, headers: { get: () => 'application/json' }, json: async () => data });
    const submit = form => {
        const e = { target: form, defaultPrevented: false, submitter: Object.assign(new Element('button'), { name: 'save', value: 'yes', formAction: 'http://localhost/wrong', formMethod: 'get' }), preventDefault() { this.defaultPrevented = true; } };
        listener(e);
        return e;
    };
    const settle = () => new Promise(resolve => setImmediate(resolve));
    const form = new Form();
    let errors = 0, successes = 0;
    form.addEventListener('ajax:error', () => { errors++; });
    next = response({ ok: false, errors: { GhiChu: ['Ghi chú chưa hợp lệ.'] } }, false, 400);
    assert.equal(submit(form).defaultPrevented, true);
    await settle();
    assert.equal(request.url, 'http://localhost/DonHang/Create');
    assert.equal(request.options.method, 'POST');
    assert.equal(request.options.credentials, 'same-origin');
    assert.equal(request.options.headers['X-Requested-With'], 'XMLHttpRequest');
    assert(request.options.body.entries.some(([key, value]) => key === '__RequestVerificationToken' && value === 'token'));
    assert(request.options.body.entries.some(([key, value]) => key === 'Attachment' && value === file));
    assert.equal(form.elements[0].value, 'Giữ " ghi chú');
    assert.equal(form.span.textContent, 'Ghi chú chưa hợp lệ.');
    assert.equal(errors, 1);
    assert.equal(form.button.disabled, false);
    assert.equal(form.dataset.ajaxPending, undefined);
    form.addEventListener('ajax:success', e => { successes++; assert.equal(e.detail.redirectUrl, 'http://localhost/DonHang/Details/9'); e.preventDefault(); });
    next = response({ ok: true, redirectUrl: '/DonHang/Details/9' });
    submit(form);
    await settle();
    assert.equal(successes, 1);
    assert.equal(navigations.length, 0);
    next = new Error('offline');
    submit(form);
    await settle();
    assert.equal(errors, 2);
    assert.equal(form.elements[0].value, 'Giữ " ghi chú');
    const htmlField = new Element('span');
    htmlField.dataset.valmsgFor = 'GhiChu';
    htmlField.textContent = 'Lỗi MVC';
    parsedPage = { forms: [], querySelector: () => null, querySelectorAll: query => query === '[data-valmsg-for]' ? [htmlField] : [] };
    next = { url: form.action, ok: true, status: 200, redirected: false, headers: { get: () => 'text/html' }, text: async () => '<html>invalid</html>' };
    submit(form);
    await settle();
    assert.equal(errors, 3);
    assert.equal(form.span.textContent, 'Lỗi MVC');
    assert.equal(form.elements[0].value, 'Giữ " ghi chú');
    parsedPage = { forms: [], querySelector: () => new Element('input'), querySelectorAll: () => [] };
    next = { url: 'http://localhost/admin?ReturnUrl=%2FDonHang%2FCreate', ok: true, status: 200, redirected: true, headers: { get: () => 'text/html' }, text: async () => '<html>login</html>' };
    let expired;
    form.addEventListener('ajax:error', e => { expired = e.detail.expired; });
    submit(form);
    await settle();
    assert.equal(expired, true);
    assert.equal(navigations.length, 0);
    assert.equal(form.elements[0].value, 'Giữ " ghi chú');
    const login = new Form();
    login.password = new Element('input');
    assert.equal(submit(login).defaultPrevented, false);
    const deletion = new Form();
    deletion.action = 'http://localhost/MonAn/Delete/5';
    assert.equal(submit(deletion).defaultPrevented, false);
    console.log('PASS AJAX: form target/method, CSRF/files, JSON/MVC validation, cancelable success, network/expired-cookie failures, native protected forms.');
}

checkStaffPos();
checkAjax().catch(error => { console.error(error); process.exitCode = 1; });
