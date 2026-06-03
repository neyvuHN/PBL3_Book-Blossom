class WishlistController {
    constructor(model, view) {
        this.model = model;
        this.view = view;

        // Bind view events to controller methods
        this.view.bindRemoveItem(this.handleRemoveItem.bind(this));
        this.view.bindClearAll(this.handleClearAll.bind(this));
        this.view.bindAddToCart(this.handleAddToCart.bind(this));
        this.view.bindNavigateToDetails(this.handleNavigateToDetails.bind(this));
    }

    async init() {
        this.view.showLoading();
        try {
            const items = await this.model.fetchWishlist();
            this.view.render(items);
        } catch (error) {
            console.error("Failed to load wishlist", error);
            this.view.hideLoading();
            // Could show error state in view
        }
    }

    async handleRemoveItem(id) {
        try {
            const success = await this.model.removeItem(id);
            if (success) {
                this.view.render(this.model.items);
            }
        } catch (error) {
            console.error("Failed to remove item", error);
            showToast("Failed to remove item. Please try again.", "error");
        }
    }

    async handleClearAll() {
        try {
            const success = await this.model.clearAll();
            if (success) {
                this.view.render(this.model.items);
            }
        } catch (error) {
            console.error("Failed to clear wishlist", error);
            showToast("Failed to clear wishlist. Please try again.", "error");
        }
    }

    async handleAddToCart(id) {
        // Find item
        const item = this.model.items.find(i => String(i.id) === String(id));
        if (item) {
            console.log("Adding to cart:", item);
            try {
                if (window.apiClient) {
                    const requestBody = { quantity: 1 };
                    if (item.isBlindDate) {
                        requestBody.blindBookID = item.blindBookID || item.id;
                        if (typeof requestBody.blindBookID === 'string') {
                            const parsed = parseInt(requestBody.blindBookID.replace(/\D/g, ''));
                            if (!isNaN(parsed) && parsed > 0) requestBody.blindBookID = parsed;
                            else requestBody.blindBookID = 1; 
                        }
                    } else {
                        requestBody.bookID = item.bookID || item.id;
                        if (typeof requestBody.bookID === 'string') {
                            const parsed = parseInt(requestBody.bookID.replace(/\D/g, ''));
                            if (!isNaN(parsed) && parsed > 0) requestBody.bookID = parsed;
                            else requestBody.bookID = 1; 
                        }
                    }
                    
                    await window.apiClient.apiPost('/api/Cart', requestBody);
                    showToast(`Added "${item.title}" to cart!`, 'success');
                    
                    if (window.BookBlossomLayout) {
                        window.BookBlossomLayout.refreshCartBadge();
                    }
                } else if (window.BookBlossomCart) {
                    showToast(`Added "${item.title}" to cart!`, 'success');
                    window.BookBlossomCart.addToCart({
                        bookID: item.isBlindDate ? null : (item.bookID || parseInt(String(item.id).replace(/\D/g, '')) || 1),
                        blindBookID: item.isBlindDate ? (item.blindBookID || parseInt(String(item.id).replace(/\D/g, '')) || 1) : null,
                        qty: 1
                    }).catch(err => {
                        console.error('Failed background add to cart', err);
                    });
                } else {
                    showToast(`Added "${item.title}" to cart (simulated)!`, 'success');
                }
            } catch (error) {
                console.error("Failed to add to cart", error);
                showToast('Failed to add item to cart.', 'error');
            }
        } else {
            console.error("Could not find item with id", id);
        }
    }

    handleNavigateToDetails(id, isBlind) {
        const item = this.model.items.find(i => String(i.id) === String(id));
        if (item) {
            if (isBlind) {
                const idToUse = item.blindBookID || item.id;
                if (typeof idToUse === 'number' || (typeof idToUse === 'string' && !idToUse.includes('blind-') && !idToUse.includes('book-'))) {
                    window.location.href = `/BlindDate#blind-details-${idToUse}`;
                } else {
                    const tags = item.hashtags && item.hashtags.length
                        ? item.hashtags.join('-')
                        : 'Mystery';
                    window.location.href = `/BlindDate#blind-details-${encodeURIComponent(tags)}`;
                }
            } else {
                const idToUse = item.bookID || item.id;
                if (typeof idToUse === 'number' || (typeof idToUse === 'string' && !idToUse.includes('blind-') && !idToUse.includes('book-'))) {
                    window.location.href = `/Explore#book-details-${idToUse}`;
                } else {
                    window.location.href = `/Explore#book-details-${encodeURIComponent(item.title)}`;
                }
            }
        }
    }
}

// Initialize on DOM load
document.addEventListener('DOMContentLoaded', () => {
    // Only initialize if we're on the wishlist page
    if (document.getElementById('wishlist-section')) {
        const model = new WishlistModel();
        const view = new WishlistView();
        const controller = new WishlistController(model, view);
        
        controller.init();
    }
});

// Premium Custom Toast Notification System Helper
function showToast(message, type = 'success', title = '') {
    let container = document.getElementById('toastContainer');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }

    const toast = document.createElement('div');
    toast.className = `custom-toast ${type}`;

    let iconSvg = '';
    if (type === 'success') {
        iconSvg = `
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="#EBBAB9" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>`;
        if (!title) title = 'Success';
    } else if (type === 'error') {
        iconSvg = `
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="#EA4335" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>`;
        if (!title) title = 'Error';
    } else {
        iconSvg = `
            <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="#4285F4" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>`;
        if (!title) title = 'Info';
    }

    toast.innerHTML = `
        <div class="toast-icon">${iconSvg}</div>
        <div class="toast-content">
            <div class="toast-title">${title}</div>
            <div class="toast-message">${message}</div>
        </div>
        <button class="toast-close" type="button">&times;</button>
        <div class="toast-timer-bar">
            <div class="toast-timer-fill"></div>
        </div>
    `;

    container.appendChild(toast);

    // Trigger transition
    setTimeout(() => {
        toast.classList.add('active');
        const fill = toast.querySelector('.toast-timer-fill');
        if (fill) fill.style.width = '0%';
    }, 10);

    // Close logic
    const closeBtn = toast.querySelector('.toast-close');
    const dismissToast = () => {
        toast.classList.remove('active');
        setTimeout(() => toast.remove(), 400);
    };

    closeBtn.addEventListener('click', dismissToast);

    // Auto remove after 4 seconds
    setTimeout(dismissToast, 4000);
}
