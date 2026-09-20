const description = document.querySelector('#Description');
const count = document.querySelector('#count');
const updateCount = () => { count.textContent = `${description.value.length.toLocaleString()} / 2,000`; };
description.addEventListener('input', updateCount);
document.querySelectorAll('[data-example]').forEach(button => button.addEventListener('click', () => {
  description.value = button.dataset.example; updateCount(); description.focus();
}));
document.querySelector('#intake-form').addEventListener('submit', () => {
  document.querySelector('#analyse').disabled = true;
  document.querySelector('#working').hidden = false;
});
const reviewed = document.querySelector('#reviewed');
const download = document.querySelector('#download');
const card = document.querySelector('#job-card');
if (reviewed) {
  reviewed.addEventListener('change', () => { download.disabled = !reviewed.checked || !card.value.trim(); });
  card.addEventListener('input', () => { reviewed.checked = false; download.disabled = true; document.querySelector('#export-status').textContent = ''; });
  download.addEventListener('click', () => {
    if (!reviewed.checked || !card.value.trim()) return;
    const text = `GARAGE INTAKE — ADVISER-REVIEWED DRAFT\n\n${card.value}\n\nORIGINAL CUSTOMER DESCRIPTION\n${description.defaultValue}\n\nIntake notes only; not a diagnosis or roadworthiness assessment.\n`;
    const url = URL.createObjectURL(new Blob([text], {type: 'text/plain;charset=utf-8'}));
    const link = document.createElement('a'); link.href = url; link.download = 'garage-job-card.txt'; link.click();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
    document.querySelector('#export-status').textContent = 'Job card downloaded.';
  });
}
