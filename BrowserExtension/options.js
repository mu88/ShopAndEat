const DEFAULT_HOSTS = [
  'http://localhost:8080/shopAndEat/shopping*',
  'http://localhost:5176/shopAndEat/shopping*',
];

function applyI18n() {
  document.querySelectorAll('[data-i18n]').forEach(el => {
    const message = chrome.i18n.getMessage(el.dataset.i18n);
    if (message) el.textContent = message;
  });
  document.title = chrome.i18n.getMessage('optionsTitle');
}

function showStatus(messageKey) {
  const status = document.getElementById('status');
  status.textContent = chrome.i18n.getMessage(messageKey);
  setTimeout(() => { status.textContent = ''; }, 2500);
}

function loadOptions() {
  chrome.storage.sync.get({ bridgeHosts: DEFAULT_HOSTS }, ({ bridgeHosts }) => {
    document.getElementById('bridgeHosts').value = bridgeHosts.join('\n');
  });
}

function saveOptions() {
  const raw = document.getElementById('bridgeHosts').value;
  const bridgeHosts = raw.split('\n').map(l => l.trim()).filter(l => l.length > 0);
  chrome.storage.sync.set({ bridgeHosts }, () => showStatus('optionsSaved'));
}

function resetOptions() {
  document.getElementById('bridgeHosts').value = DEFAULT_HOSTS.join('\n');
  chrome.storage.sync.set({ bridgeHosts: DEFAULT_HOSTS }, () => showStatus('optionsResetDone'));
}

document.addEventListener('DOMContentLoaded', () => {
  applyI18n();
  loadOptions();
  document.getElementById('save').addEventListener('click', saveOptions);
  document.getElementById('reset').addEventListener('click', resetOptions);
});
