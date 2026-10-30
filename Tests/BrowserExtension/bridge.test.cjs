const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const extensionPath = path.resolve(__dirname, '../../BrowserExtension');

function createBackground(bridgeHosts, firefox = true) {
  const listeners = {};
  const registrations = [];
  const state = { bridgeHosts };
  const chrome = {
    i18n: { getMessage: message => message },
    runtime: {
      onMessage: { addListener: listener => { listeners.message = listener; } },
      onInstalled: { addListener: listener => { listeners.installed = listener; } },
      onStartup: { addListener: () => {} },
    },
    storage: {
      sync: { get: async defaults => ({ bridgeHosts: state.bridgeHosts ?? defaults.bridgeHosts }) },
      onChanged: { addListener: listener => { listeners.changed = listener; } },
    },
    scripting: {
      unregisterContentScripts: async () => { await new Promise(resolve => setImmediate(resolve)); },
      registerContentScripts: async scripts => { registrations.push(scripts[0]); },
    },
    tabs: { onRemoved: { addListener: () => {} } },
  };
  const context = { chrome, console: { log() {} }, URL, setTimeout, clearTimeout };
  if (firefox) context.browser = { runtime: { getBrowserInfo() {} } };
  vm.runInNewContext(fs.readFileSync(path.join(extensionPath, 'background.js'), 'utf8'), context);
  return {
    state, listeners, registrations,
    send(message, url) {
      return new Promise((resolve, reject) => {
        const timer = setTimeout(() => reject(new Error('Message was not answered')), 1000);
        listeners.message(message, url ? { url } : {}, response => { clearTimeout(timer); resolve(response); });
      });
    },
  };
}

const cases = [
  ['allowed-port', 'http://localhost:8080/shopAndEat/shopping*', 'http://localhost:8080/shopAndEat/shopping?test=1', true],
  ['wrong-port', 'http://localhost:8080/shopAndEat/shopping*', 'http://localhost:9999/shopAndEat/shopping', false],
  ['wrong-path', 'http://localhost:8080/shopAndEat/shopping*', 'http://localhost:8080/other', false],
  ['wrong-scheme', 'https://example.test/shopping*', 'http://example.test/shopping', false],
  ['wrong-host', 'https://example.test/shopping*', 'https://example.test.attacker.test/shopping', false],
  ['implicit-default-port', 'https://example.test:443/shopping*', 'https://example.test/shopping', true],
  ['no-port-restriction', 'http://localhost/shopping*', 'http://localhost:9999/shopping', true],
  ['wildcard-subdomain', '*://*.example.test/shopping*', 'https://app.example.test:8443/shopping', true],
  ['wildcard-root', '*://*.example.test/shopping*', 'http://example.test/shopping', true],
  ['wildcard-host-boundary', '*://*.example.test/shopping*', 'https://notexample.test/shopping', false],
  ['ipv6-port', 'http://[::1]:8080/shopping*', 'http://[::1]:8080/shopping', true],
  ['ipv6-wrong-port', 'http://[::1]:8080/shopping*', 'http://[::1]:8081/shopping', false],
  ['missing-sender', 'http://localhost:8080/*', undefined, false],
  ['empty-settings', null, 'http://localhost:8080/shopAndEat/shopping', false],
  ['invalid-pattern', 'invalid', 'http://localhost:8080/shopAndEat/shopping', false],
  ['path-query', 'https://example.test/shopping?view=*', 'https://example.test/shopping?view=cart', true],
  ['path-regex-literal', 'https://example.test/shop.ping*', 'https://example.test/shopXping', false],
  ['all-urls', '<all_urls>', 'https://example.test/shopping', true],
  ['all-urls-rejects-file', '<all_urls>', 'file:///shopping', false],
  ['malformed-sender', 'http://localhost/*', 'not a URL', false],
];

for (const [name, pattern, url, allowed] of cases) {
  test(`BridgeAuthorization_${name}`, async () => {
    const background = createBackground(pattern === null ? [] : [pattern]);
    const response = await background.send({ type: 'BRIDGE_CHECK' }, url);
    assert.equal(response.allowed, allowed);
  });
}

test('ToolRequest_RejectsWrongPortAndPreservesCallId', async () => {
  const background = createBackground(['http://localhost:8080/shopping*']);
  const response = await background.send({ type: 'TOOL_REQUEST', request: { id: 'denied', tool: 'unknown', args: {} } }, 'http://localhost:9999/shopping');
  assert.equal(response.success, false);
  assert.equal(response.id, 'denied');
  assert.match(response.error, /not allowed/i);
});

test('ToolRequest_RechecksSavedSettingsAfterInjection', async () => {
  const background = createBackground(['http://localhost:8080/shopping*']);
  const url = 'http://localhost:8080/shopping';
  assert.equal((await background.send({ type: 'BRIDGE_CHECK' }, url)).allowed, true);
  background.state.bridgeHosts = [];
  const response = await background.send({ type: 'TOOL_REQUEST', request: { id: 'revoked', tool: 'unknown', args: {} } }, url);
  assert.equal(response.success, false);
  assert.match(response.error, /not allowed/i);
});

test('ToolRequest_AllowedSenderReachesToolDispatch', async () => {
  const background = createBackground(['https://app.example.test/shopping*']);
  const response = await background.send({ type: 'TOOL_REQUEST', request: { id: 'allowed', tool: 'unknown', args: {} } }, 'https://app.example.test/shopping');
  assert.equal(response.id, 'allowed');
  assert.equal(response.error, 'unknownTool');
});

test('Registration_DeduplicatesFirefoxPatternsAndPreservesStoredPorts', async () => {
  const background = createBackground(['http://localhost:8080/shopping*', 'http://localhost:5176/shopping*']);
  await background.listeners.installed();
  assert.deepEqual(Array.from(background.registrations[0].matches), ['http://localhost/shopping*']);
  assert.deepEqual(background.state.bridgeHosts, ['http://localhost:8080/shopping*', 'http://localhost:5176/shopping*']);
});

test('Registration_PreservesChromePatterns', async () => {
  const background = createBackground(['http://localhost:8080/shopping*'], false);
  await background.listeners.installed();
  assert.deepEqual(Array.from(background.registrations[0].matches), ['http://localhost:8080/shopping*']);
});

test('Registration_EmptySettingsUnregistersWithoutRegisteringEmptyMatches', async () => {
  const background = createBackground([]);
  await background.listeners.installed();
  assert.equal(background.registrations.length, 0);
});

test('Registration_NormalizesIpv6Port', async () => {
  const background = createBackground(['http://[::1]:8080/shopping*']);
  await background.listeners.installed();
  assert.deepEqual(Array.from(background.registrations[0].matches), ['http://[::1]/shopping*']);
});

test('Registration_SerializesInstallationAndSettingsChanges', async () => {
  const background = createBackground(['http://localhost:8080/old*']);
  const installation = background.listeners.installed();
  background.state.bridgeHosts = ['https://app.example.test/new*'];
  const change = background.listeners.changed({ bridgeHosts: {} }, 'sync');
  await Promise.all([installation, change]);
  assert.deepEqual(Array.from(background.registrations.at(-1).matches), ['https://app.example.test/new*']);
});

for (const allowed of [false, true]) {
  test(`ContentBridge_${allowed ? 'AnnouncesAllowedPage' : 'DoesNotActivateDeniedPage'}`, async () => {
    const messages = [];
    const listeners = [];
    const window = {
      location: { origin: 'http://localhost:8080' },
      postMessage: message => messages.push(message),
      addEventListener: (name, listener) => listeners.push(listener),
    };
    const chrome = {
      i18n: { getMessage: message => message },
      runtime: {
        sendMessage: (message, callback) => callback({ allowed }),
        onMessage: { addListener() {} },
      },
    };
    vm.runInNewContext(fs.readFileSync(path.join(extensionPath, 'content-bridge.js'), 'utf8'), { chrome, window, console, setTimeout, clearTimeout });
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(messages.some(message => message.type === 'SHOP_EXTENSION_CONNECTED'), allowed);
    assert.equal(listeners.length, allowed ? 1 : 0);
  });
}
