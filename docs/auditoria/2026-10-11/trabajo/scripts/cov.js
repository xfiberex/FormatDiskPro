// Cobertura real por archivo y por carpeta a partir de un informe Cobertura (coverlet).
// Uso: node cov.js <coverage.cobertura.xml>
const fs = require('fs');
const xml = fs.readFileSync(process.argv[2], 'utf8');
const BS = String.fromCharCode(92);
const files = {};
const classRe = /<class name="([^"]*)" filename="([^"]*)"[^>]*>([\s\S]*?)<\/class>/g;
let m;
while ((m = classRe.exec(xml))) {
  const file = m[2].split(BS).join('/').replace(/^.*\/src\/FormatDiskPro\//, '').replace(/^FormatDiskPro\//, '');
  const f = (files[file] ||= { lines: new Map(), br: [0, 0] });
  // Solo las líneas de la clase: las de <methods> están repetidas.
  const body = m[3].replace(/<methods>[\s\S]*?<\/methods>/, '');
  const lineRe = /<line number="(\d+)" hits="(\d+)"([^>]*)>/g;
  let l;
  while ((l = lineRe.exec(body))) {
    const n = +l[1], h = +l[2];
    f.lines.set(n, Math.max(f.lines.get(n) || 0, h));
    const cc = /condition-coverage="\d+% \((\d+)\/(\d+)\)"/.exec(l[3]);
    if (cc) { f.br[0] += +cc[1]; f.br[1] += +cc[2]; }
  }
}
const wanted = k => /^(Core|Services|Localization|UI)\//.test(k) || /^(App\.xaml|Program)\.cs$/.test(k);
const rows = Object.entries(files)
  .filter(([k]) => wanted(k) && !/\.g(\.i)?\.cs$/.test(k))
  .map(([k, v]) => {
    const t = v.lines.size, c = [...v.lines.values()].filter(h => h > 0).length;
    return { k, t, c, pct: t ? (100 * c / t) : 0, br: v.br[1] ? (100 * v.br[0] / v.br[1]) : null };
  })
  .sort((a, b) => a.k.localeCompare(b.k));
const agg = {};
for (const r of rows) {
  const d = r.k.includes('/') ? r.k.split('/')[0] : '(raiz)';
  agg[d] ||= [0, 0]; agg[d][0] += r.c; agg[d][1] += r.t;
}
for (const r of rows) {
  if (r.k.startsWith('UI/')) continue;
  const br = r.br === null ? '  -' : r.br.toFixed(0).padStart(3) + ' %';
  console.log(`${r.k.padEnd(34)} ${String(r.c).padStart(4)}/${String(r.t).padEnd(4)} ${r.pct.toFixed(0).padStart(3)} %  ramas ${br}`);
}
console.log('--- por carpeta ---');
for (const [d, [c, t]] of Object.entries(agg)) console.log(`${d.padEnd(14)} ${c}/${t} = ${(100 * c / t).toFixed(1)} %`);
