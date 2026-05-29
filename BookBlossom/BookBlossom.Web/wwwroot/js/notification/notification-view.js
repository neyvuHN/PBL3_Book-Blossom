class NotificationView {
    constructor() {
        this.bell = document.getElementById('notification-bell');
        this.dot = document.getElementById('notification-dot');
        this.dropdown = document.getElementById('notification-dropdown');
    }

    /**
     * Toggles dropdown open/close state
     */
    toggleDropdown() {
        if (!this.dropdown) return;
        this.dropdown.classList.toggle('active');
        this.bell.classList.toggle('active');
    }

    /**
     * Closes the dropdown
     */
    closeDropdown() {
        if (!this.dropdown) return;
        this.dropdown.classList.remove('active');
        this.bell.classList.remove('active');
    }

    /**
     * Renders the loading spinner state inside the dropdown
     */
    showLoading() {
        if (!this.dropdown) return;

        this.dropdown.innerHTML = `
            <div class="notif-dropdown-header">
                <h3>Notifications</h3>
            </div>
            <div class="notif-loading-wrapper">
                <div class="notif-spinner"></div>
                <p>Loading your notifications...</p>
            </div>
        `;
    }

    /**
     * Renders the full notifications view
     * @param {Array} notifications list of notifications to display
     * @param {string} filter active filter ('all' or 'unread')
     * @param {number} unreadCount number of unread notifications
     */
    render(notifications, filter, unreadCount) {
        if (!this.dropdown) return;

        // 1. Update Notification Dot/Badge in Navbar
        if (this.dot) {
            if (unreadCount > 0) {
                this.dot.textContent = unreadCount > 9 ? '9+' : unreadCount;
                this.dot.style.display = 'flex';
                this.dot.classList.add('pulse');
            } else {
                this.dot.style.display = 'none';
                this.dot.classList.remove('pulse');
            }
        }

        // 2. Generate Dropdown Layout
        const hasUnread = notifications.some(n => !n.IsRead);
        
        let dropdownHtml = `
            <div class="notif-dropdown-header">
                <h3>Notifications</h3>
                <div class="notif-header-actions">
                    ${hasUnread ? `<button type="button" class="btn-notif-mark-all" id="btn-notif-mark-all">Mark all as read</button>` : ''}
                </div>
            </div>

            <div class="notif-dropdown-tabs">
                <button type="button" class="notif-tab ${filter === 'all' ? 'active' : ''}" data-filter="all">
                    All ${filter === 'all' ? `<span class="tab-count">${notifications.length}</span>` : ''}
                </button>
                <button type="button" class="notif-tab ${filter === 'unread' ? 'active' : ''}" data-filter="unread">
                    Unread ${filter === 'unread' ? `<span class="tab-count">${notifications.length}</span>` : ''}
                </button>
                <div class="notif-tab-indicator ${filter === 'unread' ? 'slide-unread' : ''}"></div>
            </div>

            <div class="notif-dropdown-body" id="notif-list-container">
        `;

        if (notifications.length === 0) {
            // Render Empty State
            dropdownHtml += `
                <div class="notif-empty-state">
                    <div class="notif-empty-icon">🌸</div>
                    <h4>All caught up!</h4>
                    <p>You have no notifications in this category.</p>
                </div>
            `;
        } else {
            // Render Notification Items
            notifications.forEach(n => {
                const isReadClass = n.IsRead ? 'read' : 'unread';
                const iconHtml = this.getTypeIcon(n.NotificationType);
                const timeAgo = this.formatRelativeTime(n.CreatedDate);

                dropdownHtml += `
                    <div class="notif-item ${isReadClass}" 
                         data-id="${n.NotificationID}" 
                         data-type="${n.NotificationType}" 
                         data-ref-id="${n.ReferenceID}">
                        ${iconHtml}
                        <div class="notif-item-content">
                            <h4 class="notif-item-title">${n.Title}</h4>
                            <p class="notif-item-text">${n.Content}</p>
                            <span class="notif-item-time">${timeAgo}</span>
                        </div>
                        ${!n.IsRead ? '<span class="unread-dot-indicator"></span>' : ''}
                        <button type="button" class="btn-notif-delete" data-id="${n.NotificationID}" title="Dismiss">
                            &times;
                        </button>
                    </div>
                `;
            });
        }

        dropdownHtml += `
            </div>
        `;

        this.dropdown.innerHTML = dropdownHtml;
    }

    /**
     * Map notification type to visual premium icon wrapper
     */
    getTypeIcon(type) {
        switch (type) {
            case 'Order':
                return `
                    <div class="notif-icon-wrapper order">
                        <i class="fas fa-shopping-bag"></i>
                    </div>`;
            case 'Book':
                return `
                    <div class="notif-icon-wrapper book">
                        <i class="fas fa-book-open"></i>
                    </div>`;
            case 'Community':
                return `
                    <div class="notif-icon-wrapper community">
                        <i class="fas fa-comments"></i>
                    </div>`;
            case 'System':
            default:
                return `
                    <div class="notif-icon-wrapper system">
                        <i class="fas fa-bell"></i>
                    </div>`;
        }
    }

    /**
     * Format a date ISO string to elegant relative time string
     */
    formatRelativeTime(dateString) {
        const now = new Date();
        const date = new Date(dateString);
        const diffMs = now - date;
        const diffSec = Math.floor(diffMs / 1000);
        const diffMin = Math.floor(diffSec / 60);
        const diffHr = Math.floor(diffMin / 60);
        const diffDays = Math.floor(diffHr / 24);

        if (diffSec < 60) {
            return 'Just now';
        } else if (diffMin < 60) {
            return `${diffMin} ${diffMin === 1 ? 'minute' : 'minutes'} ago`;
        } else if (diffHr < 24) {
            return `${diffHr} ${diffHr === 1 ? 'hour' : 'hours'} ago`;
        } else if (diffDays < 7) {
            return `${diffDays} ${diffDays === 1 ? 'day' : 'days'} ago`;
        } else {
            return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
        }
    }

    /* =========================================================================
     * Event Binding Helpers for MVC Controller
     * ========================================================================= */

    bindBellClick(handler) {
        if (!this.bell) return;
        this.bell.addEventListener('click', e => {
            e.preventDefault();
            e.stopPropagation();
            handler();
        });
    }

    bindClickOutside(handler) {
        document.addEventListener('click', e => {
            if (!this.dropdown) return;
            
            // Check if dropdown is active/visible
            if (this.dropdown.classList.contains('active')) {
                const clickInsideBell = this.bell && this.bell.contains(e.target);
                const clickInsideDropdown = this.dropdown.contains(e.target);
                
                if (!clickInsideBell && !clickInsideDropdown) {
                    handler();
                }
            }
        });
    }

    bindNotificationClick(handler) {
        if (!this.dropdown) return;
        
        // Use event delegation on dropdown container
        this.dropdown.addEventListener('click', e => {
            const notifItem = e.target.closest('.notif-item');
            const deleteBtn = e.target.closest('.btn-notif-delete');
            
            // Ignore click if clicking the delete button
            if (notifItem && !deleteBtn) {
                e.stopPropagation();
                const id = notifItem.dataset.id;
                const type = notifItem.dataset.type;
                const refId = notifItem.dataset.refId;
                handler(id, type, refId);
            }
        });
    }

    bindDeleteNotificationClick(handler) {
        if (!this.dropdown) return;

        this.dropdown.addEventListener('click', e => {
            const deleteBtn = e.target.closest('.btn-notif-delete');
            if (deleteBtn) {
                e.preventDefault();
                e.stopPropagation();
                const id = deleteBtn.dataset.id;
                handler(id);
            }
        });
    }

    bindMarkAllReadClick(handler) {
        if (!this.dropdown) return;

        this.dropdown.addEventListener('click', e => {
            const btn = e.target.closest('#btn-notif-mark-all');
            if (btn) {
                e.preventDefault();
                e.stopPropagation();
                handler();
            }
        });
    }

    bindFilterTabClick(handler) {
        if (!this.dropdown) return;

        this.dropdown.addEventListener('click', e => {
            const tab = e.target.closest('.notif-tab');
            if (tab) {
                e.preventDefault();
                e.stopPropagation();
                const filterValue = tab.dataset.filter;
                handler(filterValue);
            }
        });
    }
}
