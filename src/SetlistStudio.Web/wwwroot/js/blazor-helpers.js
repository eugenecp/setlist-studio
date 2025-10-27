// Blazor Helper Functions
// Safe JavaScript functions for Blazor interop without CSP violations

globalThis.blazorHelpers = {
    // Safe connection status check
    getConnectionStatus: function() {
        try {
            return navigator.onLine === true;
        } catch (error) {
            console.warn('[BlazorHelpers] Connection status check failed:', error.message);
            return true; // Assume online if check fails
        }
    },
    
    // Safe navigator.onLine access for Blazor
    isOnline: function() {
        try {
            return navigator.onLine;
        } catch (error) {
            console.warn('[BlazorHelpers] Online check failed:', error.message);
            return true;
        }
    },
    
    // Initialize mobile test without eval
    initializeMobileTest: function() {
        try {
            if (globalThis.mobileEnhancements) {
                globalThis.mobileEnhancements.initializeMobileTest();
                return true;
            } else {
                console.warn('[BlazorHelpers] MobileEnhancements not available');
                return false;
            }
        } catch (e) {
            console.error('[BlazorHelpers] Error initializing mobile test:', e);
            return false;
        }
    },
    
    // Safe event listener additions
    addSwipeListener: function() {
        try {
            if (globalThis.mobileEnhancements?.addSwipeListener) {
                globalThis.mobileEnhancements.addSwipeListener();
            } else {
                console.log('[BlazorHelpers] Adding basic swipe listener');
                // Add basic swipe listener without mobileEnhancements
                document.addEventListener('swipe', function(e) {
                    if (globalThis.DotNet) {
                        globalThis.DotNet.invokeMethodAsync('SetlistStudio.Web', 'HandleSwipeGesture', e.detail.direction);
                    }
                });
            }
        } catch (e) {
            console.error('[BlazorHelpers] Error adding swipe listener:', e);
        }
    },
    
    addPullRefreshListener: function() {
        try {
            document.addEventListener('pullRefresh', function(e) {
                if (globalThis.DotNet) {
                    globalThis.DotNet.invokeMethodAsync('SetlistStudio.Web', 'HandlePullRefresh');
                }
            });
        } catch (e) {
            console.error('[BlazorHelpers] Error adding pull refresh listener:', e);
        }
    },
    
    addTouchCountListener: function() {
        try {
            document.addEventListener('touchstart', function(e) {
                if (globalThis.DotNet) {
                    globalThis.DotNet.invokeMethodAsync('SetlistStudio.Web', 'IncrementTouchCount');
                }
            });
        } catch (e) {
            console.error('[BlazorHelpers] Error adding touch count listener:', e);
        }
    },
    
    // Show user feedback
    showFeedback: function(message) {
        try {
            // Create a simple toast notification
            const toast = document.createElement('div');
            toast.textContent = message;
            toast.style.cssText = `
                position: fixed;
                bottom: 20px;
                left: 50%;
                transform: translateX(-50%);
                background: #333;
                color: white;
                padding: 12px 24px;
                border-radius: 8px;
                z-index: 9999;
                font-size: 14px;
                box-shadow: 0 4px 12px rgba(0,0,0,0.3);
            `;
            
            document.body.appendChild(toast);
            
            // Auto-remove after 3 seconds
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.remove();
                }
            }, 3000);
            
            return true;
        } catch (e) {
            console.error('[BlazorHelpers] Error showing feedback:', e);
            return false;
        }
    }
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        console.log('[BlazorHelpers] Initialized');
    });
} else {
    console.log('[BlazorHelpers] Initialized');
}