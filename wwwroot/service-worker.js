const CACHE_NAME = 'hrms-static-v1';
const STATIC_ASSETS = [
    '/css/hrms-theme.css',
    '/js/hrms-core.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js',
    '/manifest.json',
    '/favicon.ico'
];

// Sensitive routes that must NEVER be cached
const SENSITIVE_PATTERNS = [
    '/Account/',
    '/Attendance/ProcessPunch',
    '/Attendance/FacePunch',
    '/FaceRegistration/',
    '/Employee/',
    '/Dashboard/GetLiveStats',
    '/Dashboard/ExportTodayCsv'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.addAll(STATIC_ASSETS);
        }).then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(keys => {
            return Promise.all(
                keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key))
            );
        }).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    const request = event.request;
    const url = new URL(request.url);

    // Only handle GET requests
    if (request.method !== 'GET') {
        return;
    }

    // Do NOT cache sensitive routes or API endpoints
    for (const pattern of SENSITIVE_PATTERNS) {
        if (url.pathname.includes(pattern)) {
            event.respondWith(fetch(request));
            return;
        }
    }

    // Network-first strategy with cache fallback for static assets
    event.respondWith(
        fetch(request)
            .then(networkResponse => {
                if (networkResponse && networkResponse.status === 200) {
                    const cloned = networkResponse.clone();
                    if (STATIC_ASSETS.some(asset => url.pathname.endsWith(asset))) {
                        caches.open(CACHE_NAME).then(cache => cache.put(request, cloned));
                    }
                }
                return networkResponse;
            })
            .catch(() => caches.match(request))
    );
});

