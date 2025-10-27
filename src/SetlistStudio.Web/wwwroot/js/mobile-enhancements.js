// Mobile Enhancements for Setlist Studio
// Gesture navigation and touch interactions for musicians

class MobileEnhancements {
    constructor() {
        this.isTouch = 'ontouchstart' in globalThis;
        this.gestureThreshold = 50;
        this.swipeThreshold = 100;
        this.pullRefreshThreshold = 80;
        
        this.init();
    }
    
    init() {
        this.addTouchTargetEnhancements();
        this.initializeGestureNavigation();
        this.initializePullToRefresh();
        this.initializeFloatingActionButton();
        this.initializePerformanceMode();
        this.addHapticFeedback();
        this.optimizeForMobile();
        
        // Listen for orientation changes
        globalThis.addEventListener('orientationchange', () => {
            setTimeout(() => this.handleOrientationChange(), 100);
        });
        
        // Listen for connection changes
        globalThis.addEventListener('online', () => this.handleConnectionChange(true));
        globalThis.addEventListener('offline', () => this.handleConnectionChange(false));
    }
    
    // ===== TOUCH TARGET ENHANCEMENTS =====
    
    addTouchTargetEnhancements() {
        if (!this.isTouch) return;
        
        // Add mobile-touch-target class to interactive elements
        const interactiveElements = document.querySelectorAll('button, .mud-button, .mud-icon-button, .mud-nav-link, input, select');
        for (const element of interactiveElements) {
            element.classList.add('mobile-touch-target');
        }
        
        // Add click delay removal for iOS
        document.addEventListener('touchstart', () => {}, { passive: true });
    }
    
    // ===== GESTURE NAVIGATION =====
    
    initializeGestureNavigation() {
        if (!this.isTouch) return;
        
        let startX = 0;
        let startY = 0;
        let currentX = 0;
        let currentY = 0;
        let isSwiping = false;
        
        document.addEventListener('touchstart', (e) => {
            if (e.touches.length === 1) {
                startX = e.touches[0].clientX;
                startY = e.touches[0].clientY;
                isSwiping = false;
            }
        }, { passive: true });
        
        document.addEventListener('touchmove', (e) => {
            if (e.touches.length === 1 && !isSwiping) {
                currentX = e.touches[0].clientX;
                currentY = e.touches[0].clientY;
                
                const deltaX = Math.abs(currentX - startX);
                const deltaY = Math.abs(currentY - startY);
                
                if (deltaX > this.gestureThreshold && deltaX > deltaY) {
                    this.handleSwipeGesture(startX, currentX, e.target);
                    isSwiping = true;
                }
            }
        }, { passive: true });
        
        document.addEventListener('touchend', () => {
            isSwiping = false;
        }, { passive: true });
    }
    
    handleSwipeGesture(startX, endX, target) {
        const distance = endX - startX;
        const swipeContainer = target.closest('.swipe-container');
        
        if (!swipeContainer) return;
        
        if (Math.abs(distance) > this.swipeThreshold) {
            if (distance > 0) {
                // Swipe right
                this.triggerSwipeAction(swipeContainer, 'right');
            } else {
                // Swipe left
                this.triggerSwipeAction(swipeContainer, 'left');
            }
        }
    }
    
    triggerSwipeAction(container, direction) {
        const event = new CustomEvent('swipe', {
            detail: { direction, container }
        });
        container.dispatchEvent(event);
        
        // Visual feedback
        this.showSwipeAnimation(container, direction);
        this.triggerHapticFeedback('light');
    }
    
    showSwipeAnimation(container, direction) {
        const item = container.querySelector('.swipe-item');
        if (item) {
            item.style.transform = direction === 'right' ? 'translateX(20px)' : 'translateX(-20px)';
            setTimeout(() => {
                item.style.transform = 'translateX(0)';
            }, 200);
        }
    }
    
    // ===== PULL TO REFRESH =====
    
    initializePullToRefresh() {
        const refreshContainers = document.querySelectorAll('.pull-refresh-container');
        
        for (const container of refreshContainers) {
            let startY = 0;
            let currentY = 0;
            let isPulling = false;
            let indicator = container.querySelector('.pull-refresh-indicator');
            
            if (!indicator) {
                indicator = this.createPullRefreshIndicator();
                container.appendChild(indicator);
            }
            
            container.addEventListener('touchstart', (e) => {
                if (container.scrollTop === 0) {
                    startY = e.touches[0].clientY;
                    isPulling = true;
                }
            }, { passive: true });
            
            container.addEventListener('touchmove', (e) => {
                if (isPulling && container.scrollTop === 0) {
                    currentY = e.touches[0].clientY;
                    const pullDistance = currentY - startY;
                    
                    if (pullDistance > 0) {
                        e.preventDefault();
                        this.updatePullIndicator(indicator, pullDistance);
                    }
                }
            });
            
            container.addEventListener('touchend', (e) => {
                if (isPulling) {
                    const pullDistance = currentY - startY;
                    if (pullDistance > this.pullRefreshThreshold) {
                        this.triggerPullRefresh(container, indicator);
                    } else {
                        this.resetPullIndicator(indicator);
                    }
                    isPulling = false;
                }
            }, { passive: true });
        }
    }
    
    createPullRefreshIndicator() {
        const indicator = document.createElement('div');
        indicator.className = 'pull-refresh-indicator';
        indicator.innerHTML = '↓';
        return indicator;
    }
    
    updatePullIndicator(indicator, distance) {
        const progress = Math.min(distance / this.pullRefreshThreshold, 1);
        indicator.style.transform = `translateY(${Math.min(distance * 0.5, 60)}px)`;
        indicator.style.opacity = progress;
        
        if (progress >= 1) {
            indicator.innerHTML = '↻';
            indicator.classList.add('visible');
        } else {
            indicator.innerHTML = '↓';
            indicator.classList.remove('visible');
        }
    }
    
    triggerPullRefresh(container, indicator) {
        indicator.classList.add('loading', 'visible');
        indicator.innerHTML = '↻';
        
        const event = new CustomEvent('pullRefresh', { detail: { container } });
        container.dispatchEvent(event);
        
        this.triggerHapticFeedback('medium');
        
        // Reset after 2 seconds (or when refresh completes)
        setTimeout(() => this.resetPullIndicator(indicator), 2000);
    }
    
    resetPullIndicator(indicator) {
        indicator.classList.remove('loading', 'visible');
        indicator.style.transform = 'translateY(-60px)';
        indicator.style.opacity = '0';
        indicator.innerHTML = '↓';
    }
    
    // ===== FLOATING ACTION BUTTON =====
    
    initializeFloatingActionButton() {
        const fabContainer = document.querySelector('.fab-container');
        if (!fabContainer) {
            this.createFloatingActionButton();
        }
        
        document.addEventListener('click', (e) => {
            if (e.target.closest('.fab-main')) {
                this.toggleFAB();
            }
        });
    }
    
    createFloatingActionButton() {
        if (!document.querySelector('.fab-container')) {
            const fabHtml = `
                <div class="fab-container" id="fabContainer">
                    <button class="fab-secondary" onclick="MobileEnhancements.quickAction('search')" title="Search">
                        🔍
                    </button>
                    <button class="fab-secondary" onclick="MobileEnhancements.quickAction('add')" title="Add Song">
                        ➕
                    </button>
                    <button class="fab-secondary" onclick="MobileEnhancements.quickAction('setlist')" title="New Setlist">
                        📝
                    </button>
                    <button class="fab-main" onclick="MobileEnhancements.toggleFAB()" title="Quick Actions">
                        🎵
                    </button>
                </div>
            `;
            document.body.insertAdjacentHTML('beforeend', fabHtml);
        }
    }
    
    static toggleFAB() {
        const container = document.getElementById('fabContainer');
        if (container) {
            container.classList.toggle('expanded');
            // Haptic feedback for successful actions
            globalThis.mobileEnhancements?.triggerHapticFeedback('light');
        }
    }
    
    static quickAction(action) {
        const event = new CustomEvent('quickAction', { detail: { action } });
        document.dispatchEvent(event);
        
        // Navigate based on action
        switch (action) {
            case 'search':
                globalThis.location.href = '/songs?focus=search';
                break;
            case 'add':
                globalThis.location.href = '/songs/add';
                break;
            case 'create':
                globalThis.location.href = '/setlists/create';
                break;
        }
        
        globalThis.mobileEnhancements?.triggerHapticFeedback('medium');
        MobileEnhancements.toggleFAB();
    }
    
    // ===== PERFORMANCE MODE =====
    
    initializePerformanceMode() {
        // Auto-enable performance mode when offline
        if (!this.checkConnectionStatus()) {
            this.enablePerformanceMode();
        }
        
        // Add performance mode toggle
        this.addPerformanceModeToggle();
    }
    
    enablePerformanceMode() {
        document.body.classList.add('performance-mode', 'performance-optimized');
        document.documentElement.style.setProperty('--performance-mode', '1');
        
        // Store preference
        localStorage.setItem('performanceMode', 'enabled');
        
        this.showPerformanceModeNotification('Performance Mode Enabled');
    }
    
    disablePerformanceMode() {
        document.body.classList.remove('performance-mode', 'performance-optimized');
        document.documentElement.style.removeProperty('--performance-mode');
        
        localStorage.setItem('performanceMode', 'disabled');
        
        this.showPerformanceModeNotification('Performance Mode Disabled');
    }
    
    addPerformanceModeToggle() {
        // Check if already exists
        if (document.querySelector('#performanceModeToggle')) return;
        
        const toggle = document.createElement('button');
        toggle.id = 'performanceModeToggle';
        toggle.className = 'performance-mode-toggle';
        toggle.innerHTML = '🎭';
        toggle.title = 'Toggle Performance Mode';
        toggle.style.cssText = `
            position: fixed;
            top: 60px;
            right: 20px;
            width: 44px;
            height: 44px;
            border-radius: 50%;
            background: rgba(0, 0, 0, 0.7);
            color: white;
            border: none;
            font-size: 1.2rem;
            cursor: pointer;
            z-index: 1001;
            transition: all 0.2s ease;
        `;
        
        toggle.addEventListener('click', () => {
            const isPerformanceMode = document.body.classList.contains('performance-mode');
            if (isPerformanceMode) {
                this.disablePerformanceMode();
            } else {
                this.enablePerformanceMode();
            }
        });
        
        document.body.appendChild(toggle);
    }
    
    showPerformanceModeNotification(message) {
        const notification = document.createElement('div');
        notification.className = 'performance-notification';
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            left: 50%;
            transform: translateX(-50%);
            background: rgba(0, 0, 0, 0.8);
            color: white;
            padding: 12px 24px;
            border-radius: 20px;
            font-size: 0.9rem;
            font-weight: 500;
            z-index: 9999;
            animation: slideIn 0.3s ease;
        `;
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.style.animation = 'slideOut 0.3s ease';
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }
    
    // ===== HAPTIC FEEDBACK =====
    
    addHapticFeedback() {
        // Add to all interactive elements
        const elements = document.querySelectorAll('button, .mud-button, .mud-icon-button');
        for (const element of elements) {
            element.addEventListener('touchstart', () => {
                this.triggerHapticFeedback('light');
            }, { passive: true });
        }
    }
    
    triggerHapticFeedback(intensity = 'light') {
        // Use native haptic feedback if available
        if (globalThis.navigator.vibrate) {
            const patterns = {
                light: [10],
                medium: [15],
                heavy: [25],
                success: [10, 50, 10],
                error: [50, 50, 50]
            };
            globalThis.navigator.vibrate(patterns[intensity] || patterns.light);
        } else {
            // Visual feedback fallback
            document.body.classList.add('haptic-feedback');
            setTimeout(() => document.body.classList.remove('haptic-feedback'), 100);
        }
    }
    
    // ===== CONNECTION HANDLING =====
    
    handleConnectionChange(isOnline) {
        const statusElement = document.querySelector('.connection-status-mobile');
        if (statusElement) {
            statusElement.className = `connection-status-mobile ${isOnline ? 'online' : 'offline'} show`;
            statusElement.textContent = isOnline ? 'Online' : 'Offline';
        }
        
        if (isOnline === false) {
            this.enablePerformanceMode();
            this.showConnectionNotification('Switched to Performance Mode (Offline)');
        } else {
            this.showConnectionNotification('Back Online');
        }
        
        // Auto-hide status after 3 seconds
        setTimeout(() => {
            if (statusElement) {
                statusElement.classList.remove('show');
            }
        }, 3000);
    }
    
    showConnectionNotification(message) {
        const notification = document.createElement('div');
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            bottom: 80px;
            left: 50%;
            transform: translateX(-50%);
            background: #ff6b35;
            color: white;
            padding: 12px 20px;
            border-radius: 20px;
            font-weight: 500;
            z-index: 9999;
            animation: slideUp 0.3s ease;
        `;
        
        document.body.appendChild(notification);
        setTimeout(() => notification.remove(), 3000);
    }
    
    // ===== ORIENTATION HANDLING =====
    
    handleOrientationChange() {
        // Adjust layout based on orientation
        const isLandscape = window.innerWidth > window.innerHeight;
        document.body.classList.toggle('landscape-mode', isLandscape);
        
        // Show/hide quick access toolbar for landscape tablets
        const toolbar = document.querySelector('.quick-access-toolbar');
        if (isLandscape && window.innerWidth <= 1024) {
            if (!toolbar) {
                this.createQuickAccessToolbar();
            }
        } else if (toolbar) {
            toolbar.remove();
        }
        
        // Trigger layout recalculation
        setTimeout(() => {
            globalThis.dispatchEvent(new Event('resize'));
        }, 100);
    }
    
    createQuickAccessToolbar() {
        const toolbar = document.createElement('div');
        toolbar.className = 'quick-access-toolbar';
        toolbar.innerHTML = `
            <button onclick="history.back()" title="Back">←</button>
            <button onclick="location.href='/songs'" title="Songs">♪</button>
            <button onclick="location.href='/setlists'" title="Setlists">📝</button>
            <button onclick="MobileEnhancements.quickAction('search')" title="Search">🔍</button>
            <button onclick="MobileEnhancements.togglePerformanceMode()" title="Performance Mode">🎭</button>
        `;
        document.body.appendChild(toolbar);
    }
    
    static togglePerformanceMode() {
        const isPerformanceMode = document.body.classList.contains('performance-mode');
        if (isPerformanceMode) {
            globalThis.mobileEnhancements?.disablePerformanceMode();
        } else {
            globalThis.mobileEnhancements?.enablePerformanceMode();
        }
    }
    
    // ===== MOBILE OPTIMIZATIONS =====
    
    optimizeForMobile() {
        // Prevent zoom on input focus (iOS)
        const inputs = document.querySelectorAll('input, select, textarea');
        for (const input of inputs) {
            if (!input.style.fontSize) {
                input.style.fontSize = '16px';
            }
        }
        
        // Optimize scroll performance
        document.body.style.webkitOverflowScrolling = 'touch';
        
        // Add safe area support
        const safeAreaElements = document.querySelectorAll('.safe-area-container, .fab-container');
        for (const element of safeAreaElements) {
            element.classList.add('safe-area-container');
        }
        
        // Load performance preferences
        const performanceMode = localStorage.getItem('performanceMode');
        if (performanceMode === 'enabled') {
            this.enablePerformanceMode();
        }
    }
    
    // ===== MOBILE TEST FUNCTIONS =====
    
    addSwipeListener() {
        document.addEventListener('swipe', function(e) {
            if (globalThis.DotNet) {
                globalThis.DotNet.invokeMethodAsync('SetlistStudio.Web', 'HandleSwipeGesture', e.detail.direction);
            }
        });
    }
    
    addPullRefreshListener() {
        document.addEventListener('pullRefresh', function(e) {
            if (globalThis.DotNet) {
                globalThis.DotNet.invokeMethodAsync('SetlistStudio.Web', 'HandlePullRefresh');
            }
        });
    }
    
    addTouchCountListener() {
        document.addEventListener('touchstart', function(e) {
            if (globalThis.DotNet) {
                globalThis.DotNet.invokeMethodAsync('SetlistStudio.Web', 'IncrementTouchCount');
            }
        });
    }
    
    // Connection status check without using navigator.onLine property directly
    checkConnectionStatus() {
        try {
            return navigator.onLine !== false;
        } catch (error) {
            console.warn('[MobileEnhancements] Connection status check failed:', error.message);
            return true; // Assume online if check fails
        }
    }
    
    // Initialize mobile test events - separate function for mobile test page
    initializeMobileTest() {
        try {
            // Add event listeners for mobile test interactions
            this.addMobileTestListeners();
            console.log('[MobileEnhancements] Mobile test initialized');
        } catch (e) {
            console.error('[MobileEnhancements] Error initializing mobile test:', e);
        }
    }
    
    addMobileTestListeners() {
        // Add swipe event listener
        document.addEventListener('touchstart', this.handleTouchStart.bind(this), { passive: true });
        document.addEventListener('touchmove', this.handleTouchMove.bind(this), { passive: true });
        document.addEventListener('touchend', this.handleTouchEnd.bind(this), { passive: true });
    }
    
    handleTouchStart(e) {
        this.touchStartX = e.touches[0].clientX;
        this.touchStartY = e.touches[0].clientY;
        this.touchStartTime = Date.now();
    }
    
    handleTouchMove(e) {
        if (!this.touchStartX || !this.touchStartY) return;
        
        const touchEndX = e.touches[0].clientX;
        const touchEndY = e.touches[0].clientY;
        const diffX = this.touchStartX - touchEndX;
        const diffY = this.touchStartY - touchEndY;
        
        // Update for pull-to-refresh
        if (diffY < -this.pullRefreshThreshold && Math.abs(diffX) < 100) {
            this.showPullRefreshIndicator();
        }
    }
    
    handleTouchEnd(e) {
        if (!this.touchStartX || !this.touchStartY) return;
        
        const touchEndX = e.changedTouches[0].clientX;
        const touchEndY = e.changedTouches[0].clientY;
        const diffX = this.touchStartX - touchEndX;
        const diffY = this.touchStartY - touchEndY;
        const diffTime = Date.now() - this.touchStartTime;
        
        // Check for swipe gesture
        if (Math.abs(diffX) > this.swipeThreshold && Math.abs(diffY) < 100 && diffTime < 500) {
            const direction = diffX > 0 ? 'left' : 'right';
            this.dispatchSwipeEvent(direction);
        }
        
        // Check for pull-to-refresh
        if (diffY < -this.pullRefreshThreshold && Math.abs(diffX) < 100 && diffTime < 1000) {
            this.dispatchPullRefreshEvent();
        }
        
        // Reset touch data
        this.touchStartX = null;
        this.touchStartY = null;
        this.touchStartTime = null;
    }
    
    dispatchSwipeEvent(direction) {
        const swipeEvent = new CustomEvent('swipe', { detail: { direction } });
        document.dispatchEvent(swipeEvent);
    }
    
    dispatchPullRefreshEvent() {
        const pullRefreshEvent = new CustomEvent('pullRefresh');
        document.dispatchEvent(pullRefreshEvent);
    }
    
    showPullRefreshIndicator() {
        const indicator = document.querySelector('.pull-refresh-indicator');
        if (indicator) {
            indicator.style.transform = 'translateY(0)';
            indicator.style.opacity = '1';
        }
    }
}

// Animation keyframes (add to CSS)
const animationCSS = `
    @keyframes slideIn {
        from { transform: translateX(-50%) translateY(-100%); opacity: 0; }
        to { transform: translateX(-50%) translateY(0); opacity: 1; }
    }
    @keyframes slideOut {
        from { transform: translateX(-50%) translateY(0); opacity: 1; }
        to { transform: translateX(-50%) translateY(-100%); opacity: 0; }
    }
    @keyframes slideUp {
        from { transform: translateX(-50%) translateY(100%); opacity: 0; }
        to { transform: translateX(-50%) translateY(0); opacity: 1; }
    }
`;

// Add CSS to document
const style = document.createElement('style');
style.textContent = animationCSS;
document.head.appendChild(style);

// Initialize mobile enhancements when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        globalThis.mobileEnhancements = new MobileEnhancements();
    });
} else {
    globalThis.mobileEnhancements = new MobileEnhancements();
}

// Export for use in Blazor components
globalThis.MobileEnhancements = MobileEnhancements;