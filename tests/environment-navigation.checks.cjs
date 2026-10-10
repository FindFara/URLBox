const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../src/URLBox.Presentation/wwwroot/js/environment-navigation.js'), 'utf8');

for (const count of [0, 1, 5, 6, 10, 12]) {
    const columns = Array.from({ length: count }, () => ({ hidden: false }));
    const button = () => ({ disabled: false, addEventListener(_, action) { this.click = action; } });
    const previous = button();
    const next = button();
    let updates = 0;
    const document = {
        querySelector(selector) { return selector === '[data-environment-previous]' ? previous : next; },
        querySelectorAll() { return columns; },
        dispatchEvent(event) { assert.equal(event.type, 'environment-page-change'); updates++; }
    };
    vm.runInNewContext(source, { document, Event: class { constructor(type) { this.type = type; } } });
    const visible = () => columns.flatMap((c, i) => c.hidden ? [] : [i]);
    const pages = Math.max(1, Math.ceil(count / 5));
    const expected = page => Array.from({ length: Math.max(0, Math.min(5, count - page * 5)) }, (_, i) => page * 5 + i);
    assert.deepEqual(visible(), expected(0));
    assert.equal(previous.disabled, true);
    previous.click();
    assert.deepEqual(visible(), expected(0));
    for (let page = 1; page < pages; page++) {
        next.click();
        assert.deepEqual(visible(), expected(page));
        assert.equal(previous.disabled, false);
    }
    assert.equal(next.disabled, true);
    next.click();
    assert.deepEqual(visible(), expected(pages - 1));
    for (let page = pages - 2; page >= 0; page--) {
        previous.click();
        assert.deepEqual(visible(), expected(page));
    }
    assert.equal(previous.disabled, true);
    assert.ok(updates > 0);
}
vm.runInNewContext(source, { document: { querySelector() { return null; } } });
console.log('Passed environment navigation checks: five-column pages, next/previous, partial final page, boundaries, and absent controls.');
