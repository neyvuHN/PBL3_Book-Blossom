class WishlistModel {
    constructor() {
        this.items = [];
        this.isLoading = false;
    }

    async fetchWishlist() {
        this.isLoading = true;
        // Simulate slight network delay
        await new Promise(resolve => setTimeout(resolve, 300));
        
        if (window.BookBlossomWishlist) {
            this.items = window.BookBlossomWishlist.getWishlistItems();
        } else {
            console.error("BookBlossomWishlist store not found.");
            this.items = [];
        }

        this.isLoading = false;
        return this.items;
    }

    async removeItem(itemId) {
        if (window.BookBlossomWishlist) {
            this.items = window.BookBlossomWishlist.removeItem(itemId);
            return true;
        }
        return false;
    }

    async clearAll() {
        if (window.BookBlossomWishlist) {
            this.items = window.BookBlossomWishlist.clearAll();
            return true;
        }
        return false;
    }
}
