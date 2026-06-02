class NotificationModel {
    constructor() {
        this.notifications = [];
        this.filter = 'all'; // 'all' or 'unread'
        this.isLoading = false;
        this.isInitialized = false;
    }

    /**
     * Fetch all notifications from the API
     */
    async fetchNotifications() {
        this.isLoading = true;
        
        try {
            if (window.apiClient) {
                // Fetch from backend
                const data = await window.apiClient.apiGet('/api/Notification');
                if (Array.isArray(data)) {
                    this.notifications = data;
                }
                this.isInitialized = true;
            } else {
                console.error("apiClient not found.");
            }
        } catch (error) {
            console.error("Error fetching notifications:", error);
        } finally {
            this.isLoading = false;
        }

        return this.getFilteredNotifications();
    }

    /**
     * Filter notifications based on active tab state
     */
    getFilteredNotifications() {
        if (this.filter === 'unread') {
            return this.notifications.filter(n => !n.isRead);
        }
        return this.notifications;
    }

    /**
     * Mark a single notification as read
     */
    async markAsRead(id) {
        try {
            if (window.apiClient) {
                await window.apiClient.apiPut(`/api/Notification/${id}/read`, {});
                
                // Update local state
                const n = this.notifications.find(x => x.notificationID === id);
                if (n) {
                    n.isRead = true;
                }
                return true;
            }
        } catch (error) {
            console.error("Error marking notification as read:", error);
        }
        return false;
    }

    /**
     * Mark all notifications as read
     */
    async markAllAsRead() {
        try {
            if (window.apiClient) {
                await window.apiClient.apiPut('/api/Notification/read-all', {});
                
                // Update local state
                this.notifications = this.notifications.map(n => ({ ...n, isRead: true }));
                return true;
            }
        } catch (error) {
            console.error("Error marking all notifications as read:", error);
        }
        return false;
    }

    /**
     * Delete a notification
     */
    async deleteNotification(id) {
        try {
            if (window.apiClient) {
                await window.apiClient.apiDelete(`/api/Notification/${id}`);
                
                // Update local state
                this.notifications = this.notifications.filter(n => n.notificationID !== id);
                return true;
            }
        } catch (error) {
            console.error("Error deleting notification:", error);
        }
        return false;
    }

    /**
     * Get number of unread notifications
     */
    getUnreadCount() {
        return this.notifications.filter(n => !n.isRead).length;
    }

    /**
     * Update active filter state
     */
    setFilter(filter) {
        if (filter === 'all' || filter === 'unread') {
            this.filter = filter;
            return true;
        }
        return false;
    }

    /**
     * Append a new notification from SignalR
     */
    addNotification(notification) {
        // notification is already camelCase from API
        this.notifications.unshift(notification);
    }
}
