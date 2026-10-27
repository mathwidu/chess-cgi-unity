#!/usr/bin/env node
// Measure a lossless transfer-only alternative. Never mutates the Unity build.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const assert = require('node:assert/strict');
const { performance } = require('node:perf_hooks');

const root = path.resolve(process.argv[2] || 'Builds/Web');
const entries = [];
for (const folder of ['Build', 'stockfish']) {
  for (const name of fs.readdirSync(path.join(root, folder)).sort()) {
    if (!(folder === 'Build' ? /\.(gz|js|wasm)$/ : /\.(js|wasm)$/).test(name)) continue;
    const served = fs.readFileSync(path.join(root, folder, name));
    const original = name.endsWith('.gz') ? zlib.gunzipSync(served) : served;
    const start = performance.now();
    const br = zlib.brotliCompressSync(original, {params: {[zlib.constants.BROTLI_PARAM_QUALITY]: 9}});
    assert.ok(zlib.brotliDecompressSync(br).equals(original), 'Compression changed the original bytes');
    entries.push({file:`${folder}/${name}`, group:folder === 'Build' ? 'initial' : 'ai', servedBytes:served.length, decodedBytes:original.length, brotliQ9Bytes:br.length, compressionMs:Math.round(performance.now()-start)});
  }
}
const totals = {};
for (const group of ['initial', 'ai']) {
  const rows = entries.filter(item => item.group === group);
  const sum = key => rows.reduce((total, item) => total + item[key], 0);
  totals[group] = {servedBytes:sum('servedBytes'), brotliQ9Bytes:sum('brotliQ9Bytes')};
}
const report = {measuredAt:new Date().toISOString(), method:'Node Brotli quality 9; decoded-byte equality verified; estimate only, build untouched', totals, entries};
if (process.argv[3]) fs.writeFileSync(process.argv[3], JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
