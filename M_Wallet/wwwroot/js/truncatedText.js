// Helper for TruncatedText component - detects text overflow and handles resize
window.truncatedTextHelper = {
    observers: new WeakMap(),
    
    init: function(element, dotNetRef) {
        if (!element || this.observers.has(element)) return;
        
        // Create a ResizeObserver to detect when the element's container resizes
        const observer = new ResizeObserver(() => {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnResize');
            }
        });
        
        // Observe the parent element for size changes
        if (element.parentElement) {
            observer.observe(element.parentElement);
        }
        
        this.observers.set(element, { observer, dotNetRef });
    },
    
    isOverflowing: function(element) {
        if (!element) return false;
        // Check if the text content is wider than the visible area
        return element.scrollWidth > element.clientWidth;
    },
    
    dispose: function(element) {
        if (!element) return;
        
        const data = this.observers.get(element);
        if (data) {
            data.observer.disconnect();
            this.observers.delete(element);
        }
    }
};
