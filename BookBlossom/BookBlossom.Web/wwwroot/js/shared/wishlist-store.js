(function (window) {
    const MOCK_KEY = 'bookblossom_mock_wishlist';
    
    // In-memory cache for fast UI updates, updated from backend
    let cachedItems = [];
    
    function getMockItems() {
        try {
            return JSON.parse(localStorage.getItem(MOCK_KEY) || '[]');
        } catch { return []; }
    }
    
    function saveMockItems(items) {
        localStorage.setItem(MOCK_KEY, JSON.stringify(items));
    }

    // Map backend DTO to frontend format
    function normalizeItem(dto) {
        return {
            id: dto.wishlistID || dto.id,
            bookID: dto.bookID,
            blindBookID: dto.blindBookID,
            title: dto.title || 'Unknown Book',
            price: Number(dto.price) || 0,
            imageUrl: dto.imageUrl || (dto.blindBookID || dto.isBlindDate ? '/images/BlindDateBook/BlindBook.jpg' : '/images/Book/book1.jpg'),
            isBlindDate: dto.isBlindDate !== undefined ? dto.isBlindDate : !!dto.blindBookID,
            author: dto.author || (dto.blindBookID ? 'Unknown' : 'BookBlossom'), // Placeholder if backend doesn't return author
            hashtags: dto.hashtags || []
        };
    }

    async function getWishlistItems() {
        let apiItems = [];
        if (window.apiClient) {
            try {
                const data = await window.apiClient.apiGet('/api/Wishlist');
                apiItems = Array.isArray(data) ? data.map(normalizeItem) : [];
            } catch (error) {
                console.warn("Failed to fetch wishlist from API, using mock/cache only.", error);
            }
        }
        
        const mockItems = getMockItems().map(normalizeItem);
        
        // Merge without duplicates (by ID)
        const combined = [...apiItems];
        mockItems.forEach(mockItem => {
            if (!combined.some(i => i.id === mockItem.id || (mockItem.bookID && i.bookID === mockItem.bookID) || (mockItem.blindBookID && i.blindBookID === mockItem.blindBookID))) {
                combined.push(mockItem);
            }
        });
        
        cachedItems = combined;
        return cachedItems;
    }

    async function saveWishlistItems(items) {
        return cachedItems;
    }

    async function addToWishlist(item) {
        if (!window.apiClient) {
            const mocks = getMockItems();
            if(!mocks.find(i => i.id === item.id)) {
                mocks.push(item);
                saveMockItems(mocks);
            }
            await getWishlistItems();
            return cachedItems;
        }
        
        let isSuccess = false;
        try {
            const requestBody = {};
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

            await window.apiClient.apiPost('/api/Wishlist', requestBody);
            isSuccess = true;
        } catch (error) {
            console.warn("API rejected adding to wishlist. Falling back to local mock storage.", error);
        }

        if (!isSuccess) {
            const mocks = getMockItems();
            if(!mocks.find(i => i.id === item.id)) {
                mocks.push(item);
                saveMockItems(mocks);
            }
        }

        await getWishlistItems();
        return cachedItems;
    }

    async function removeItem(id) {
        const existing = cachedItems.find(i => String(i.id) === String(id) || String(i.bookID) === String(id) || String(i.blindBookID) === String(id) || `book-${i.bookID}` === String(id) || `blind-${i.blindBookID}` === String(id) || `tindbook-${i.bookID}` === String(id));
        
        let targetWishlistId = id;
        let removedFromApi = false;
        
        if (window.apiClient) {
            try {
                if (existing && existing.id && typeof existing.id === 'number') {
                    targetWishlistId = existing.id;
                } else if (existing && existing.wishlistID) {
                    targetWishlistId = existing.wishlistID;
                }
                
                if (typeof targetWishlistId === 'number' || !isNaN(parseInt(targetWishlistId))) {
                    await window.apiClient.apiDelete(`/api/Wishlist/${targetWishlistId}`);
                    removedFromApi = true;
                }
            } catch (error) {
                console.warn("API rejected removing from wishlist or not found in API.");
            }
        }

        let mocks = getMockItems();
        const initialLen = mocks.length;
        mocks = mocks.filter(i => String(i.id) !== String(id) && String(i.bookID) !== String(id) && String(i.blindBookID) !== String(id) && `book-${i.bookID}` !== String(id) && `blind-${i.blindBookID}` !== String(id) && `tindbook-${i.bookID}` !== String(id));
        if (mocks.length < initialLen) {
            saveMockItems(mocks);
            removedFromApi = true;
        }

        await getWishlistItems();
        return removedFromApi;
    }

    async function clearAll() {
        if (window.apiClient) {
            try {
                const apiItems = cachedItems.filter(i => typeof i.id === 'number');
                for (const item of apiItems) {
                    await window.apiClient.apiDelete(`/api/Wishlist/${item.id}`);
                }
            } catch (error) {
                console.error("Failed to clear wishlist via API", error);
            }
        }
        
        saveMockItems([]);
        await getWishlistItems();
        return true;
    }

    window.BookBlossomWishlist = {
        getWishlistItems,
        saveWishlistItems,
        addToWishlist,
        removeItem,
        clearAll,
        getCachedItems: () => cachedItems
    };
})(window);
