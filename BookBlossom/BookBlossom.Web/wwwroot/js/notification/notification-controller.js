class NotificationController {
    constructor(model, view) {
        this.model = model;
        this.view = view;

        // Bind View events to Controller handlers
        this.view.bindBellClick(this.handleToggleDropdown.bind(this));
        this.view.bindClickOutside(this.handleCloseDropdown.bind(this));
        this.view.bindNotificationClick(this.handleNotificationClick.bind(this));
        this.view.bindDeleteNotificationClick(this.handleDeleteNotification.bind(this));
        this.view.bindMarkAllReadClick(this.handleMarkAllAsRead.bind(this));
        this.view.bindFilterTabClick(this.handleFilterChange.bind(this));

        // Listen for internal store updates to keep the navbar badge synced in real-time
        window.addEventListener('bookblossom_notifications_updated', () => {
            this.updateBadgeOnly();
        });
    }

    /**
     * Initializes notifications: Fetches list to display the correct unread badge count immediately
     */
    async init() {
        try {
            // Load state silently on startup to show badge dot immediately
            if (window.BookBlossomNotification) {
                this.model.notifications = window.BookBlossomNotification.getNotifications();
            }
            this.updateBadgeOnly();
        } catch (error) {
            console.error("Failed to initialize notifications store", error);
        }
    }

    /**
     * Updates navbar bell unread badge without rendering the entire dropdown content
     */
    updateBadgeOnly() {
        const count = this.model.getUnreadCount();
        if (this.view.dot) {
            if (count > 0) {
                this.view.dot.textContent = count > 9 ? '9+' : count;
                this.view.dot.style.display = 'flex';
                this.view.dot.classList.add('pulse');
            } else {
                this.view.dot.style.display = 'none';
                this.view.dot.classList.remove('pulse');
            }
        }
    }

    /**
     * Handles clicking the bell icon to toggle the dropdown
     */
    handleToggleDropdown() {
        if (!this.view.dropdown) return;
        
        const isOpening = !this.view.dropdown.classList.contains('active');
        this.view.toggleDropdown();

        if (isOpening) {
            // Trigger skeleton/loading and fetch fresh data
            this.refreshDropdown();
        }
    }

    /**
     * Handles click outside dropdown to close it
     */
    handleCloseDropdown() {
        this.view.closeDropdown();
    }

    /**
     * Refreshes and renders dropdown contents
     */
    async refreshDropdown() {
        this.view.showLoading();
        try {
            const items = await this.model.fetchNotifications();
            const unreadCount = this.model.getUnreadCount();
            this.view.render(items, this.model.filter, unreadCount);
        } catch (error) {
            console.error("Failed to refresh notifications", error);
            // Render basic layout with empty state on error
            this.view.render([], this.model.filter, 0);
        }
    }

    /**
     * Handles clicking a notification item: marks as read and redirects
     */
    async handleNotificationClick(id, type, refId) {
        try {
            // Mark as read in store
            await this.model.markAsRead(id);
            this.updateBadgeOnly();
            
            // Close dropdown for smooth transition
            this.view.closeDropdown();

            // Redirect user based on notification type and ReferenceID
            this.navigateByNotification(type, refId);
        } catch (error) {
            console.error("Failed to process notification click", error);
        }
    }

    /**
     * Handles deletion/dismissal of a single notification card
     */
    async handleDeleteNotification(id) {
        try {
            await this.model.deleteNotification(id);
            const items = this.model.getFilteredNotifications();
            const unreadCount = this.model.getUnreadCount();
            
            // Re-render immediately
            this.view.render(items, this.model.filter, unreadCount);
        } catch (error) {
            console.error("Failed to delete notification", error);
        }
    }

    /**
     * Handles clicking "Mark all as read"
     */
    async handleMarkAllAsRead() {
        try {
            await this.model.markAllAsRead();
            const items = this.model.getFilteredNotifications();
            const unreadCount = this.model.getUnreadCount();
            
            this.view.render(items, this.model.filter, unreadCount);
            
            // Trigger a custom toast if global system helper is available
            if (typeof window.showToast === 'function') {
                window.showToast("All notifications marked as read", "success");
            } else if (typeof showToast === 'function') {
                showToast("All notifications marked as read", "success");
            }
        } catch (error) {
            console.error("Failed to mark all as read", error);
        }
    }

    /**
     * Handles switching between "All" and "Unread" tabs
     */
    handleFilterChange(newFilter) {
        this.model.setFilter(newFilter);
        const items = this.model.getFilteredNotifications();
        const unreadCount = this.model.getUnreadCount();
        this.view.render(items, this.model.filter, unreadCount);
    }

    /**
     * Navigates the application based on reference metadata
     */
    navigateByNotification(type, refId) {
        switch (type) {
            case 'Order':
                // Navigate to My Orders section
                window.location.href = '/Orders';
                break;
            case 'Book':
                if (refId === 'w2') {
                    // Navigate to Blind Date details using existing hash routing patterns
                    window.location.href = `/BlindDate#blind-details-Mystery-Thriller`;
                } else if (refId) {
                    window.location.href = `/Explore#book-details-${encodeURIComponent(refId)}`;
                } else {
                    window.location.href = '/Explore';
                }
                break;
            case 'Community':
                // Navigate to community forum/threads
                window.location.href = '/Community';
                break;
            case 'System':
            default:
                // Stay on current page or redirect to profile
                window.location.href = '/Profile';
                break;
        }
    }
}

// Global initialization on DOMContentLoaded
document.addEventListener('DOMContentLoaded', () => {
    const bellElement = document.getElementById('notification-bell');
    if (bellElement) {
        const model = new NotificationModel();
        const view = new NotificationView();
        const controller = new NotificationController(model, view);
        
        controller.init();
        
        // Expose globally so debugging or other code can trigger update checks
        window.BookBlossomNotificationController = controller;
    }
});
