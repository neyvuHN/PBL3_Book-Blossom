class NotificationController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
        this.hubConnection = null;

        // Bind View events to Controller handlers
        this.view.bindBellClick(this.handleToggleDropdown.bind(this));
        this.view.bindClickOutside(this.handleCloseDropdown.bind(this));
        this.view.bindNotificationClick(this.handleNotificationClick.bind(this));
        this.view.bindDeleteNotificationClick(this.handleDeleteNotification.bind(this));
        this.view.bindMarkAllReadClick(this.handleMarkAllAsRead.bind(this));
        this.view.bindFilterTabClick(this.handleFilterChange.bind(this));
    }

    /**
     * Initializes notifications: Fetches list to display the correct unread badge count immediately
     */
    async init() {
        try {
            const token = localStorage.getItem('accessToken');
            if (!token) return; // Only for logged-in users

            await this.model.fetchNotifications();
            this.updateBadgeOnly();
            
            // Connect to SignalR
            this.initSignalR(token);
        } catch (error) {
            console.error("Failed to initialize notifications store", error);
        }
    }

    initSignalR(token) {
        if (typeof signalR === 'undefined') {
            console.warn("SignalR library not loaded. Real-time notifications disabled.");
            return;
        }

        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/notification", { accessTokenFactory: () => token })
            .withAutomaticReconnect()
            .build();

        this.hubConnection.on("ReceiveNotification", (notification) => {
            // SignalR provides camelCase properties
            this.model.addNotification(notification);
            this.updateBadgeOnly();
            
            // If dropdown is open, re-render
            if (this.view.dropdown && this.view.dropdown.classList.contains('active')) {
                const items = this.model.getFilteredNotifications();
                const unreadCount = this.model.getUnreadCount();
                this.view.render(items, this.model.filter, unreadCount);
            } else {
                if (window.apiClient && window.apiClient.showToast) {
                    window.apiClient.showToast(notification.title, 'info');
                }
            }
        });

        this.hubConnection.start()
            .then(() => console.log("Connected to NotificationHub"))
            .catch(err => console.error("Error connecting to NotificationHub:", err));
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
    async handleToggleDropdown() {
        if (!this.view.dropdown) return;
        
        const isOpening = !this.view.dropdown.classList.contains('active');
        this.view.toggleDropdown();

        if (isOpening) {
            // Close the user-dropdown if it is open
            const userDropdown = document.querySelector('.user-dropdown');
            if (userDropdown) {
                userDropdown.classList.remove('active');
            }

            if (!this.model.isInitialized) {
                await this.refreshDropdown();
            } else {
                const items = this.model.getFilteredNotifications();
                const unreadCount = this.model.getUnreadCount();
                this.view.render(items, this.model.filter, unreadCount);
            }
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
            if (window.apiClient && window.apiClient.showToast) {
                window.apiClient.showToast("All notifications marked as read", "success");
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
        // Enums map: 0 = OrderStatus, 1 = ReturnUpdate, 3 = NewThread, etc.
        // We can just rely on basic logic if we pass the reference correctly.
        // Currently the frontend passes type as integer from backend or maps it.
        // The previous mock used strings like 'Order', 'Book', etc.
        // With backend it will be an integer NotificationType.
        
        switch (type) {
            case 0: // OrderStatus
            case 11: // NewReturnRequest
                window.location.href = '/Orders';
                break;
            case 1: // ReturnUpdate
                window.location.href = '/Orders'; // Or return specific URL
                break;
            case 9: // NewBookArrival
                if (refId) {
                    window.location.href = `/Explore#book-details-${encodeURIComponent(refId)}`;
                } else {
                    window.location.href = '/Explore';
                }
                break;
            case 3: // NewThread
            case 10: // ReportAlert
            case 7: // NewInteraction
                window.location.href = '/Community';
                break;
            default:
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
