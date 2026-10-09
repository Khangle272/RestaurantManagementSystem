// Run: node tests/Management.SmokeTests/customer-cart.check.cjs
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { webcrypto } = require('node:crypto');
const script = fs.readFileSync(path.join(__dirname, '../../RestaurantManagement.Web/wwwroot/js/customer-cart.js'), 'utf8');
class Element {
    constructor(tag = 'div') { this.tagName = tag; this.children = []; this.listeners = {}; this.dataset = {}; this.hidden = false; this.value = ''; this.checked = false; const classes = new Set(); this.classList = { add: value => classes.add(value), remove: value => classes.delete(value), contains: value => classes.has(value) }; }
    append(...nodes) { this.children.push(...nodes); }
    replaceChildren(...nodes) { this.children = [...nodes]; }
    setAttribute(name, value) { this[name] = value; }
    addEventListener(name, callback) { (this.listeners[name] ||= []).push(callback); }
    querySelector(selector) { return this.queries?.[selector] || null; }
    async fire(name, detail = {}) {
        const event = { detail, defaultPrevented: false, preventDefault() { this.defaultPrevented = true; }, stopImmediatePropagation() { this.stopped = true; } };
        for (const callback of this.listeners[name] || []) { await callback(event); if (event.stopped) break; }
        return event;
    }
}
const walk = node => [node, ...node.children.flatMap(walk)];
const settle = () => new Promise(resolve => setImmediate(resolve));
async function start({ search = '?cart=new', draft, context, owner = '7', store = new Map() } = {}) {
    const ids = new Map(), element = id => { if (!ids.has(id)) { const node = new Element(); node.id = id; ids.set(id, node); } return ids.get(id); };
    ['customer-cart', 'cart-context', 'cart-count', 'cart-total', 'cart-version', 'cart-request-id', 'cart-prepare', 'cart-items', 'cart-errors', 'cart-save-form', 'cart-save-button', 'cart-description', 'cart-new-checkout', 'cart-details', 'cart-switch-new', 'cartDrawer', 'dishModal', 'cart-dish-form', 'dish-title', 'dish-image', 'dish-description', 'dish-extras', 'dish-size', 'dish-quantity', 'dish-note', 'dish-error', 'dish-add', 'cart-feedback', 'formDatBan', 'booking-cart-summary', 'booking-cart-total', 'booking-prepare', 'cart-reload'].forEach(element);
    element('customer-cart').dataset = { owner, dishUrl: '/Home/CartDish', contextUrl: '/DatBan/CartContext', bookingUrl: '/DatBan', saveUrl: '/DatBan/SavePreorder', detailUrl: '/DatBan/ChiTiet' };
    const hidden = [new Element(), new Element()], createRequest = new Element(), createSubmit = new Element(), choose = new Element('button');
    choose.dataset.cartDish = '1';
    element('formDatBan').queries = { '[name="RequestId"]': createRequest, '[type="submit"]': createSubmit };
    const storage = { getItem: key => store.get(key) || null, setItem: (key, value) => store.set(key, value), removeItem: key => store.delete(key) };
    const bookingId = new URLSearchParams(search).get('datBanId');
    const key = `customer-cart:v1:${owner}:${bookingId || 'new'}`;
    if (draft) store.set(key, JSON.stringify(draft));
    const dish = { id: 1, name: 'Món thử', description: 'Mô tả', image: '/images/test.jpg', canOrder: true, sizes: [{ id: 11, name: 'Nhỏ', price: 125000 }], comboItems: [], promotions: [] };
    const document = { getElementById: id => ids.get(id) || null, createElement: tag => new Element(tag), querySelectorAll: selector => selector === '[data-cart-inputs]' ? hidden : selector === '[data-cart-dish]' ? [choose] : [] };
    let drawerOpens = 0;
    vm.runInNewContext(script, { document, localStorage: storage, sessionStorage: storage, location: { search, origin: 'http://localhost', href: 'http://localhost/Home' + search }, URLSearchParams, URL, Intl, crypto: webcrypto, confirm: () => true, setTimeout: () => 1, clearTimeout: () => {}, bootstrap: { Modal: { getOrCreateInstance: () => ({ show() {}, hide() {} }) }, Offcanvas: { getOrCreateInstance: () => ({ show() { drawerOpens++; } }) } }, fetch: async url => ({ ok: true, headers: { get: () => 'application/json' }, json: async () => url.includes('CartContext') ? context : dish }) });
    await settle(); await settle();
    return { ids, element, hidden, store, key, createRequest, createSubmit, choose, drawerOpens: () => drawerOpens };
}
(async () => {
    const note = 'Ít cay, "không hành" <>& \n ghi chú';
    const requestId = '12345678-1234-4123-8123-123456789abc';
    const cart = await start({ draft: { requestId, items: [{ monAnId: 1, sizeId: 11, quantity: 2, note, price: 1, password: 'must-not-persist' }, { monAnId: 1, sizeId: 11, quantity: -1, note: '' }] } });
    let saved = JSON.parse(cart.store.get(cart.key));
    assert.equal(saved.items.length, 1, 'Invalid quantity must be discarded');
    assert.equal(saved.items[0].note, note, 'Quoted notes must survive storage');
    assert.equal(saved.items[0].price, undefined, 'Client price must never persist');
    assert.equal(saved.items[0].password, undefined, 'Unrelated personal input must never persist');
    assert.equal(cart.createRequest.value, requestId);
    const fields = Object.fromEntries(cart.hidden[1].children.map(x => [x.name, x.value]));
    assert.equal(fields['Items[0].MonAnSizeId'], '11');
    assert.equal(fields['Items[0].SoLuong'], '2');
    assert.equal(fields['Items[0].YeuCauCheBien'], note, 'Posted notes are text values, not HTML');
    assert.equal(cart.element('booking-cart-total').textContent.replace(/\s/g, ''), '250.000₫');
    const rowNote = walk(cart.element('cart-items')).find(x => x.tagName === 'textarea');
    rowNote.value = 'Không đá "ít ngọt"'; await rowNote.fire('input');
    saved = JSON.parse(cart.store.get(cart.key));
    assert.equal(saved.items[0].note, rowNote.value);
    assert.notEqual(saved.requestId, requestId, 'Editing a draft needs a new request identifier');
    const editedRequest = saved.requestId;
    await cart.element('formDatBan').fire('submit');
    await cart.element('formDatBan').fire('ajax:before');
    await cart.element('formDatBan').fire('ajax:error', { message: 'Thử lại', response: { status: 500 } });
    await cart.element('formDatBan').fire('ajax:complete');
    assert.equal(JSON.parse(cart.store.get(cart.key)).requestId, editedRequest, 'Failed requests keep their identifier and draft');
    cart.store.set('customer-cart:v1:7:other', 'kept');
    await cart.element('formDatBan').fire('ajax:success', { data: { ok: true }, redirectUrl: '/DatBan/Success/1' });
    assert.equal(cart.store.has(cart.key), false, 'Only successful create clears its new cart');
    assert.equal(cart.store.get('customer-cart:v1:7:other'), 'kept', 'Other booking drafts must remain scoped');

    const modal = await start();
    await modal.choose.fire('click');
    modal.element('dish-size').value = '11'; modal.element('dish-quantity').value = '2'; modal.element('dish-note').value = note;
    await modal.element('cart-dish-form').fire('submit');
    assert.equal(JSON.parse(modal.store.get(modal.key)).items[0].sizeId, 11, 'Modal must use the real size identifier');
    assert.equal(JSON.parse(modal.store.get(modal.key)).items[0].note, note.trim());
    assert.equal(modal.element('dish-title').textContent, 'Món thử');
    assert.equal(modal.element('dish-size').children[0].textContent, 'Nhỏ — 125.000 ₫');
    assert.equal(modal.drawerOpens(), 0, 'Adding a dish must not interrupt browsing by opening the cart');
    assert.match(modal.element('cart-feedback').textContent, /Đã thêm 2 phần/);

    const guest = await start({ owner: 'guest', draft: { requestId, items: [{ monAnId: 1, sizeId: 11, quantity: 2, note }] } });
    const loggedIn = await start({ store: guest.store, draft: { items: [] } });
    assert.equal(loggedIn.element('cart-count').textContent, 2, 'An empty old account cart must not hide dishes selected before login');
    assert.equal(JSON.parse(loggedIn.store.get(loggedIn.key)).items[0].note, note);
    assert.equal(loggedIn.store.has('customer-cart:v1:guest:new'), false, 'Transferred guest drafts must not leak into another account');

    const existing = await start({ search: '?datBanId=42', context: { bookingId: 42, code: 'BK-42', version: 'v2', canEdit: true, chuanBiTruoc: true, items: [{ monAnId: 1, sizeId: 11, name: 'Đã gửi', sizeName: 'Nhỏ', price: 125000, quantity: 1, note: 'Giữ nguyên', sent: true }, { monAnId: 1, sizeId: 11, quantity: 3, note: 'Chưa gửi', sent: false }] } });
    assert.equal(walk(existing.element('cart-items')).filter(x => x.tagName === 'textarea').length, 1, 'Sent rows must be read-only');
    assert.equal(existing.element('cart-save-form').action, '/DatBan/SavePreorder/42');
    assert.equal(existing.hidden[0].children.length, 3, 'Only unsent rows can be posted');
    assert.equal(existing.element('cart-request-id').value, JSON.parse(existing.store.get(existing.key)).requestId);
    assert.equal(existing.element('cart-prepare').checked, true);
    assert.equal(existing.createSubmit.disabled, true, 'An existing booking context cannot create another booking');
    assert.equal((await existing.element('formDatBan').fire('submit')).defaultPrevented, true);

    const stale = await start({ search: '?datBanId=42', draft: { version: 'v1', requestId, items: [{ monAnId: 1, sizeId: 11, quantity: 4, note }] }, context: { bookingId: 42, code: 'BK-42', version: 'v2', canEdit: true, items: [] } });
    assert.equal(stale.element('cart-save-button').disabled, true, 'Stale drafts must be reviewed before resubmission');
    assert.equal(JSON.parse(stale.store.get(stale.key)).items[0].quantity, 4, 'Concurrency failures keep the local draft');
    await stale.element('cart-reload').fire('click');
    assert.equal(JSON.parse(stale.store.get(stale.key)).items.length, 0, 'Explicit reload adopts only current unsent rows');
    assert.equal(stale.element('cart-save-button').disabled, false);
    console.log('PASS: cart notes, quantities, safe scoped drafts, checkout bindings, failure retry, sent rows, and stale-version reload.');
})().catch(error => { console.error(error); process.exitCode = 1; });
