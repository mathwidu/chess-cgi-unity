const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const source = fs.readFileSync(path.resolve(__dirname,
  '../../game/Assets/Plugins/WebGL/ChessWebSession.jslib'), 'utf8');
const key = 'cicochess.match.v1';

// Exercise the real jslib with a bounded UTF-8 heap and a fake browser surface.
// These tests verify bridge behavior; C# validates the JSON schema and replay.
function load(initial = {}, options = {}, sharedValues) {
  const values = sharedValues || new Map(Object.entries(initial));
  const calls = [];
  const events = [];
  const heap = new Map();
  const allocations = [];
  let address = 1;
  function malloc(size) {
    const pointer = address++;
    heap.set(pointer, Buffer.alloc(size));
    allocations.push(size);
    return pointer;
  }
  function write(value, pointer, size) {
    const bytes = Buffer.from(value, 'utf8');
    const buffer = heap.get(pointer);
    assert.equal(buffer.length, size);
    assert.ok(bytes.length + 1 <= size, 'UTF-8 payload and NUL must fit the allocation.');
    bytes.copy(buffer);
    buffer[bytes.length] = 0;
  }
  function decode(pointer) {
    const buffer = heap.get(pointer);
    assert.ok(buffer, 'Bridge must use a valid UTF-8 pointer.');
    const terminator = buffer.indexOf(0);
    assert.notEqual(terminator, -1);
    return buffer.toString('utf8', 0, terminator);
  }
  function encode(value) {
    const size = Buffer.byteLength(value, 'utf8') + 1;
    const pointer = malloc(size);
    write(value, pointer, size);
    return pointer;
  }
  const localStorage = {
    getItem(name) {
      calls.push(['getItem', name]);
      if (options.readError) throw options.readError;
      return values.has(name) ? values.get(name) : null;
    },
    setItem(name, value) {
      calls.push(['setItem', name, value]);
      // Model localStorage's atomic failure: do not replace the previous bytes.
      if (options.writeError) throw options.writeError;
      values.set(name, value);
    },
    removeItem(name) { calls.push(['removeItem', name]); assert.fail('Bridge must not remove a save.'); },
    clear() { calls.push(['clear']); assert.fail('Bridge must not clear origin storage.'); }
  };
  const window = { dispatchEvent(event) { events.push(event); return true; } };
  Object.defineProperty(window, 'localStorage', {
    get() {
      if (options.accessError) throw options.accessError;
      return localStorage;
    }
  });
  class CustomEvent {
    constructor(type, init) { this.type = type; this.detail = init.detail; }
  }
  const library = {};
  const context = vm.createContext({
    LibraryManager: { library }, mergeInto: Object.assign, window, CustomEvent,
    UTF8ToString: decode, lengthBytesUTF8: value => Buffer.byteLength(value, 'utf8'),
    _malloc: malloc, stringToUTF8: write
  });
  vm.runInContext(source, context);
  context.ChessSessionStorage = library.$ChessSessionStorage;
  return {
    values, calls, allocations, api: library,
    read() { return JSON.parse(decode(library.ChessSession_Read(encode(key)))); },
    write(value) { return library.ChessSession_Write(encode(key), encode(value)); },
    notify(name, ok, exists, reason) {
      library.ChessSession_Notify(encode(name), ok, exists, encode(reason));
      // Normalize cross-realm prototypes while retaining exactly the payload.
      return JSON.parse(JSON.stringify(events.at(-1)));
    }
  };
}

test('missing key reads as absent without writing, removing or creating it', () => {
  const bridge = load({ 'unrelated.preference': 'keep' });
  assert.deepEqual(bridge.read(), { ok: true, hasSave: false, reason: '', value: '' });
  assert.deepEqual(bridge.calls, [['getItem', key]]);
  assert.equal(bridge.values.has(key), false);
  assert.equal(bridge.values.get('unrelated.preference'), 'keep');
});

test('an existing empty save is preserved and distinguished from a missing key', () => {
  const bridge = load({ [key]: '' });
  assert.deepEqual(bridge.read(), { ok: true, hasSave: true, reason: '', value: '' });
  assert.equal(bridge.values.get(key), '');
  assert.deepEqual(bridge.calls, [['getItem', key]]);
});

test('oversize read is bounded without deleting or truncating the stored save', () => {
  const value = 'x'.repeat(65537);
  const bridge = load({ [key]: value });
  assert.deepEqual(bridge.read(), { ok: false, hasSave: true, reason: 'save-too-large', value: '' });
  assert.equal(bridge.values.get(key), value);
  assert.ok(bridge.allocations.at(-1) < 256, 'Oversize contents must not cross into the C# heap.');
  assert.deepEqual(bridge.calls, [['getItem', key]]);
  const boundary = load({ [key]: 'x'.repeat(65536) });
  assert.equal(boundary.read().value.length, 65536, 'The exact bridge character limit is inclusive.');
});

test('corrupt content and Unicode cross the bridge unchanged for C# validation', () => {
  const corrupt = '{"version":99,"moves":["♞",\n"e2e4"]';
  const bridge = load({ [key]: corrupt });
  assert.deepEqual(bridge.read(), { ok: true, hasSave: true, reason: '', value: corrupt });
  assert.equal(bridge.values.get(key), corrupt);
  assert.deepEqual(bridge.calls, [['getItem', key]]);
});

test('denied localStorage access returns a sanitized failure for both read and write', () => {
  const secretError = Object.assign(new Error('Private browser configuration must not leak'), { name: 'SecurityError' });
  const bridge = load({ [key]: 'previous' }, { accessError: secretError });
  assert.deepEqual(bridge.read(), { ok: false, hasSave: false, reason: 'storage-unavailable', value: '' });
  assert.equal(bridge.write('next'), 2);
  assert.equal(bridge.values.get(key), 'previous');
  assert.deepEqual(bridge.calls, []);
});

test('storage method failures are caught and never remove the previous save', () => {
  const failure = new Error('Storage backend unavailable');
  const bridge = load({ [key]: 'previous' }, { readError: failure, writeError: failure });
  assert.deepEqual(bridge.read(), { ok: false, hasSave: false, reason: 'storage-unavailable', value: '' });
  assert.equal(bridge.write('next'), 2);
  assert.equal(bridge.values.get(key), 'previous');
  assert.deepEqual(bridge.calls.map(call => call[0]), ['getItem', 'getItem']);
});

for (const name of ['QuotaExceededError', 'NS_ERROR_DOM_QUOTA_REACHED']) {
  test(`${name} returns quota status and preserves the last complete value`, () => {
    const options = {
      writeError: Object.assign(new Error('Quota detail is private'), { name })
    };
    const bridge = load({ [key]: 'last consistent save', unrelated: 'keep' }, options);
    bridge.read();
    assert.equal(bridge.write('replacement'), 1);
    assert.equal(bridge.values.get(key), 'last consistent save');
    assert.equal(bridge.values.get('unrelated'), 'keep');
    assert.deepEqual(bridge.calls, [['getItem', key], ['getItem', key], ['setItem', key, 'replacement']]);
    delete options.writeError;
    assert.equal(bridge.write('retry'), 0, 'A quota failure must not advance the expected baseline.');
    assert.equal(bridge.values.get(key), 'retry');
  });
}

test('successful write replaces only the dedicated key and round-trips exact bytes', () => {
  const value = '{"version":1,"mode":"local","humanSide":"white","difficulty":"beginner","moves":["e2e4"]}';
  const bridge = load({ [key]: 'old', unrelated: 'keep' });
  bridge.read();
  assert.equal(bridge.write(value), 0);
  assert.equal(bridge.values.get(key), value);
  assert.equal(bridge.values.get('unrelated'), 'keep');
  assert.deepEqual(bridge.read(), { ok: true, hasSave: true, reason: '', value });
});

test('read and write declare their Emscripten dependency on per-instance observation memory', () => {
  const bridge = load();
  assert.deepEqual(Array.from(bridge.api.ChessSession_Read__deps), ['$ChessSessionStorage']);
  assert.deepEqual(Array.from(bridge.api.ChessSession_Write__deps), ['$ChessSessionStorage']);
});

test('a stale tab cannot overwrite a newer tab until it explicitly reads that save', () => {
  const tabA = load({ [key]: 'initial' });
  const tabB = load({}, {}, tabA.values);
  tabA.read();
  tabB.read();
  assert.equal(tabB.write('newer from B'), 0);
  assert.equal(tabA.write('stale from A'), 3);
  assert.equal(tabA.values.get(key), 'newer from B');
  assert.equal(tabA.write('another stale attempt'), 3, 'Conflict must not silently advance the baseline.');
  assert.equal(tabA.read().value, 'newer from B');
  assert.equal(tabA.write('new from A after reading B'), 0);
  assert.equal(tabA.values.get(key), 'new from A after reading B');
  assert.equal(tabB.write('stale exit from B'), 3, 'Successful writes must advance only their own tab baseline.');
});

test('writing the exact already stored JSON is a no-op and establishes its baseline', () => {
  const options = { writeError: new Error('A no-op must not call setItem') };
  const bridge = load({ [key]: 'same JSON' }, options);
  assert.equal(bridge.write('same JSON'), 0);
  assert.deepEqual(bridge.calls, [['getItem', key]]);
  delete options.writeError;
  assert.equal(bridge.write('next JSON'), 0);
  assert.equal(bridge.values.get(key), 'next JSON');
});

test('null and empty are distinct comparison baselines and unknown baselines fail closed', () => {
  const absent = load();
  assert.equal(absent.write('new without reading'), 3);
  absent.read();
  absent.values.set(key, '');
  assert.equal(absent.write('stale after missing key'), 3);
  assert.equal(absent.values.get(key), '');
  const empty = load({ [key]: '' });
  empty.read();
  empty.values.delete(key);
  assert.equal(empty.write('stale after empty key'), 3);
  assert.equal(empty.values.has(key), false);
  assert.equal(empty.read().hasSave, false);
  assert.equal(empty.write('fresh after reading absent key'), 0);
});

test('a failed read does not accept an unseen newer value as the comparison baseline', () => {
  const options = {};
  const bridge = load({ [key]: 'initial' }, options);
  bridge.read();
  bridge.values.set(key, 'newer elsewhere');
  options.readError = new Error('Transient denied read');
  assert.equal(bridge.read().ok, false);
  delete options.readError;
  assert.equal(bridge.write('stale local'), 3);
  assert.equal(bridge.values.get(key), 'newer elsewhere');
});

test('window event payloads expose only status and reason, with resume omitting hasSave', () => {
  const bridge = load();
  assert.deepEqual(bridge.notify('cicochess:session-ready', 1, 1, 'invalid-save'), {
    type: 'cicochess:session-ready', detail: { ok: true, hasSave: true, reason: 'invalid-save' }
  });
  assert.deepEqual(bridge.notify('cicochess:save', 0, 1, 'storage-full'), {
    type: 'cicochess:save', detail: { ok: false, hasSave: true, reason: 'storage-full' }
  });
  assert.deepEqual(bridge.notify('cicochess:resume', 1, 1, ''), {
    type: 'cicochess:resume', detail: { ok: true, reason: '' }
  });
  assert.deepEqual(bridge.notify('cicochess:resume', 0, 0, 'illegal-move'), {
    type: 'cicochess:resume', detail: { ok: false, reason: 'illegal-move' }
  });
  assert.deepEqual(bridge.calls, [], 'Notifications do not read or mutate browser storage.');
});
