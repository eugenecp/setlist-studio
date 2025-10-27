// Setlist Studio App JavaScript
// Handles accessibility, theme management, and UI interactions

window.setlistStudioApp = {
    
    // Initialize app
    init: function() {
        this.setupAccessibility();
        this.setupKeyboardNavigation();
        this.setupReducedMotion();
        console.log('Setlist Studio app initialized');
    },
    
    // Accessibility helpers
    setupAccessibility: function() {
        // Announce route changes to screen readers
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                    const mainContent = document.querySelector('#main-content');
                    if (mainContent?.contains(mutation.addedNodes[0])) {
                        this.announcePageChange();
                    }
                }
            });
        });
        
        observer.observe(document.body, { 
            childList: true, 
            subtree: true 
        });
    },
    
    // Announce page changes for screen readers
    announcePageChange: function() {
        const title = document.title;
        const announcement = `Page changed to ${title}`;
        this.announceToScreenReader(announcement);
    },
    
    // Create live region for screen reader announcements
    announceToScreenReader: function(message) {
        let liveRegion = document.getElementById('sr-live-region');
        if (!liveRegion) {
            liveRegion = document.createElement('div');
            liveRegion.id = 'sr-live-region';
            liveRegion.setAttribute('aria-live', 'polite');
            liveRegion.setAttribute('aria-atomic', 'true');
            liveRegion.style.position = 'absolute';
            liveRegion.style.left = '-10000px';
            liveRegion.style.width = '1px';
            liveRegion.style.height = '1px';
            liveRegion.style.overflow = 'hidden';
            document.body.appendChild(liveRegion);
        }
        
        liveRegion.textContent = message;
        setTimeout(() => {
            liveRegion.textContent = '';
        }, 1000);
    },
    
    // Enhanced keyboard navigation
    setupKeyboardNavigation: function() {
        document.addEventListener('keydown', (e) => {
            // Skip to main content (Alt + M)
            if (e.altKey && e.key === 'm') {
                e.preventDefault();
                const mainContent = document.getElementById('main-content');
                if (mainContent) {
                    mainContent.focus();
                    this.announceToScreenReader('Jumped to main content');
                }
            }
            
            // Skip to navigation (Alt + N)
            if (e.altKey && e.key === 'n') {
                e.preventDefault();
                const nav = document.querySelector('nav[role="navigation"]');
                if (nav) {
                    nav.focus();
                    this.announceToScreenReader('Jumped to navigation');
                }
            }
        });
    },
    
    // Respect user's reduced motion preference
    setupReducedMotion: function() {
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
        
        if (reducedMotion.matches) {
            document.body.classList.add('reduced-motion');
        }
        
        reducedMotion.addEventListener('change', (e) => {
            if (e.matches) {
                document.body.classList.add('reduced-motion');
            } else {
                document.body.classList.remove('reduced-motion');
            }
        });
    },
    
    // Focus management helpers
    focusElement: function(selector) {
        const element = document.querySelector(selector);
        if (element) {
            element.focus();
            return true;
        }
        return false;
    },
    
    // Trap focus within modal dialogs
    trapFocus: function(element) {
        const focusableElements = element.querySelectorAll(
            'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
        );
        
        if (focusableElements.length === 0) return;
        
        const firstElement = focusableElements[0];
        const lastElement = focusableElements[focusableElements.length - 1];
        
        element.addEventListener('keydown', (e) => {
            if (e.key === 'Tab') {
                if (e.shiftKey) {
                    if (document.activeElement === firstElement) {
                        e.preventDefault();
                        lastElement.focus();
                    }
                } else if (document.activeElement === lastElement) {
                    e.preventDefault();
                    firstElement.focus();
                }
            }
            
            if (e.key === 'Escape') {
                this.closeModal();
            }
        });
        
        firstElement.focus();
    },
    
    // Modal helpers
    closeModal: function() {
        const modal = document.querySelector('.mud-dialog');
        if (modal) {
            const closeButton = modal.querySelector('[aria-label*="close"], [aria-label*="Close"]');
            if (closeButton) {
                closeButton.click();
            }
        }
    },
    
    // Drag and drop helpers with accessibility
    setupDragAndDrop: function(containerId, onReorder) {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        let draggedElement = null;
        
        container.addEventListener('dragstart', (e) => {
            draggedElement = e.target;
            e.target.style.opacity = '0.5';
        });
        
        container.addEventListener('dragend', (e) => {
            e.target.style.opacity = '';
            draggedElement = null;
        });
        
        container.addEventListener('dragover', (e) => {
            e.preventDefault();
        });
        
        container.addEventListener('drop', (e) => {
            e.preventDefault();
            
            if (draggedElement && e.target !== draggedElement) {
                const items = Array.from(container.children);
                const draggedIndex = items.indexOf(draggedElement);
                const targetIndex = items.indexOf(e.target);
                
                if (draggedIndex < targetIndex) {
                    e.target.parentNode.insertBefore(draggedElement, e.target.nextSibling);
                } else {
                    e.target.parentNode.insertBefore(draggedElement, e.target);
                }
                
                if (onReorder) {
                    onReorder();
                }
                
                this.announceToScreenReader('Items reordered');
            }
        });
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    window.setlistStudioApp.init();
});

// Blazor reconnection helpers

// Service Worker registration for offline performance support
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/service-worker.js')
        .then(registration => {
            console.log('[App] Service Worker registered - offline performance mode enabled');
            
            // Listen for updates to the service worker
            registration.addEventListener('updatefound', () => {
                console.log('[App] New Service Worker version available');
                const newWorker = registration.installing;
                
                if (newWorker) {
                    newWorker.addEventListener('statechange', () => {
                        if (newWorker.state === 'installed' && navigator.serviceWorker.controller) {
                            console.log('[App] New Service Worker installed - refresh recommended');
                            // Could show a notification to refresh the app
                        }
                    });
                }
            });
        })
        .catch(error => {
            console.error('[App] Service Worker registration failed:', error);
        });
    
    // Listen for service worker messages
    navigator.serviceWorker.addEventListener('message', event => {
        const { type, payload } = event.data;
        
        switch (type) {
            case 'CACHE_STATUS':
                console.log('[App] Cache status:', payload);
                break;
                
            case 'OFFLINE_READY':
                console.log('[App] Offline capabilities ready');
                window.setlistStudioApp.showOfflineReady?.();
                break;
                
            default:
                console.log('[App] Unknown service worker message:', type);
        }
    });
}

// Offline functionality for performance scenarios
window.setlistStudioApp.offline = {
    // Cache specific setlist for offline access
    cacheSetlist: function(setlistId) {
        if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({
                type: 'CACHE_SETLIST',
                payload: { setlistId }
            });
            console.log('[App] Requested offline caching for setlist:', setlistId);
        }
    },
    
    // Cache multiple songs for offline access  
    cacheSongs: function(songs) {
        if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({
                type: 'CACHE_SONGS', 
                payload: { songs }
            });
            console.log('[App] Requested offline caching for songs:', songs.length);
        }
    },
    
    // Get current cache status
    getCacheStatus: function() {
        return new Promise((resolve) => {
            if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
                const messageChannel = new MessageChannel();
                
                messageChannel.port1.onmessage = (event) => {
                    if (event.data.type === 'CACHE_STATUS') {
                        resolve(event.data.payload);
                    }
                };
                
                navigator.serviceWorker.controller.postMessage(
                    { type: 'GET_CACHE_STATUS' },
                    [messageChannel.port2]
                );
            } else {
                resolve({});
            }
        });
    },
    
    // Clear API cache (for troubleshooting)
    clearCache: function() {
        if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
            navigator.serviceWorker.controller.postMessage({ type: 'CLEAR_CACHE' });
            console.log('[App] Cache clear requested');
        }
    },
    
    // Check if app is currently offline
    isOffline: function() {
        return !navigator.onLine;
    },
    
    // Show offline notification to users
    showOfflineReady: function() {
        // This will be called when offline capabilities are ready
        // Can be hooked up to MudBlazor notifications later
        console.log('[App] Offline mode ready for performance use');
    }
};

// Connection status callbacks for Blazor components
window.setlistStudioApp.connectionStatusCallback = null;

window.setlistStudioApp.registerConnectionStatusCallback = function(dotNetRef) {
    window.setlistStudioApp.connectionStatusCallback = dotNetRef;
    console.log('[App] Connection status callback registered');
};

window.setlistStudioApp.unregisterConnectionStatusCallback = function() {
    window.setlistStudioApp.connectionStatusCallback = null;
    console.log('[App] Connection status callback unregistered');
};

// Connection status monitoring for live performance scenarios
window.addEventListener('online', () => {
    console.log('[App] Connection restored - sync mode enabled');
    document.body.classList.remove('offline-mode');
    document.body.classList.add('online-mode');
    
    // Notify Blazor component
    if (window.setlistStudioApp.connectionStatusCallback) {
        window.setlistStudioApp.connectionStatusCallback.invokeMethodAsync('OnConnectionStatusChanged', true);
    }
});

window.addEventListener('offline', () => {
    console.log('[App] Offline detected - performance mode activated');
    document.body.classList.remove('online-mode'); 
    document.body.classList.add('offline-mode');
    
    // Notify Blazor component
    if (window.setlistStudioApp.connectionStatusCallback) {
        window.setlistStudioApp.connectionStatusCallback.invokeMethodAsync('OnConnectionStatusChanged', false);
    }
});