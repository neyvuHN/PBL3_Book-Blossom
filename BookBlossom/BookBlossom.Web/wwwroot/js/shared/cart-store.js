(function (window) {
    const CART_CACHE_KEY = 'bookblossom_cart_cache';
    let cachedItems = [];

    // Initialize guest session implicitly if needed when cart loads
    if (window.apiClient) {
        window.apiClient.init();
    }

    function safeParse(json, fallback) {
        if (!json) return fallback;
        try {
            const parsed = JSON.parse(json);
            return parsed !== null ? parsed : fallback;
        } catch {
            return fallback;
        }
    }

    // Map Backend DTO to Frontend UI Model
    function mapApiItemToCartItem(apiItem) {
        // C# System.Text.Json uses camelCase by default (CartID -> cartId)
        const cartId = apiItem.cartId || apiItem.cartID || apiItem.CartID;
        const bookId = apiItem.bookId || apiItem.bookID || apiItem.BookID;
        const blindBookId = apiItem.blindBookId || apiItem.blindBookID || apiItem.BlindBookID;
        
        const isBlind = blindBookId != null;
        const priceVnd = apiItem.price; // Backend price is already in VND, no need to multiply by 20000
        
        return {
            id: cartId ? cartId.toString() : '0',
            title: apiItem.title || 'Unknown Book',
            shop: isBlind ? 'Blind Date Books' : 'Normal Books',
            price: apiItem.price,
            priceVnd: priceVnd,
            qty: apiItem.quantity,
            condition: isBlind ? 'New Curated' : 'Like New',
            // Default images since API doesn't provide them yet
            img: isBlind ? '/images/BlindDateBook/BlindBook.jpg' : '/images/Book/book1.jpg',
            selected: true, // Default to selected when loaded
            isBlind: isBlind,
            hashtags: isBlind ? ['Mystery'] : [],
            limit: null,
            author: 'BookBlossom Edition',
            bookID: bookId,
            blindBookID: blindBookId
        };
    }

    // Preserve selection state when reloading from server
    function mergeSelectionState(newItems) {
        newItems.forEach(newItem => {
            const existing = cachedItems.find(i => i.id === newItem.id);
            if (existing && existing.selected !== undefined) {
                newItem.selected = existing.selected;
            }
        });
        return newItems;
    }

    // Core async API methods
    async function loadCartFromServer() {
        if (!window.apiClient) {
            console.warn("apiClient not found. Cannot load cart.");
            return cachedItems;
        }
        try {
            const data = await window.apiClient.apiGet('/api/Cart');
            if (Array.isArray(data)) {
                let mapped = data.map(mapApiItemToCartItem);
                cachedItems = mergeSelectionState(mapped);
                saveCache(cachedItems);
                updateBadge();
            }
            return cachedItems;
        } catch (error) {
            console.error("Failed to load cart from server:", error);
            // Fallback to local cache if API fails
            return getCartItems();
        }
    }

    async function addToCart(payload) {
        if (!window.apiClient) return null;
        
        // Payload expects { bookID, blindBookID, quantity }
        const requestBody = {
            bookID: payload.bookID || null,
            blindBookID: payload.blindBookID || null,
            quantity: payload.qty || 1
        };

        try {
            await window.apiClient.apiPost('/api/Cart', requestBody);
            // Reload cart to sync state
            await loadCartFromServer();
            return cachedItems;
        } catch (error) {
            console.error("Failed to add to cart:", error);
            if (window.apiClient.showToast) {
                window.apiClient.showToast(error.message || "Failed to add to cart.", 'error');
            }
            throw error;
        }
    }

    async function updateQuantity(id, qty) {
        if (!window.apiClient) return null;
        
        try {
            await window.apiClient.apiPut(`/api/Cart/${id}/quantity`, { quantity: qty });
            await loadCartFromServer();
            return cachedItems;
        } catch (error) {
            console.error("Failed to update cart quantity:", error);
            if (window.apiClient.showToast) {
                window.apiClient.showToast(error.message || "Failed to update quantity.", 'error');
            }
            throw error;
        }
    }

    async function removeItem(id) {
        if (!window.apiClient) return null;
        
        try {
            await window.apiClient.apiDelete(`/api/Cart/${id}`);
            await loadCartFromServer();
            return cachedItems;
        } catch (error) {
            console.error("Failed to remove item from cart:", error);
            throw error;
        }
    }

    // Local state management for immediate UI reflection and selection
    function getCartItems() {
        if (!cachedItems || cachedItems.length === 0) {
            const raw = localStorage.getItem(CART_CACHE_KEY);
            cachedItems = safeParse(raw, []);
        }
        return cachedItems;
    }

    function saveCache(items) {
        cachedItems = items;
        localStorage.setItem(CART_CACHE_KEY, JSON.stringify(cachedItems));
        updateBadge();
    }

    function toggleSelected(id, selected) {
        const item = cachedItems.find(x => x.id === id);
        if (item) {
            item.selected = selected;
            saveCache(cachedItems);
        }
        return cachedItems;
    }

    function toggleShopSelected(shop, selected) {
        cachedItems.forEach(item => {
            if (item.shop === shop) {
                item.selected = selected;
            }
        });
        saveCache(cachedItems);
        return cachedItems;
    }

    function clearSelectedItems() {
        // Only updates local state. Backend checkout process should handle actual clearing.
        cachedItems = cachedItems.filter(x => !x.selected);
        saveCache(cachedItems);
        return cachedItems;
    }

    function getCartCount() {
        return getCartItems().reduce((total, item) => total + item.qty, 0);
    }

    function updateBadge() {
        const badge = document.getElementById('cart-badge');
        if (!badge) return;

        const count = getCartCount();
        if (count > 0) {
            badge.textContent = count;
            badge.style.display = '';
            badge.classList.remove('cart-badge-bounce');
            void badge.offsetWidth;
            badge.classList.add('cart-badge-bounce');
        } else {
            badge.style.display = 'none';
        }
    }

    // Expose API globally
    window.BookBlossomCart = {
        loadCartFromServer,
        getCartItems,
        addToCart,
        updateQuantity,
        removeItem,
        toggleSelected,
        toggleShopSelected,
        clearSelectedItems,
        getCartCount,
        updateBadge,
        // Fallback for codes expecting saveCartItems for selection state
        saveCartItems: saveCache
    };

    document.addEventListener('DOMContentLoaded', () => {
        // Immediately fetch from server on load to sync
        if (window.apiClient) {
            loadCartFromServer();
        } else {
            updateBadge();
        }
    });
})(window);