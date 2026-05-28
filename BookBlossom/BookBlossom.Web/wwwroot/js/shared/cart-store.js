(function (window) {
    const CART_KEY = 'bookblossom_cart_items';
    const INIT_KEY = 'bookblossom_cart_initialized';

    const DEFAULT_CART_ITEMS = [
        {
            id: 'cart-1',
            title: 'The Secret Life of Sunflowers',
            shop: 'Normal Books',
            price: 8.50,
            priceVnd: 170000,
            qty: 1,
            condition: 'Good 80%',
            img: '/images/Book/book1.jpg',
            selected: true,
            isBlind: false
        },
        {
            id: 'cart-2',
            title: 'Blind Date Mystery - Vibe: Cozy',
            shop: 'Blind Date Books',
            price: 12.00,
            priceVnd: 240000,
            qty: 1,
            condition: 'New Curated',
            img: '/images/BlindDateBook/BlindBook.jpg',
            selected: true,
            isBlind: true,
            hashtags: ['Romance', 'Dark']
        },
        {
            id: 'cart-3',
            title: 'The Silent Patient',
            shop: 'Normal Books',
            price: 14.00,
            priceVnd: 280000,
            qty: 1,
            condition: 'Like New',
            img: '/images/Book/book1.jpg',
            selected: true,
            isBlind: false
        },
        {
            id: 'cart-4',
            title: "Harry Potter and the Sorcerer's Stone",
            shop: 'Normal Books',
            price: 6.00,
            priceVnd: 120000,
            qty: 1,
            condition: 'Good 85%',
            img: '/images/Book/book1.jpg',
            selected: true,
            isBlind: false,
            limit: 1
        }
    ];

    function safeParse(json, fallback) {
        try {
            return JSON.parse(json);
        } catch {
            return fallback;
        }
    }

    function normalizeItem(item) {
        const priceVnd = Number(item.priceVnd) || Math.round((Number(item.price) || 0) * 20000);

        return {
            id: item.id || ('cart-' + Date.now() + '-' + Math.floor(Math.random() * 10000)),
            title: item.title || 'Unknown Book',
            shop: item.isBlind ? 'Blind Date Books' : (item.shop || 'Normal Books'),
            price: Number(item.price) || priceVnd / 20000,
            priceVnd: priceVnd,
            qty: Number(item.qty) || 1,
            condition: item.condition || (item.isBlind ? 'New Curated' : 'Like New'),
            img: item.img || (item.isBlind ? '/images/BlindDateBook/BlindBook.jpg' : '/images/Book/book1.jpg'),
            selected: item.selected !== false,
            isBlind: item.isBlind === true,
            hashtags: Array.isArray(item.hashtags) ? item.hashtags : [],
            limit: item.limit || null,
            author: item.author || ''
        };
    }

    function getCartItems() {
        const initialized = localStorage.getItem(INIT_KEY);

        if (!initialized) {
            localStorage.setItem(INIT_KEY, 'true');
            saveCartItems(DEFAULT_CART_ITEMS);
            return DEFAULT_CART_ITEMS.map(normalizeItem);
        }

        const raw = localStorage.getItem(CART_KEY);
        const items = safeParse(raw, []);

        if (!Array.isArray(items)) {
            return [];
        }

        return items.map(normalizeItem);
    }

    function saveCartItems(items) {
        const normalizedItems = Array.isArray(items) ? items.map(normalizeItem) : [];
        localStorage.setItem(CART_KEY, JSON.stringify(normalizedItems));
        updateBadge();
        return normalizedItems;
    }

    function addToCart(item) {
        const items = getCartItems();
        const newItem = normalizeItem(item);

        const existingItem = items.find(x =>
            x.title === newItem.title &&
            x.isBlind === newItem.isBlind
        );

        if (existingItem) {
            existingItem.qty += newItem.qty;
        } else {
            items.push(newItem);
        }

        return saveCartItems(items);
    }

    function removeItem(id) {
        const items = getCartItems().filter(x => x.id !== id);
        return saveCartItems(items);
    }

    function updateQuantity(id, qty) {
        const items = getCartItems();
        const item = items.find(x => x.id === id);

        if (item) {
            item.qty = Math.max(1, Number(qty) || 1);
        }

        return saveCartItems(items);
    }

    function toggleSelected(id, selected) {
        const items = getCartItems();
        const item = items.find(x => x.id === id);

        if (item) {
            item.selected = selected;
        }

        return saveCartItems(items);
    }

    function toggleShopSelected(shop, selected) {
        const items = getCartItems();

        items.forEach(item => {
            if (item.shop === shop) {
                item.selected = selected;
            }
        });

        return saveCartItems(items);
    }

    function clearSelectedItems() {
        const items = getCartItems().filter(x => !x.selected);
        return saveCartItems(items);
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

    window.BookBlossomCart = {
        getCartItems,
        saveCartItems,
        addToCart,
        removeItem,
        updateQuantity,
        toggleSelected,
        toggleShopSelected,
        clearSelectedItems,
        getCartCount,
        updateBadge
    };

    document.addEventListener('DOMContentLoaded', updateBadge);
})(window);