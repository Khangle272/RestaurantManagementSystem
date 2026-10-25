const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

// Small DOM stub verifies time feedback; it is not a browser end-to-end test.
const element = (value = '') => ({ value, dataset: {}, listeners: {}, classList: { toggle() {} },
    addEventListener(name, callback) { this.listeners[name] = callback; },
    setAttribute() {}, setCustomValidity(message) { this.validationMessage = message; },
    replaceChildren() {}, append() {}, reportValidity() {} });
const day = element('2026-10-25'), arrival = element(), guests = element('4');
const status = element(), suggestions = element(), error = element();
const fields = { '#booking-day': day, '#inputThoiGianDen': arrival,
    '[name="SoNguoi"]': guests, '#slot-status': status, '[data-time-suggestions]': suggestions,
    '[data-time-error]': error };
const form = element();
form.dataset = { slotUrl: '/slots', serverNow: '2026-10-25T13:06:00Z',
    leadMinutes: '30', openTime: '11:00', closeTime: '22:00' };
form.querySelector = selector => fields[selector];
const script = fs.readFileSync(path.resolve(__dirname, '../../RestaurantManagement.Web/wwwroot/js/reservation-slots.js'), 'utf8');
vm.runInNewContext(script, { document: { querySelector: () => form }, Date, URL, AbortController,
    setTimeout, clearTimeout, location: { origin: 'https://localhost' },
    fetch: async () => ({ ok: true, headers: { get: () => 'application/json' },
        json: async () => ({ slots: [], available: false, message: 'Choose time' }) }) });

assert.equal(arrival.value, '', 'The form must not silently choose a time for the guest');
assert.equal(arrival.validationMessage, '');
function input(value) { arrival.value = value; arrival.listeners.input(); }
input('2026-10-25T20:00');
assert.match(error.textContent, /30 phút/, 'Past/too-soon time shows feedback before submitting');
assert.equal(error.hidden, false);
input('2026-10-25T20:45');
assert.equal(arrival.validationMessage, '');
assert.equal(error.hidden, true);
input('2026-10-26T16:07');
assert.equal(day.value, '2026-10-26');
assert.equal(arrival.validationMessage, '', '16:07 remains valid outside shift shortcuts');
input('2026-10-26T22:00');
assert.match(error.textContent, /đóng cửa/);
input('2026-10-26T10:59');
assert.match(error.textContent, /đóng cửa/);
input('2027-05-01T12:00');
assert.match(error.textContent, /180 ngày/);
input('');
day.value = '2026-10-27'; day.listeners.change();
assert.equal(arrival.value, '', 'Changing date does not invent an arrival time');
console.log('PASS: reservation input validation checks (DOM stub, not browser E2E).');
