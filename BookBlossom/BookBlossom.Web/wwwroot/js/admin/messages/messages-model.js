/**
 * BOOKBLOSSOM ADMIN MESSAGES MODEL (MVC PATTERN)
 * Manages the state, data queries, and API communications for the Admin Messages panel.
 */
class AdminMessagesModel {
    constructor() {
        this.activeConversationId = null;
        this.activeSupportRequestId = null;
        this.conversations = [];
        this.supportRequests = [];
        this.tagBooksLoaded = false;
    }

    async fetchConversations() {
        if (!window.apiClient) return [];
        try {
            const res = await window.apiClient.apiGet('/api/MessagesAPI/conversations');
            this.conversations = res?.data || [];
            return this.conversations;
        } catch (err) {
            console.error("Failed to fetch conversations:", err);
            throw err;
        }
    }

    async fetchSupportRequests() {
        if (!window.apiClient) return [];
        try {
            const res = await window.apiClient.apiGet('/api/MessagesAPI/call-requests');
            this.supportRequests = res?.data || [];
            return this.supportRequests;
        } catch (err) {
            console.error("Failed to fetch support requests:", err);
            throw err;
        }
    }

    async fetchMessages(conversationId) {
        if (!window.apiClient) return [];
        try {
            const res = await window.apiClient.apiGet(`/api/MessagesAPI/conversations/${conversationId}`);
            return res?.data || [];
        } catch (err) {
            console.error(`Failed to fetch messages for conversation ${conversationId}:`, err);
            throw err;
        }
    }

    async fetchBuyerProfile(conversationId) {
        if (!window.apiClient) return null;
        try {
            const res = await window.apiClient.apiGet(`/api/MessagesAPI/conversations/${conversationId}/buyer-profile`);
            return res?.data || null;
        } catch (err) {
            console.error(`Failed to fetch buyer profile for conversation ${conversationId}:`, err);
            throw err;
        }
    }

    async sendMessage(dto) {
        if (!window.apiClient) return null;
        try {
            const res = await window.apiClient.apiPost('/api/MessagesAPI', dto);
            return res?.data || null;
        } catch (err) {
            console.error("Failed to send message:", err);
            throw err;
        }
    }

    async resolveSupportRequest(requestId) {
        if (!window.apiClient) return;
        try {
            await window.apiClient.apiPut(`/api/MessagesAPI/call-requests/${requestId}/resolve`, {});
        } catch (err) {
            console.error(`Failed to resolve support request ${requestId}:`, err);
            throw err;
        }
    }

    async setSupportRequestInProgress(requestId) {
        if (!window.apiClient) return;
        try {
            await window.apiClient.apiPut(`/api/MessagesAPI/call-requests/${requestId}/in-progress`, {});
        } catch (err) {
            console.error(`Failed to set support request ${requestId} to In Progress:`, err);
            throw err;
        }
    }

    async fetchRecentBooks() {
        if (!window.apiClient) return [];
        try {
            const res = await window.apiClient.apiGet('/api/RealBook?pageSize=5');
            return res?.data?.items || res?.data || [];
        } catch (err) {
            console.error("Failed to fetch recent books:", err);
            throw err;
        }
    }

    async fetchWishlist() {
        if (!window.apiClient) return [];
        try {
            const res = await window.apiClient.apiGet('/api/Wishlist');
            return res?.data || [];
        } catch (err) {
            console.error("Failed to fetch wishlist:", err);
            throw err;
        }
    }
}

// Attach to window namespace for global access
window.AdminMessagesModel = AdminMessagesModel;
