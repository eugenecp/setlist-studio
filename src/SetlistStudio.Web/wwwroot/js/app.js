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
window.Blazor?.reconnect();

// Service Worker registration for PWA support
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('/service-worker.js')
        .then(registration => {
            console.log('Service Worker registered successfully');
        })
        .catch(error => {
            console.log('Service Worker registration failed');
        });
}