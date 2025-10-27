/*
 * Setlist Studio Service Worker
 * Provides offline capabilities for musicians during performances
 * 
 * Cache Strategy:
 * - Critical App Resources: Cache First (CSS, JS, Images)
 * - API Data (Songs/Setlists): Network First with Cache Fallback  
 * - User-Generated Content: Cache with Background Sync
 * 
 * Performance Focus: Ensure reliable access to setlists during live performances
 * when internet connectivity is poor or unavailable.
 */

const CACHE_NAME = 'setlist-studio-v1.0.0';
const DYNAMIC_CACHE_NAME = 'setlist-studio-dynamic-v1.0.0';
const API_CACHE_NAME = 'setlist-studio-api-v1.0.0';

// Critical resources that should be available offline immediately
// These are essential for the app to function during performances
const CRITICAL_RESOURCES = [
    '/',
    '/_Host',
    '/manifest.json',
    '/favicon.png',
    '/icon-192.png', 
    '/icon-512.png',
    '/css/app.css',
    '/js/app.js',
    
    // MudBlazor essentials
    '/_content/MudBlazor/MudBlazor.min.css',
    '/_content/MudBlazor/MudBlazor.min.js',
    
    // Blazor Server essentials
    '/_framework/blazor.server.js',
    
    // Offline page for graceful degradation
    '/offline.html'
];

// API endpoints to cache for offline access
// Focus on read-only operations musicians need during performances
const CACHEABLE_API_ROUTES = [
    '/api/songs',
    '/api/setlists', 
    '/api/songs/search',
    '/api/setlists/search'
];

// Resources to exclude from caching (authentication, real-time data)
const CACHE_EXCLUSIONS = [
    '/api/auth/',
    '/login',
    '/logout', 
    '/_blazor/',
    '/negotiate',
    '.hot-reload'
];

/*
 * SERVICE WORKER INSTALLATION
 * Pre-cache critical resources for immediate offline access
 */
self.addEventListener('install', event => {
    console.log('[SW] Service Worker installing...');
    
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[SW] Pre-caching critical resources');
                return cache.addAll(CRITICAL_RESOURCES.map(url => {
                    // Handle relative URLs properly
                    return new Request(url, { cache: 'reload' });
                }));
            })
            .then(() => {
                console.log('[SW] Installation complete - ready for offline performance use');
                return self.skipWaiting();
            })
            .catch(error => {
                console.error('[SW] Pre-caching failed:', error);
            })
    );
});

/*
 * SERVICE WORKER ACTIVATION  
 * Clean up old caches and claim clients for immediate control
 */
self.addEventListener('activate', event => {
    console.log('[SW] Service Worker activating...');
    
    event.waitUntil(
        // Clean up old caches
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    if (cacheName !== CACHE_NAME && 
                        cacheName !== DYNAMIC_CACHE_NAME && 
                        cacheName !== API_CACHE_NAME) {
                        console.log('[SW] Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
        .then(() => {
            console.log('[SW] Activation complete - taking control of all clients');
            return self.clients.claim();
        })
    );
});

/*
 * FETCH EVENT HANDLER
 * Implements caching strategies based on request type
 * Critical for offline performance functionality
 */
self.addEventListener('fetch', event => {
    const request = event.request;
    const url = new URL(request.url);
    
    // Skip non-GET requests and excluded URLs
    if (request.method !== 'GET' || shouldExcludeFromCache(url)) {
        return;
    }
    
    // Route to appropriate caching strategy
    if (isCriticalResource(url)) {
        event.respondWith(cacheFirstStrategy(request));
    } else if (isAPIRequest(url)) {
        event.respondWith(networkFirstStrategy(request));
    } else if (isStaticResource(url)) {
        event.respondWith(cacheFirstStrategy(request));
    } else {
        event.respondWith(networkFirstWithFallback(request));
    }
});

/*
 * CACHE-FIRST STRATEGY
 * Perfect for static resources that rarely change (CSS, JS, images)
 * Provides instant loading for cached resources
 */
async function cacheFirstStrategy(request) {
    try {
        const cachedResponse = await caches.match(request);
        
        if (cachedResponse) {
            console.log('[SW] Cache hit:', request.url);
            return cachedResponse;
        }
        
        // Not in cache, fetch from network and cache
        const networkResponse = await fetch(request);
        
        if (networkResponse.ok) {
            const cache = await caches.open(CACHE_NAME);
            cache.put(request, networkResponse.clone());
            console.log('[SW] Cached new resource:', request.url);
        }
        
        return networkResponse;
        
    } catch (error) {
        console.error('[SW] Cache-first strategy failed:', error);
        
        // Return cached version if available, otherwise offline page
        const cachedResponse = await caches.match(request);
        return cachedResponse || caches.match('/offline.html');
    }
}

/*
 * NETWORK-FIRST STRATEGY  
 * Ideal for API requests - get fresh data when online, fallback to cache
 * Critical for setlist and song data during performances
 */
async function networkFirstStrategy(request) {
    try {
        // Always try network first for fresh data
        const networkResponse = await fetch(request);
        
        if (networkResponse.ok) {
            // Cache successful API responses for offline access
            const cache = await caches.open(API_CACHE_NAME);
            cache.put(request, networkResponse.clone());
            console.log('[SW] Cached API response:', request.url);
        }
        
        return networkResponse;
        
    } catch (error) {
        console.log('[SW] Network failed, checking cache for:', request.url);
        
        // Network failed, try cache
        const cachedResponse = await caches.match(request);
        
        if (cachedResponse) {
            console.log('[SW] Serving cached API data (offline mode):', request.url);
            return cachedResponse;
        }
        
        // No cached data available - return offline response for API calls
        return new Response(
            JSON.stringify({ 
                error: 'Offline', 
                message: 'This data is not available offline',
                offline: true 
            }),
            {
                status: 503,
                statusText: 'Service Unavailable - Offline Mode',
                headers: { 'Content-Type': 'application/json' }
            }
        );
    }
}

/*
 * NETWORK-FIRST WITH FALLBACK
 * For pages and general navigation
 * Ensures users can still access cached pages when offline
 */
async function networkFirstWithFallback(request) {
    try {
        const networkResponse = await fetch(request);
        
        if (networkResponse.ok) {
            const cache = await caches.open(DYNAMIC_CACHE_NAME);
            cache.put(request, networkResponse.clone());
        }
        
        return networkResponse;
        
    } catch (error) {
        console.log('[SW] Network failed, trying cache for:', request.url);
        
        // Try cache
        const cachedResponse = await caches.match(request);
        
        if (cachedResponse) {
            return cachedResponse;
        }
        
        // No cache available - redirect to offline page
        return caches.match('/offline.html') || 
               new Response('Offline - Page not available', { status: 503 });
    }
}

/*
 * UTILITY FUNCTIONS
 * Helper functions for routing and cache management
 */

function isCriticalResource(url) {
    const pathname = url.pathname;
    return CRITICAL_RESOURCES.some(resource => pathname.endsWith(resource));
}

function isAPIRequest(url) {
    const pathname = url.pathname;
    return pathname.startsWith('/api/') && 
           CACHEABLE_API_ROUTES.some(route => pathname.startsWith(route));
}

function isStaticResource(url) {
    const pathname = url.pathname;
    return pathname.match(/\.(css|js|png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot|ico)$/);
}

function shouldExcludeFromCache(url) {
    const pathname = url.pathname;
    return CACHE_EXCLUSIONS.some(exclusion => pathname.includes(exclusion)) ||
           url.protocol !== 'http:' && url.protocol !== 'https:';
}

/*
 * MESSAGE HANDLING
 * Allow the main app to communicate with the service worker
 * Enables manual cache management and status reporting
 */
self.addEventListener('message', event => {
    const { type, payload } = event.data;
    
    switch (type) {
        case 'CACHE_SETLIST':
            cacheSetlistData(payload.setlistId);
            break;
            
        case 'CACHE_SONGS':
            cacheSongsData(payload.songs);
            break;
            
        case 'GET_CACHE_STATUS':
            getCacheStatus().then(status => {
                event.ports[0].postMessage({ type: 'CACHE_STATUS', payload: status });
            });
            break;
            
        case 'CLEAR_CACHE':
            clearAPICache();
            break;
            
        default:
            console.log('[SW] Unknown message type:', type);
    }
});

/*
 * MANUAL CACHE MANAGEMENT
 * Functions to allow the app to explicitly cache important data
 */
async function cacheSetlistData(setlistId) {
    try {
        const response = await fetch(`/api/setlists/${setlistId}`);
        if (response.ok) {
            const cache = await caches.open(API_CACHE_NAME);
            await cache.put(`/api/setlists/${setlistId}`, response);
            console.log('[SW] Manually cached setlist:', setlistId);
        }
    } catch (error) {
        console.error('[SW] Failed to cache setlist:', error);
    }
}

async function cacheSongsData(songs) {
    try {
        const cache = await caches.open(API_CACHE_NAME);
        
        for (const song of songs) {
            const response = await fetch(`/api/songs/${song.id}`);
            if (response.ok) {
                await cache.put(`/api/songs/${song.id}`, response);
            }
        }
        
        console.log('[SW] Manually cached songs:', songs.length);
    } catch (error) {
        console.error('[SW] Failed to cache songs:', error);
    }
}

async function getCacheStatus() {
    const cacheNames = await caches.keys();
    const status = {};
    
    for (const cacheName of cacheNames) {
        const cache = await caches.open(cacheName);
        const keys = await cache.keys();
        status[cacheName] = keys.length;
    }
    
    return status;
}

async function clearAPICache() {
    await caches.delete(API_CACHE_NAME);
    console.log('[SW] API cache cleared');
}

/*
 * BACKGROUND SYNC (for future enhancement)
 * Placeholder for offline data synchronization when connection returns
 */
self.addEventListener('sync', event => {
    console.log('[SW] Background sync triggered:', event.tag);
    
    if (event.tag === 'sync-offline-data') {
        event.waitUntil(syncOfflineData());
    }
});

async function syncOfflineData() {
    // Future: Implement background sync for offline changes
    console.log('[SW] Background sync not yet implemented');
}

/*
 * ERROR HANDLING
 * Global error handler for service worker issues
 */
self.addEventListener('error', event => {
    console.error('[SW] Service Worker error:', event.error);
});

self.addEventListener('unhandledrejection', event => {
    console.error('[SW] Unhandled promise rejection:', event.reason);
    event.preventDefault();
});

console.log('[SW] Service Worker loaded successfully - ready for offline performance support');