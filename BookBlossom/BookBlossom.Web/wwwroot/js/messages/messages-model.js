/**
 * BOOKBLOSSOM MESSAGES MODEL (MVC PATTERN)
 * Handles state, data queries, and API calls for the Messages module.
 */
class MessagesModel {
    constructor() {
        this.activeConversationId = null;
        this.tagBooksLoaded = false;
    }

    getCookie(name) {
        let matches = document.cookie.match(new RegExp("(?:^|; )" + name.replace(/([\.$?*|{}\(\)\[\]\\/\+^])/g, '\\$1') + "=([^;]*)"));
        return matches ? decodeURIComponent(matches[1]) : undefined;
    }

    isGuest() {
        return !this.getCookie("AuthToken");
    }

    async fetchConversations() {
        if (!window.apiClient) return null;
        try {
            return await window.apiClient.apiGet('/api/MessagesAPI/conversations');
        } catch (error) {
            console.error("Failed to fetch conversations:", error);
            throw error;
        }
    }

    async fetchMessages(conversationId) {
        if (!window.apiClient) return null;
        try {
            return await window.apiClient.apiGet('/api/MessagesAPI/conversations/' + conversationId);
        } catch (error) {
            console.error(`Failed to fetch messages for conversation ${conversationId}:`, error);
            throw error;
        }
    }

    async sendMessage(dto) {
        if (!window.apiClient) return null;
        try {
            return await window.apiClient.apiPost('/api/MessagesAPI', dto);
        } catch (error) {
            console.error("Failed to send message:", error);
            throw error;
        }
    }

    async fetchRecentBooks() {
        if (!window.apiClient) return [];
        try {
            const realBooksRes = await window.apiClient.apiGet('/api/RealBook?pageSize=50');
            const realBooks = Array.isArray(realBooksRes) ? realBooksRes : (realBooksRes?.data?.items || realBooksRes?.data || []);
            
            let blindBooks = [];
            try {
                const blindBooksRes = await window.apiClient.apiGet('/api/BlindBook?pageSize=50');
                blindBooks = Array.isArray(blindBooksRes) ? blindBooksRes : (blindBooksRes?.data?.items || blindBooksRes?.data || []);
            } catch (e) {
                console.warn("Failed to fetch recent blind books:", e);
            }
            
            return { realBooks, blindBooks };
        } catch (error) {
            console.error("Failed to fetch recent books:", error);
            throw error;
        }
    }

    async fetchWishlist() {
        if (!window.apiClient) return [];
        try {
            return await window.apiClient.apiGet('/api/Wishlist');
        } catch (error) {
            console.error("Failed to fetch wishlist:", error);
            throw error;
        }
    }

    async fetchBookDetails(bookId) {
        if (!window.apiClient) return null;
        try {
            return await window.apiClient.apiGet('/api/RealBook/' + bookId);
        } catch (error) {
            console.error(`Failed to fetch book details for ${bookId}:`, error);
            throw error;
        }
    }
}

// Attach to window namespace for global access
window.MessagesModel = MessagesModel;
