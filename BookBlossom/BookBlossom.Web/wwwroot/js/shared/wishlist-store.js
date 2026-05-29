(function (window) {
    const WISHLIST_KEY = 'bookblossom_wishlist_items';
    const INIT_KEY = 'bookblossom_wishlist_initialized';

    const DEFAULT_WISHLIST_ITEMS = [
        {
            id: 'w1',
            title: 'The Great Gatsby',
            author: 'F. Scott Fitzgerald',
            price: 297000,
            imageUrl: '/images/Book/book1.jpg',
            isBlindDate: false
        },
        {
            id: 'w2',
            title: 'Mystery Thriller Blind Date',
            author: 'Unknown',
            price: 150000,
            imageUrl: '/images/BlindDateBook/BlindBook.jpg',
            isBlindDate: true,
            hashtags: ['Mystery', 'Thriller']
        },
        {
            id: 'w3',
            title: 'Pride and Prejudice',
            author: 'Jane Austen',
            price: 198000,
            imageUrl: '/images/Book/book1.jpg',
            isBlindDate: false
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
        item = item || {};

        return {
            id: item.id || ('wishlist-' + Date.now() + '-' + Math.floor(Math.random() * 10000)),
            title: item.title || 'Unknown Book',
            author: item.author || 'Unknown Author',
            price: Number(item.price) || 0,
            imageUrl: item.imageUrl || (item.isBlindDate ? '/images/BlindDateBook/BlindBook.jpg' : '/images/Book/book1.jpg'),
            isBlindDate: item.isBlindDate === true,
            hashtags: Array.isArray(item.hashtags) ? item.hashtags : []
        };
    }

    function getWishlistItems() {
        const initialized = localStorage.getItem(INIT_KEY);

        let shouldReset = !initialized;
        if (initialized) {
            const raw = localStorage.getItem(WISHLIST_KEY);
            const items = safeParse(raw, []);
            if (Array.isArray(items) && items.some(item => Number(item.price) < 1000)) {
                shouldReset = true;
            }
        }

        if (shouldReset) {
            localStorage.setItem(INIT_KEY, 'true');
            saveWishlistItems(DEFAULT_WISHLIST_ITEMS);
            return DEFAULT_WISHLIST_ITEMS.map(normalizeItem);
        }

        const raw = localStorage.getItem(WISHLIST_KEY);
        const items = safeParse(raw, []);

        if (!Array.isArray(items)) {
            return [];
        }

        return items.map(normalizeItem);
    }

    function saveWishlistItems(items) {
        const normalizedItems = Array.isArray(items) ? items.map(normalizeItem) : [];
        localStorage.setItem(WISHLIST_KEY, JSON.stringify(normalizedItems));
        return normalizedItems;
    }

    function addToWishlist(item) {
        const items = getWishlistItems();
        const newItem = normalizeItem(item);

        const existingItem = items.find(x =>
            x.title === newItem.title &&
            x.isBlindDate === newItem.isBlindDate
        );

        if (!existingItem) {
            items.push(newItem);
            return saveWishlistItems(items);
        }
        return items;
    }

    function removeItem(id) {
        const items = getWishlistItems().filter(x => x.id !== id);
        return saveWishlistItems(items);
    }

    function clearAll() {
        return saveWishlistItems([]);
    }

    window.BookBlossomWishlist = {
        getWishlistItems,
        saveWishlistItems,
        addToWishlist,
        removeItem,
        clearAll
    };
})(window);
