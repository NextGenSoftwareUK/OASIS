// Inserts the sections and keys that a DNA file lacks (compared with the template), as text, so comments and the
// existing values stay exactly as they are. Never changes an existing value.
//   merge(text, template, overrides) -> { text, added: [paths] }
// `overrides` maps '/OASIS/Section/Key' -> value and applies only to keys that are inserted.
const SECRET_NAME = /(PrivateKey|PrivateSeed|ApiKey|Password|Secret|Token)$/i;

function parseWithPositions(text) {
  let i = 0;
  const ws = () => {
    for (;;) {
      while (i < text.length && /\s/.test(text[i])) i++;
      if (text[i] === '/' && text[i + 1] === '/') { while (i < text.length && text[i] !== '\n') i++; continue; }
      if (text[i] === '/' && text[i + 1] === '*') { i += 2; while (i < text.length && !(text[i] === '*' && text[i + 1] === '/')) i++; i += 2; continue; }
      break;
    }
  };
  const str = () => {
    const start = i; i++;
    while (text[i] !== '"') { if (text[i] === '\\') i++; i++; }
    i++;
    return JSON.parse(text.slice(start, i));
  };
  const value = () => {
    ws();
    const start = i;
    if (text[i] === '{') {
      const node = { type: 'object', start, members: new Map(), openPos: i, lastEnd: i + 1 };
      i++;
      for (;;) {
        ws();
        if (text[i] === '}') { node.closePos = i; i++; break; }
        if (text[i] === ',') { i++; continue; }
        const key = str(); ws();
        if (text[i] !== ':') throw new Error('expected : at ' + i);
        i++;
        const v = value();
        node.members.set(key, v);
        node.lastEnd = v.end;
      }
      node.end = i; return node;
    }
    if (text[i] === '[') {
      let depth = 0;
      for (; i < text.length; i++) {
        if (text[i] === '"') { str(); i--; continue; }
        if (text[i] === '/' && text[i + 1] === '/') { while (text[i] !== '\n') i++; continue; }
        if (text[i] === '[') depth++;
        if (text[i] === ']' && --depth === 0) { i++; break; }
      }
      return { type: 'array', start, end: i };
    }
    if (text[i] === '"') { str(); return { type: 'scalar', start, end: i }; }
    while (i < text.length && !/[,}\]\s]/.test(text[i])) i++;
    return { type: 'scalar', start, end: i };
  };
  const root = value();
  return root;
}

function lineIndent(text, pos) {
  const ls = text.lastIndexOf('\n', pos - 1) + 1;
  const m = /^[ \t]*/.exec(text.slice(ls, pos));
  return m[0];
}

function scrub(v, key) {
  if (v && typeof v === 'object' && !Array.isArray(v)) {
    const o = {};
    for (const k of Object.keys(v)) o[k] = scrub(v[k], k);
    return o;
  }
  return typeof v === 'string' && v !== '' && SECRET_NAME.test(key) ? '' : v;
}

function applyOverrides(v, path, overrides) {
  if (path in overrides) return overrides[path];
  if (v && typeof v === 'object' && !Array.isArray(v)) {
    const o = {};
    for (const k of Object.keys(v)) o[k] = applyOverrides(v[k], path + '/' + k, overrides);
    return o;
  }
  return v;
}

exports.merge = function merge(text, template, overrides = {}, options = {}) {
  const clean = (v, k) => (options.keepSecrets ? v : scrub(v, k));
  const root = parseWithPositions(text);
  const edits = []; // {pos, insert, end?}
  const added = [];
  const walk = (node, tpl, path) => {
    const missing = Object.keys(tpl).filter(k => !node.members.has(k));
    if (missing.length) {
      const memberIndent = node.members.size
        ? lineIndent(text, [...node.members.values()][0].start)
        : lineIndent(text, node.openPos) + '  ';
      const block = missing.map(k => {
        const v = applyOverrides(clean(tpl[k], k), path + '/' + k, overrides);
        added.push(path + '/' + k);
        const body = JSON.stringify(v, null, 2).split('\n').join('\n' + memberIndent);
        return memberIndent + JSON.stringify(k) + ': ' + body;
      }).join(',\n');
      if (node.members.size) edits.push({ pos: node.lastEnd, insert: ',\n' + block });
      else edits.push({ pos: node.openPos + 1, insert: '\n' + block + '\n' + lineIndent(text, node.openPos) });
    }
    for (const k of Object.keys(tpl)) {
      const child = node.members.get(k);
      // A section set to null is filled with its defaults, as Scripts/Sync-OASISDNATemplate.ps1 does.
      if (child && child.type === 'scalar' && text.slice(child.start, child.end) === 'null' && tpl[k] && typeof tpl[k] === 'object' && !Array.isArray(tpl[k])) {
        const memberIndent = lineIndent(text, child.start);
        const v = applyOverrides(clean(tpl[k], k), path + '/' + k, overrides);
        added.push(path + '/' + k + ' (was null)');
        edits.push({ pos: child.start, end: child.end, insert: JSON.stringify(v, null, 2).split('\n').join('\n' + memberIndent) });
        continue;
      }
      if (child && child.type === 'object' && tpl[k] && typeof tpl[k] === 'object' && !Array.isArray(tpl[k])) walk(child, tpl[k], path + '/' + k);
    }
  };
  walk(root, template, '');
  edits.sort((a, b) => b.pos - a.pos);
  let out = text;
  for (const e of edits) out = out.slice(0, e.pos) + e.insert + out.slice(e.end ?? e.pos);
  return { text: out, added };
};

// Replaces the value of an existing key (scalar/array/object) with new JSON, keeping everything around it.
exports.replace = function replace(text, path, newValue) {
  const root = parseWithPositions(text);
  let node = root;
  for (const k of path.split('/').filter(Boolean)) { node = node.members.get(k); if (!node) throw new Error('no such path ' + path); }
  const indent = lineIndent(text, node.start);
  const body = JSON.stringify(newValue, null, 2).split('\n').join('\n' + indent);
  return text.slice(0, node.start) + body + text.slice(node.end);
};
