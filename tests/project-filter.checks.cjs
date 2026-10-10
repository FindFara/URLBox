const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const path = require('node:path');

function element(dataset = {}) {
    return { dataset, hidden: false, attributes: {}, handlers: {},
        classList: { values: new Set(), toggle(name, active) { if (active) this.values.add(name); else this.values.delete(name); } },
        setAttribute(name, value) { this.attributes[name] = value; },
        addEventListener(name, action) { this.handlers[name] = action; },
        click() { this.handlers.click(); },
        closest() { return null; }
    };
}
const tags = ['API', 'Docs', 'Empty'].map(projectFilter => element({ projectFilter }));
const all = element();
const empty = element();
empty.hidden = true;
const entries = [['API'], ['Docs'], ['API', 'Docs'], ['Other']].map(projects => element({ projects: JSON.stringify(projects) }));
const document = {
    addEventListener() {},
    querySelector(selector) { return selector === '[data-project-all]' ? all : null; },
    querySelectorAll(selector) { return selector === '[data-project-filter]' ? tags : entries; },
    getElementById(id) { return id === 'projectFilterEmpty' ? empty : null; }
};
vm.runInNewContext(fs.readFileSync(path.join(__dirname, '../src/URLBox.Presentation/wwwroot/js/site.js'), 'utf8'), { document });
const visible = () => entries.map(e => !e.hidden);
tags[0].click();
assert.deepEqual(visible(), [true, false, true, false]);
assert.equal(tags[0].attributes['aria-pressed'], 'true');
tags[1].click();
assert.deepEqual(visible(), [true, true, true, false]);
tags[0].click();
assert.deepEqual(visible(), [false, true, true, false]);
assert.equal(tags[0].attributes['aria-pressed'], 'false');
all.click();
assert.deepEqual(visible(), [true, true, true, true]);
assert.equal(all.attributes['aria-pressed'], 'true');
tags[2].click();
assert.deepEqual(visible(), [false, false, false, false]);
assert.equal(empty.hidden, false);
tags[2].click();
assert.deepEqual(visible(), [true, true, true, true]);
assert.equal(empty.hidden, true);
console.log('Passed project tag filtering checks: select, multiple projects, deselect, All, empty result, and clearing the last tag.');
