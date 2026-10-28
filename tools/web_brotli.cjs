#!/usr/bin/env node
// No dependencies: Node 22 zlib, quality 9, bounded decoding, byte-for-byte proof.
'use strict';
const fs = require('node:fs');
const zlib = require('node:zlib');
const crypto = require('node:crypto');

function record(data) {
  return { bytes: data.length, sha256: crypto.createHash('sha256').update(data).digest('hex') };
}

try {
  const [operation, input, outputOrLimit, inputEncoding, limit] = process.argv.slice(2);
  const inputBytes = fs.readFileSync(input);
  if (operation === 'encode') {
    const maxOutputLength = Number(limit);
    if (!Number.isSafeInteger(maxOutputLength) || maxOutputLength < 1 || !['gzip', 'identity'].includes(inputEncoding)) throw Error('Invalid encode arguments');
    const decoded = inputEncoding === 'gzip' ? zlib.gunzipSync(inputBytes, { maxOutputLength }) : inputBytes;
    if (!decoded.length || decoded.length > maxOutputLength) throw Error('Decoded payload is empty or exceeds limit');
    const encoded = zlib.brotliCompressSync(decoded, { params: {
      [zlib.constants.BROTLI_PARAM_QUALITY]: 9,
      [zlib.constants.BROTLI_PARAM_SIZE_HINT]: decoded.length,
    } });
    const roundtrip = zlib.brotliDecompressSync(encoded, { maxOutputLength });
    if (!decoded.equals(roundtrip)) throw Error('Brotli roundtrip mismatch');
    fs.writeFileSync(outputOrLimit, encoded, { flag: 'wx' });
    process.stdout.write(JSON.stringify(record(decoded)) + '\n');
  } else if (operation === 'inspect') {
    const maxOutputLength = Number(outputOrLimit);
    if (!Number.isSafeInteger(maxOutputLength) || maxOutputLength < 1) throw Error('Invalid decode limit');
    const decoded = zlib.brotliDecompressSync(inputBytes, { maxOutputLength });
    if (!decoded.length) throw Error('Decoded Brotli is empty');
    process.stdout.write(JSON.stringify({ ...record(decoded), beginning: decoded.subarray(0, 32).toString('hex') }) + '\n');
  } else {
    throw Error('Expected encode or inspect');
  }
} catch (error) {
  process.stderr.write('web-brotli: ' + error.message + '\n');
  process.exitCode = 1;
}
