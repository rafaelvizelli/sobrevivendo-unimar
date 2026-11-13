// Servidor local da prévia do trailer (Node, sem dependências).
// - Serve a pasta Trailer/ (a prévia lê os planos em ../clips/ sem "manchar" o canvas, então a exportação funciona).
// - POST /salvar?arquivo=export/x.webm grava o que a página manda (só em export/ e verificacao/).
// Uso:
//   node Trailer/previa/servidor.mjs             -> abre em http://127.0.0.1:8765/previa/Trailer_Previa.html
//   node Trailer/previa/servidor.mjs --exportar  -> roda o Chrome sem janela, exporta os 3 cortes e as verificações, e sai
//   node Trailer/previa/servidor.mjs --verificar[=nome] -> checagem determinística dos 3 cortes (capturas e relatório em
//                                                   verificacao/<nome>/); sai com código 1 se achar problema
import http from 'node:http';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const raiz = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const porta = Number(process.env.PORTA || 8765);
const exportar = process.argv.includes('--exportar');
const argVerificar = process.argv.find(a => a.startsWith('--verificar'));   // --verificar ou --verificar=nome
const verificar = argVerificar ? (argVerificar.split('=')[1] || 'atual') : null;
const tipos = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.mjs': 'text/javascript', '.mp4': 'video/mp4',
  '.png': 'image/png', '.webm': 'video/webm', '.json': 'application/json', '.css': 'text/css', '.mp3': 'audio/mpeg',
  '.txt': 'text/plain; charset=utf-8',
};
let terminar;
const fim = new Promise(r => (terminar = r));

const srv = http.createServer((req, res) => {
  const u = new URL(req.url, 'http://local');
  if (req.method === 'POST' && u.pathname === '/salvar') {
    const rel = u.searchParams.get('arquivo') || '';
    if (!/^(export|verificacao)\/([\w\-]+\/)?[\w.\-]+$/.test(rel)) { res.writeHead(400); res.end('caminho inválido'); return; }
    const dest = path.join(raiz, rel);
    fs.mkdirSync(path.dirname(dest), { recursive: true });
    const ws = fs.createWriteStream(dest);
    req.pipe(ws);
    ws.on('finish', () => { console.log('salvo:', rel, (fs.statSync(dest).size / 1e6).toFixed(1), 'MB'); res.end('ok'); });
    return;
  }
  if (req.method === 'POST' && (u.pathname === '/log' || u.pathname === '/fim')) {
    let corpo = '';
    req.on('data', d => (corpo += d));
    req.on('end', () => {
      console.log(u.pathname === '/fim' ? '[fim] ' + corpo : '[página] ' + corpo);
      res.end('ok');
      if (u.pathname === '/fim') terminar(corpo);
    });
    return;
  }
  const arquivo = path.join(raiz, decodeURIComponent(u.pathname));
  if (!arquivo.startsWith(raiz)) { res.writeHead(403); res.end(); return; }
  fs.stat(arquivo, (e, st) => {
    if (e || !st.isFile()) { res.writeHead(404); res.end('não achei'); return; }
    const tipo = tipos[path.extname(arquivo).toLowerCase()] || 'application/octet-stream';
    const faixa = /bytes=(\d*)-(\d*)/.exec(req.headers.range || '');
    if (faixa) {
      const ini = faixa[1] ? Number(faixa[1]) : 0;
      const ult = faixa[2] ? Number(faixa[2]) : st.size - 1;
      res.writeHead(206, { 'Content-Type': tipo, 'Content-Range': `bytes ${ini}-${ult}/${st.size}`, 'Accept-Ranges': 'bytes', 'Content-Length': ult - ini + 1 });
      fs.createReadStream(arquivo, { start: ini, end: ult }).pipe(res);
    } else {
      res.writeHead(200, { 'Content-Type': tipo, 'Content-Length': st.size, 'Accept-Ranges': 'bytes', 'Cache-Control': 'no-store' });
      fs.createReadStream(arquivo).pipe(res);
    }
  });
});

function acharChrome() {
  const c = [
    process.env.CHROME,
    'C:/Program Files/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
    'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
    '/usr/bin/google-chrome', '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
  ].filter(Boolean);
  return c.find(p => fs.existsSync(p));
}

srv.listen(porta, '127.0.0.1', async () => {
  const url = `http://127.0.0.1:${porta}/previa/Trailer_Previa.html`;
  console.log('servindo', raiz, 'em', url);
  if (!exportar && !verificar) return;
  const chrome = acharChrome();
  if (!chrome) { console.error('Não achei o Chrome. Defina CHROME=caminho.'); process.exit(1); }
  const perfil = fs.mkdtempSync(path.join(os.tmpdir(), 'trailer-chrome-'));
  const proc = spawn(chrome, [
    '--headless=new', `--user-data-dir=${perfil}`, '--no-first-run', '--no-default-browser-check',
    '--autoplay-policy=no-user-gesture-required', '--disable-background-timer-throttling',
    '--disable-renderer-backgrounding', '--window-size=1280,900', url + (verificar ? '#verificar=' + verificar : '#exportar'),
  ], { stdio: 'ignore' });
  const limite = setTimeout(() => terminar('tempo esgotado (40 min)'), 40 * 60 * 1000);
  const r = await fim;
  clearTimeout(limite);
  proc.kill();
  srv.close();
  if (verificar) for (const id of ['mestre', 'c30', 'v30']) {
    const rel = path.join(raiz, 'verificacao', verificar, `relatorio_${id}.txt`);
    if (fs.existsSync(rel)) console.log('\n' + fs.readFileSync(rel, 'utf8'));
  }
  setTimeout(() => { try { fs.rmSync(perfil, { recursive: true, force: true }); } catch { } process.exit(String(r).startsWith('ok') ? 0 : 1); }, 800);
});
