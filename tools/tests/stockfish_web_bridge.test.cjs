const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const source = fs.readFileSync(path.resolve(__dirname, '../../game/Assets/Plugins/WebGL/Stockfish.jslib'), 'utf8');
const startFen = 'rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1';

function load(WorkerClass) {
  const workers = [];
  class FakeWorker {
    constructor(url) { this.url = url; this.sent = []; workers.push(this); }
    postMessage(line) { this.sent.push(line); }
    terminate() { this.terminated = true; }
    emit(line) { this.onmessage({ data: line }); }
  }
  const library = {};
  const context = vm.createContext({
    LibraryManager: { library }, mergeInto: Object.assign,
    Worker: WorkerClass || FakeWorker, URL, document: { baseURI: 'http://localhost:8765/chess/index.html' },
    UTF8ToString: value => value, lengthBytesUTF8: value => Buffer.byteLength(value),
    _malloc: () => 1, stringToUTF8: value => { context.lastMessage = value; }
  });
  vm.runInContext(source, context);
  context.ChessStockfish = library.$ChessStockfish;
  return {
    api: library, channels: context.ChessStockfish.channels, workers,
    take(handle) {
      return library.ChessStockfish_TakeMessage(handle) ? JSON.parse(context.lastMessage) : null;
    }
  };
}

test('startup handshake precedes search and preserves history and difficulty', () => {
  const { api, workers, take } = load();
  const id = api.ChessStockfish_Create();
  api.ChessStockfish_Begin(id, 42, startFen, 'e2e4 e7e5', 14, 1200);
  const worker = workers[0];
  assert.equal(worker.url, 'http://localhost:8765/chess/stockfish/stockfish-18-lite-single.js');
  assert.deepEqual(worker.sent, ['uci']);
  worker.emit('uciok');
  assert.deepEqual(worker.sent.slice(1), [
    'setoption name Hash value 16', 'setoption name Ponder value false',
    'setoption name UCI_LimitStrength value false', 'ucinewgame',
    'setoption name Skill Level value 14', 'isready'
  ]);
  worker.emit('readyok');
  assert.equal(worker.sent.at(-2), 'position fen ' + startFen + ' moves e2e4 e7e5');
  assert.equal(worker.sent.at(-1), 'go movetime 1200');
  assert.deepEqual(take(id), { requestId: 42, type: 'searching', value: '' });
  for (let n = 0; n < 10000; n++) worker.emit('info depth 1 score cp 0');
  assert.equal(take(id), null);
  worker.emit('bestmove g1f3 ponder b8c6');
  assert.deepEqual(take(id), { requestId: 42, type: 'bestmove', value: 'g1f3' });
  api.ChessStockfish_Begin(id, 43, startFen, '', 0, 150);
  assert.equal(workers.length, 1, 'Keep the initialized worker between turns.');
  assert.equal(worker.sent.at(-2), 'setoption name Skill Level value 0');
  api.ChessStockfish_Dispose(id);
});

test('cancel terminates worker, clears messages and discards detached old callbacks', () => {
  const { api, workers, take } = load();
  const id = api.ChessStockfish_Create();
  api.ChessStockfish_Begin(id, 1, startFen, '', 0, 150);
  const old = workers[0];
  const delayedMessage = old.onmessage;
  api.ChessStockfish_Cancel(id);
  assert.equal(old.terminated, true);
  api.ChessStockfish_Begin(id, 2, startFen, '', 0, 150);
  delayedMessage({ data: 'bestmove d2d4' });
  assert.equal(take(id), null);
  const current = workers[1];
  current.emit('uciok'); current.emit('readyok'); current.emit('bestmove e2e4');
  assert.equal(take(id).requestId, 2);
  assert.equal(take(id).value, 'e2e4');
  api.ChessStockfish_Dispose(id);
});

test('worker download/runtime errors retain readable request-specific errors and allow retry', () => {
  const { api, workers, take } = load();
  const id = api.ChessStockfish_Create();
  api.ChessStockfish_Begin(id, 8, startFen, '', 6, 500);
  workers[0].onerror({ preventDefault() {} });
  assert.equal(workers[0].terminated, true);
  const error = take(id);
  assert.equal(error.requestId, 8);
  assert.equal(error.type, 'error');
  assert.match(error.value, /carregar a IA web/);
  api.ChessStockfish_Begin(id, 9, startFen, '', 6, 500);
  assert.equal(workers.length, 2);
  api.ChessStockfish_Dispose(id);
  api.ChessStockfish_Dispose(id);
});

test('unsupported Worker fails through the same error path', () => {
  class UnsupportedWorker { constructor() { throw new Error('Unsupported'); } }
  const { api, take } = load(UnsupportedWorker);
  const id = api.ChessStockfish_Create();
  api.ChessStockfish_Begin(id, 3, startFen, '', 0, 150);
  assert.match(take(id).value, /WebAssembly e Web Workers/);
  api.ChessStockfish_Dispose(id);
});

test('new request during a search replaces the worker and dispose releases the channel', () => {
  const { api, workers, channels, take } = load();
  const id = api.ChessStockfish_Create();
  api.ChessStockfish_Begin(id, 1, startFen, '', 0, 150);
  api.ChessStockfish_Begin(id, 2, startFen, '', 0, 150);
  assert.equal(workers[0].terminated, true);
  assert.equal(workers.length, 2);
  api.ChessStockfish_Dispose(id);
  assert.equal(workers[1].terminated, true);
  assert.equal(channels[id], undefined);
  assert.equal(take(id), null);
});

// This uses the real pinned WASM engine via Node stdin/stdout, while exercising
// the same bridge protocol. A real browser Worker and Unity remain separate QA.
test('real Stockfish 18 WASM returns legal candidates for both sides through bridge', { timeout: 20000 }, async t => {
  const { spawn } = require('node:child_process');
  const readline = require('node:readline');
  const engine = path.resolve(__dirname, '../../.local/stockfish-web/engine/stockfish-18-lite-single.js');
  assert.ok(fs.existsSync(engine), 'Run python3 tools/setup_stockfish_web.py first.');
  const processes = [];
  class ProcessWorker {
    constructor() {
      this.child = spawn(process.execPath, [engine], { stdio: ['pipe', 'pipe', 'pipe'] });
      processes.push(this.child);
      const lines = readline.createInterface({ input: this.child.stdout });
      lines.on('line', data => { if (this.onmessage) this.onmessage({ data }); });
      this.child.stderr.on('data', () => {});
      this.child.on('error', () => { if (this.onerror) this.onerror({}); });
    }
    postMessage(line) { this.child.stdin.write(line + '\n'); }
    terminate() { this.child.kill(); }
  }
  t.after(() => processes.forEach(child => child.kill()));
  const { api, take } = load(ProcessWorker);
  const id = api.ChessStockfish_Create();
  async function getMove(requestId, moves) {
    api.ChessStockfish_Begin(id, requestId, startFen, moves, 6, 100);
    const deadline = Date.now() + 8000;
    while (Date.now() < deadline) {
      const message = take(id);
      if (message && message.type === 'error') assert.fail(message.value);
      if (message && message.type === 'bestmove') return message.value;
      await new Promise(resolve => setTimeout(resolve, 10));
    }
    assert.fail('Real WASM engine timed out.');
  }
  const white = await getMove(1, '');
  assert.match(white, /^(?:[a-h]2[a-h][34]|[bg]1[acfh]3)$/);
  const black = await getMove(2, white);
  assert.match(black, /^(?:[a-h]7[a-h][56]|[bg]8[acfh]6)$/);
  api.ChessStockfish_Dispose(id);
  console.log('REAL_STOCKFISH_WEB_MOVES', white, black);
});
