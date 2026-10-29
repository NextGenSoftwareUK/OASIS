// Parses JSON with // and /* */ comments and trailing commas, respecting strings.
exports.strip = function (t) {
  let out = '', i = 0, inStr = false;
  while (i < t.length) {
    const c = t[i], n = t[i + 1];
    if (inStr) {
      out += c;
      if (c === '\\') { out += n; i += 2; continue; }
      if (c === '"') inStr = false;
      i++; continue;
    }
    if (c === '"') { inStr = true; out += c; i++; continue; }
    if (c === '/' && n === '/') { while (i < t.length && t[i] !== '\n') i++; continue; }
    if (c === '/' && n === '*') { i += 2; while (i < t.length && !(t[i] === '*' && t[i + 1] === '/')) i++; i += 2; continue; }
    out += c; i++;
  }
  return out.replace(/,(\s*[}\]])/g, '$1');
};
exports.parse = t => JSON.parse(exports.strip(t.replace(/^﻿/, '')));
exports.paths = function walk(o, p = '', acc = []) {
  if (o && typeof o === 'object' && !Array.isArray(o)) {
    for (const k of Object.keys(o)) { acc.push(p + '/' + k); walk(o[k], p + '/' + k, acc); }
  }
  return acc;
};
