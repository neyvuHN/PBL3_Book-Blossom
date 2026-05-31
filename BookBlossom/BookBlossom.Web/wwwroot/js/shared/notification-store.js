(function (window) {
    const NOTIFICATION_KEY = 'bookblossom_notifications';
    const NOTIFICATION_INIT_KEY = 'bookblossom_notifications_initialized_v2'; // Changed key to force re-init

    // Premium default notifications matching database schema
    const DEFAULT_NOTIFICATIONS = [
        {
            NotificationID: 'n-ord-101',
            UserID: 'Jane Doe',
            Title: 'Order Dispatched 📦',
            Content: 'Great news! Your order #BB-90812 has been hand-picked and dispatched. Click to trace your package.',
            NotificationType: 'ORDER_STATUS',
            ReferenceID: 'BB-90812',
            ReferenceType: 'Order',
            IsRead: false,
            CreatedDate: new Date(Date.now() - 1000 * 60 * 15).toISOString() // 15 mins ago
        },
        {
            NotificationID: 'n-rank-105',
            UserID: 'Jane Doe',
            Title: 'Level Up! Silver Reader 🏆',
            Content: 'Congratulations! You have reached Silver Reader rank. Enjoy your new perks and discounts.',
            NotificationType: 'RANK_UP',
            ReferenceID: 'rank-silver',
            ReferenceType: 'User',
            IsRead: false,
            CreatedDate: new Date(Date.now() - 1000 * 60 * 45).toISOString() // 45 minutes ago
        },
        {
            NotificationID: 'n-book-102',
            UserID: 'Jane Doe',
            Title: 'New Blind Date Book Released! 🌸',
            Content: 'A mysterious "Enchanted Romance" blind date package is now available. Will you discover its secret?',
            NotificationType: 'NEW_BOOK_ARRIVAL',
            ReferenceID: 'w2',
            ReferenceType: 'Book',
            IsRead: false,
            CreatedDate: new Date(Date.now() - 1000 * 60 * 60 * 2).toISOString() // 2 hours ago
        },
        {
            NotificationID: 'n-point-106',
            UserID: 'Jane Doe',
            Title: '+50 Points Earned 🪙',
            Content: 'You received 50 blossom points for leaving a detailed review on "The Midnight Library".',
            NotificationType: 'POINT_CHANGE',
            ReferenceID: 'review-99',
            ReferenceType: 'Point',
            IsRead: false,
            CreatedDate: new Date(Date.now() - 1000 * 60 * 60 * 5).toISOString() // 5 hours ago
        },
        {
            NotificationID: 'n-ret-107',
            UserID: 'Jane Doe',
            Title: 'Return Request Approved 🔄',
            Content: 'Your return request for order #BB-88021 has been approved. Please check your email for shipping instructions.',
            NotificationType: 'RETURN_UPDATE',
            ReferenceID: 'ret-88021',
            ReferenceType: 'Return',
            IsRead: true,
            CreatedDate: new Date(Date.now() - 1000 * 60 * 60 * 12).toISOString() // 12 hours ago
        },
        {
            NotificationID: 'n-comm-103',
            UserID: 'Jane Doe',
            Title: 'Community Reply 💬',
            Content: 'Lily Evans replied to your thread "My Favorite Classic Novels of all time". Join the discussion!',
            NotificationType: 'NEW_THREAD',
            ReferenceID: 'thread-12',
            ReferenceType: 'Community',
            IsRead: true,
            CreatedDate: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString() // 1 day ago
        },
        {
            NotificationID: 'n-badge-108',
            UserID: 'Jane Doe',
            Title: 'New Badge Unlocked! 🏅',
            Content: 'You have earned the "Weekend Bookworm" badge for reading consistently over the weekend!',
            NotificationType: 'BADGE_EARNED',
            ReferenceID: 'badge-weekend',
            ReferenceType: 'User',
            IsRead: true,
            CreatedDate: new Date(Date.now() - 1000 * 60 * 60 * 48).toISOString() // 2 days ago
        },
        {
            NotificationID: 'n-warn-109',
            UserID: 'Jane Doe',
            Title: 'Community Guideline Reminder ⚠️',
            Content: 'Please remember to keep discussions respectful. A recent comment of yours was flagged for review.',
            NotificationType: 'MOD_WARNING',
            ReferenceID: 'warn-12',
            ReferenceType: 'System',
            IsRead: true,
            CreatedDate: new Date(Date.now() - 1000 * 60 * 60 * 72).toISOString() // 3 days ago
        },
        {
            NotificationID: 'n-sys-104',
            UserID: 'Jane Doe',
            Title: 'Welcome to BookBlossom! ✨',
            Content: 'Thank you for joining our community of passionate readers. Complete your profile to unlock custom badges!',
            NotificationType: 'NONE',
            ReferenceID: 'profile-badge',
            ReferenceType: 'System',
            IsRead: true,
            CreatedDate: new Date(Date.now() - 1000 * 60 * 60 * 24 * 5).toISOString() // 5 days ago
        }
    ];

    function safeParse(json, fallback) {
        try {
            return JSON.parse(json);
        } catch {
            return fallback;
        }
    }

    function normalizeNotification(n) {
        n = n || {};
        return {
            NotificationID: n.NotificationID || ('notif-' + Date.now() + '-' + Math.floor(Math.random() * 10000)),
            UserID: n.UserID || 'Jane Doe',
            Title: n.Title || 'New Notification',
            Content: n.Content || '',
            NotificationType: n.NotificationType || 'System',
            ReferenceID: n.ReferenceID || '',
            ReferenceType: n.ReferenceType || 'System',
            IsRead: n.IsRead === true,
            CreatedDate: n.CreatedDate || new Date().toISOString()
        };
    }

    function getNotifications() {
        const initialized = localStorage.getItem(NOTIFICATION_INIT_KEY);
        if (!initialized) {
            localStorage.setItem(NOTIFICATION_INIT_KEY, 'true');
            saveNotifications(DEFAULT_NOTIFICATIONS);
            return DEFAULT_NOTIFICATIONS.map(normalizeNotification);
        }

        const raw = localStorage.getItem(NOTIFICATION_KEY);
        const items = safeParse(raw, []);
        if (!Array.isArray(items)) {
            return [];
        }
        return items.map(normalizeNotification);
    }

    function saveNotifications(items) {
        const normalized = Array.isArray(items) ? items.map(normalizeNotification) : [];
        localStorage.setItem(NOTIFICATION_KEY, JSON.stringify(normalized));
        return normalized;
    }

    function markAsRead(id) {
        const items = getNotifications();
        const updated = items.map(n => {
            if (n.NotificationID === id) {
                return { ...n, IsRead: true };
            }
            return n;
        });
        saveNotifications(updated);
        
        // Dispatch custom global event so other UI components can react if needed
        window.dispatchEvent(new CustomEvent('bookblossom_notifications_updated'));
        return updated;
    }

    function markAllAsRead() {
        const items = getNotifications();
        const updated = items.map(n => ({ ...n, IsRead: true }));
        saveNotifications(updated);

        window.dispatchEvent(new CustomEvent('bookblossom_notifications_updated'));
        return updated;
    }

    function deleteNotification(id) {
        const items = getNotifications().filter(n => n.NotificationID !== id);
        saveNotifications(items);

        window.dispatchEvent(new CustomEvent('bookblossom_notifications_updated'));
        return items;
    }

    function getUnreadCount() {
        return getNotifications().filter(n => !n.IsRead).length;
    }

    function addNotification(notification) {
        const items = getNotifications();
        const newItem = normalizeNotification(notification);
        items.unshift(newItem); // Put new notification at the top
        saveNotifications(items);

        window.dispatchEvent(new CustomEvent('bookblossom_notifications_updated'));
        return items;
    }

    // Expose Global Store API
    window.BookBlossomNotification = {
        getNotifications,
        saveNotifications,
        markAsRead,
        markAllAsRead,
        deleteNotification,
        getUnreadCount,
        addNotification
    };
})(window);
