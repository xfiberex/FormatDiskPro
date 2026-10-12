// Genera las tareas de la Parte 3 del ROADMAP a partir de INFORME.md, para que ID, título, área,
// severidad, esfuerzo, ubicación, qué hacer y criterio de aceptación sean los mismos en los dos sitios.
// Uso: node roadmap-gen.js <INFORME.md> <salida.md>
const fs = require('fs');
const [,, inPath, outPath] = process.argv;
const text = fs.readFileSync(inPath, 'utf8').replace(/\r\n/g, '\n');

const deps = {
  'AUD-012': 'AUD-015 (misma extracción de las guardas a `Core/`)',
  'AUD-013': 'AUD-015 (misma extracción de las guardas a `Core/`)',
  'AUD-025': 'AUD-002 (misma decisión sobre dónde vive lo que escribe el proceso elevado)',
  'AUD-036': 'AUD-001 (comparten el tratamiento de la cancelación)',
  'AUD-063': 'AUD-022',
  'AUD-071': 'AUD-069 y AUD-024 (decidir antes el identificador de licencia y el permiso adicional)',
};

const blocks = text.split(/\n(?=### AUD-\d{3} · )/).filter(b => /^### AUD-\d{3} · /.test(b));
const items = blocks.map(b => {
  const head = /^### (AUD-\d{3}) · (.+)$/m.exec(b);
  const meta = /^\*\*Área:\*\* (.+?) · \*\*Severidad:\*\* (\w+) · .*\*\*Esfuerzo:\*\* (\w+)/m.exec(b);
  const field = name => {
    const m = new RegExp('^\\*\\*' + name + ':\\*\\* (.+)$', 'm').exec(b);
    return m ? m[1].trim() : '';
  };
  if (!head || !meta) throw new Error('Bloque sin cabecera o metadatos: ' + b.slice(0, 80));
  return {
    id: head[1], title: head[2].replace(/ · \*requiere revisión legal\*$/, ''),
    legal: / · \*requiere revisión legal\*$/.test(head[2]),
    area: meta[1], sev: meta[2], effort: meta[3],
    where: field('Ubicación'), state: field('Estado'),
    todo: field('Solución propuesta'), accept: field('Criterio de aceptación'),
  };
});

const tiers = [
  ['Alto',  '## 🟠 Tier 1 — Alto'],
  ['Medio', '## 🟡 Tier 2 — Medio'],
  ['Bajo',  '## 🟢 Tier 3 — Bajo'],
];
const out = [];
for (const [sev, heading] of tiers) {
  const list = items.filter(i => i.sev === sev);
  out.push(heading, '');
  let area = null;
  for (const i of list) {
    if (sev === 'Bajo' && i.area !== area) { area = i.area; out.push(`### ${area}`, ''); }
    out.push(`- [ ] **${i.id} · ${i.title}**`);
    out.push(`  - **Área:** ${i.area} · **Severidad:** ${i.sev} · **Esfuerzo:** ${i.effort}${i.legal ? ' · *requiere revisión legal*' : ''}`);
    out.push(`  - **Ubicación:** ${i.where}`);
    out.push(`  - **Qué hacer:** ${i.todo}`);
    out.push(`  - **Criterio de aceptación:** ${i.accept}`);
    out.push(`  - **Depende de:** ${deps[i.id] || 'ninguna'}`);
    out.push(`  - **Origen:** auditoría 2026-10-11${/^(ya conocido|regresi)/.test(i.state) ? ' · ' + i.state.replace(/\.$/, '') : ''}`);
    out.push('');
  }
}
fs.writeFileSync(outPath, out.join('\n'), 'utf8');

// Resumen para el índice
const count = (f) => items.filter(f).length;
const areas = [...new Set(items.map(i => i.area))];
console.log(`Total: ${items.length} · Alto ${count(i => i.sev === 'Alto')} · Medio ${count(i => i.sev === 'Medio')} · Bajo ${count(i => i.sev === 'Bajo')}`);
console.log(`Esfuerzo: Bajo ${count(i => i.effort === 'Bajo')} · Medio ${count(i => i.effort === 'Medio')} · Alto ${count(i => i.effort === 'Alto')}`);
for (const a of areas) console.log(`${a}: ${count(i => i.area === a)} (A ${count(i => i.area === a && i.sev === 'Alto')} · M ${count(i => i.area === a && i.sev === 'Medio')} · B ${count(i => i.area === a && i.sev === 'Bajo')})`);
const missing = items.filter(i => !i.where || !i.todo || !i.accept).map(i => i.id);
console.log('Campos vacíos: ' + (missing.length ? missing.join(', ') : 'ninguno'));
