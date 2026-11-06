// Hiển thị ngày giờ theo giờ Việt Nam (UTC+7) bất kể múi giờ của máy người xem:
// vnTime.date → "dd/MM/yyyy", vnTime.dateTime → "dd/MM/yyyy HH:mm", vnTime.time → "HH:mm:ss".
(() => {
    'use strict';
    const offset = 7 * 3600000;
    const pad = value => String(value).padStart(2, '0');
    const parts = value => {
        const ms = value === undefined ? Date.now() : new Date(value).getTime();
        if (Number.isNaN(ms)) return null;
        const d = new Date(ms + offset);
        return { y: d.getUTCFullYear(), M: pad(d.getUTCMonth() + 1), d: pad(d.getUTCDate()), H: pad(d.getUTCHours()), m: pad(d.getUTCMinutes()), s: pad(d.getUTCSeconds()), month: d.getUTCMonth(), day: d.getUTCDate() };
    };
    window.vnTime = {
        parts,
        date: value => { const p = parts(value); return p ? `${p.d}/${p.M}/${p.y}` : ''; },
        dateTime: value => { const p = parts(value); return p ? `${p.d}/${p.M}/${p.y} ${p.H}:${p.m}` : ''; },
        time: value => { const p = parts(value); return p ? `${p.H}:${p.m}:${p.s}` : ''; }
    };
})();
