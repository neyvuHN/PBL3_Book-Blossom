class NotificationModel {
    constructor() {
        this.notifications = [];
        this.filter = 'all'; // 'all' or 'unread'
        this.isLoading = false;
    }

    /**
     * Fetch all notifications from the storage with a small simulated network delay
     */
    async fetchNotifications() {
        this.isLoading = true;
        
        // Premium UI experience: simulated network delay to show visual skeleton/spinner loading
        await new Promise(resolve => setTimeout(resolve, 350));

        if (window.BookBlossomNotification) {
            this.notifications = window.BookBlossomNotification.getNotifications();
        } else {
            console.error("BookBlossomNotification store not found. Using local memory mock.");
            this.notifications = [];
        }

        this.isLoading = false;
        return this.getFilteredNotifications();
    }

    /**
     * Filter notifications based on active tab state
     */
    getFilteredNotifications() {
        if (this.filter === 'unread') {
            return this.notifications.filter(n => !n.IsRead);
        }
        return this.notifications;
    }

    /**
     * Mark a single notification as read
     */
    async markAsRead(id) {
        if (window.BookBlossomNotification) {
            this.notifications = window.BookBlossomNotification.markAsRead(id);
            return true;
        }
        
        // Fallback inside local state
        const n = this.notifications.find(x => x.NotificationID === id);
        if (n) {
            n.IsRead = true;
            return true;
        }
        return false;
    }

    /**
     * Mark all notifications as read
     */
    async markAllAsRead() {
        if (window.BookBlossomNotification) {
            this.notifications = window.BookBlossomNotification.markAllAsRead();
            return true;
        }

        // Fallback
        this.notifications = this.notifications.map(n => ({ ...n, IsRead: true }));
        return true;
    }

    /**
     * Delete a notification
     */
    async deleteNotification(id) {
        if (window.BookBlossomNotification) {
            this.notifications = window.BookBlossomNotification.deleteNotification(id);
            return true;
        }

        this.notifications = this.notifications.filter(n => n.NotificationID !== id);
        return true;
    }

    /**
     * Get number of unread notifications
     */
    getUnreadCount() {
        return this.notifications.filter(n => !n.IsRead).length;
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
}
