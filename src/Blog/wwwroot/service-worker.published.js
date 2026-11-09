// Production service worker: precaches the app so it starts offline, and keeps
// a runtime cache of posts so posts that were read before stay available offline.
// See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));
self.addEventListener('message', event => {
    // Sent by js/blog.js when the visitor clicks "Reload" on the update banner.
    if (event.data && event.data.type === 'SKIP_WAITING') self.skipWaiting();
});

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
// Not version-specific, so posts read before stay available after an app update.
const contentCacheName = 'blog-content';
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/, /^404\.html$/ ];

// The base path is derived from the service worker location, e.g. "/" locally and "/blog/" on GitHub Pages.
const baseUrl = new URL('./', self.location);
const contentUrl = new URL('posts/', baseUrl).href;
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    const request = event.request;
    if (request.method !== 'GET') return fetch(request);

    // Posts (index.json, Markdown and images): network first, so new posts show up
    // without an app update; fall back to the last cached copy when offline.
    if (request.url.startsWith(contentUrl)) return networkFirst(request);

    // For all navigation requests, try to serve index.html from cache,
    // unless that request is for an offline resource.
    const shouldServeIndexHtml = request.mode === 'navigate'
        && !manifestUrlList.some(url => url === request.url);

    const cache = await caches.open(cacheName);
    const cachedResponse = await cache.match(shouldServeIndexHtml ? 'index.html' : request);
    return cachedResponse || fetch(request);
}

async function networkFirst(request) {
    const cache = await caches.open(contentCacheName);
    try {
        const response = await fetch(request);
        if (response.ok) await cache.put(request, response.clone());
        return response;
    } catch (error) {
        const cached = await cache.match(request) || await caches.match(request);
        if (cached) return cached;
        throw error;
    }
}
