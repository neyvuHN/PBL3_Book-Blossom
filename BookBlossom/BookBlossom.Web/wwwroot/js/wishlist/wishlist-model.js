class WishlistModel {
    constructor() {
        this.items = [];
        this.isLoading = false;
    }

    async fetchWishlist() {
        this.isLoading = true;
        
        if (window.BookBlossomWishlist) {
            this.items = await window.BookBlossomWishlist.getWishlistItems();
        } else {
            console.error("BookBlossomWishlist store not found.");
            this.items = [];
        }

        this.isLoading = false;
        return this.items;
    }

    async removeItem(itemId) {
        if (window.BookBlossomWishlist) {
            const success = await window.BookBlossomWishlist.removeItem(itemId);
            if (success) {
                this.items = window.BookBlossomWishlist.getCachedItems();
            }
            return success;
        }
        return false;
    }

    async clearAll() {
        if (window.BookBlossomWishlist) {
            const success = await window.BookBlossomWishlist.clearAll();
            if (success) {
                this.items = window.BookBlossomWishlist.getCachedItems();
            }
            return success;
        }
        return false;
    }
}
