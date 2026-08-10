// Script de copia de assets desde node_modules hacia carpetas que sirve IIS.
// Se ejecuta con: npm run build:assets   (o como parte de: npm run build)

const fs = require('fs');
const path = require('path');

function ensureDir(dir) {
  if (!fs.existsSync(dir)) fs.mkdirSync(dir, { recursive: true });
}

function copy(src, dest) {
  if (!fs.existsSync(src)) {
    console.error('  X NO ENCONTRADO:', src);
    return false;
  }
  ensureDir(path.dirname(dest));
  fs.copyFileSync(src, dest);
  const size = (fs.statSync(dest).size / 1024).toFixed(1);
  console.log('  +', dest.replace(/\\/g, '/'), `(${size} KB)`);
  return true;
}

console.log('Copiando assets de node_modules a las carpetas del proyecto...\n');

console.log('Chart.js:');
copy('node_modules/chart.js/dist/chart.umd.js', 'Scripts/chart.umd.min.js');

console.log('\nSweetAlert2:');
copy('node_modules/sweetalert2/dist/sweetalert2.all.min.js', 'Scripts/sweetalert2.min.js');

console.log('\nhtml2pdf.js:');
copy('node_modules/html2pdf.js/dist/html2pdf.bundle.min.js', 'Scripts/html2pdf.min.js');

// Lucide: el CSS hace referencia a "lucide.woff2" (minuscula). Copiamos con ese
// nombre exacto. El paquete npm los nombra con L mayuscula, asi que renombramos.
console.log('\nLucide icons:');
copy('node_modules/lucide-static/font/lucide.css', 'Content/css/lucide.css');
copy('node_modules/lucide-static/font/Lucide.woff2', 'Content/css/lucide.woff2');
copy('node_modules/lucide-static/font/Lucide.ttf', 'Content/css/lucide.ttf');

console.log('\nInter font:');
const interFiles = [
  { weight: '400', name: 'Regular' },
  { weight: '500', name: 'Medium' },
  { weight: '600', name: 'SemiBold' },
  { weight: '700', name: 'Bold' },
];
interFiles.forEach(({ weight, name }) => {
  copy(`node_modules/@fontsource/inter/files/inter-latin-${weight}-normal.woff2`,
       `Content/fonts/Inter-${name}.woff2`);
});

console.log('\nAssets listos. Ahora corre: npm run build:css');
