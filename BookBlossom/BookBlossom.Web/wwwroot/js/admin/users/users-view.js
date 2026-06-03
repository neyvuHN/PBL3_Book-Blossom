/**
 * Frontend MVC - View
 * Manages the DOM, rendering, active state toggles, and popover positioning.
 */
class UsersView {
    constructor() {
        // Core Containers
        this.tabs = document.querySelectorAll('.admin-tab-item');
        this.panes = {
            buyers: document.getElementById('buyersContent'),
            banned: document.getElementById('bannedContent')
        };

        // Table Bodies
        this.bodies = {
            buyers: this.panes.buyers.querySelector('tbody'),
            banned: this.panes.banned.querySelector('tbody')
        };

        // Filter Elements
        this.searchInput = document.getElementById('searchInput');
        this.roleFilter = document.getElementById('roleFilter');
        this.scoreFilter = document.getElementById('scoreFilter');





        // Buyer Notes Modal Selectors
        this.buyerNoteModal = document.getElementById('buyerNoteModal');
        this.btnCloseBuyerNoteModal = document.getElementById('btnCloseBuyerNoteModal');
        this.btnCancelBuyerNote = document.getElementById('btnCancelBuyerNote');
        this.btnSaveBuyerNote = document.getElementById('btnSaveBuyerNote');
        this.buyerNoteText = document.getElementById('buyerNoteText');
        this.noteModalUsername = document.getElementById('noteModalUsername');
    }

    /**
     * Show loading spinners in both table bodies
     */
    showLoading() {
        const loadingHtml = `
            <tr>
                <td colspan="6" style="text-align: center; padding: 40px; color: #888;">
                    <div class="users-loading-spinner"></div>
                    <div style="font-weight: 500; font-size: 0.9rem;">Đang tải danh sách người dùng...</div>
                </td>
            </tr>
        `;
        this.bodies.buyers.innerHTML = loadingHtml;
        this.bodies.banned.innerHTML = loadingHtml;
    }

    hasData() {
        return this.bodies.buyers.querySelector('tr[data-user-id]') !== null || 
               this.bodies.banned.querySelector('tr[data-user-id]') !== null;
    }

    /**
     * Toggles active classes on tabs and reveals the appropriate table pane.
     */
    showTab(tabName) {
        this.tabs.forEach(tab => {
            if (tab.getAttribute('data-tab') === tabName) {
                tab.classList.add('active');
            } else {
                tab.classList.remove('active');
            }
        });

        Object.keys(this.panes).forEach(key => {
            if (key === tabName) {
                this.panes[key].style.display = 'block';
            } else {
                this.panes[key].style.display = 'none';
            }
        });
    }

    /**
     * Helper to render a specific row for a User (both Buyers and Banned tables).
     */
    _createUserRowHtml(user) {
        const roleClass = user.role === 'Admin' ? 'role-admin' :
            user.role === 'Moderator' ? 'role-moderator' :
            user.role === 'Marketing Manager' ? 'role-marketing' :
            user.role === 'Store Manager' ? 'role-store' : 'role-user';

        const planClass = 'plan-' + (user.plan || 'free').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');

        let scoreHtml = '';
        if (user.internalScore < 30) {
            scoreHtml = `
                <span class="score-value caution" title="Threshold C: Permanently blacklisted">
                    ${user.internalScore} / 150 <i class="ph ph-x-circle"></i>
                </span>
                <div style="font-size: 0.72rem; color: #EB5757; font-weight: 600; margin-top: 2px;">⚠️ Banned / Blacklist</div>
            `;
        } else if (user.internalScore < 60) {
            scoreHtml = `
                <span class="score-value caution" title="Threshold B: COD disabled, comments muted">
                    ${user.internalScore} / 150 <i class="ph ph-warning-octagon"></i>
                </span>
                <div style="font-size: 0.72rem; color: #EB5757; font-weight: 600; margin-top: 2px;">⚠️ COD Disabled & Muted</div>
            `;
        } else if (user.internalScore < 80) {
            scoreHtml = `
                <span class="score-value caution" style="color: #F2994A;" title="Threshold A: Posts & Comments muted">
                    ${user.internalScore} / 150 <i class="ph ph-chat-slash"></i>
                </span>
                <div style="font-size: 0.72rem; color: #F2994A; font-weight: 600; margin-top: 2px;">⚠️ Posts & Comments Muted</div>
            `;
        } else {
            scoreHtml = `
                <span class="score-value high">${user.internalScore} / 150</span>
                <div style="font-size: 0.72rem; color: #27AE60; font-weight: 500; margin-top: 2px;">Active / Excellent</div>
            `;
        }

        const isChecked = user.status === 'Active' ? 'checked' : '';
        const statusClass = user.status === 'Active' ? 'active' : 'banned';

        return `
            <tr data-user-id="${user.id}">
                <td>
                    <div class="user-info-cell">
                        <img src="${user.avatarUrl}" alt="${user.username}" class="user-avatar" onerror="this.src='https://i.pravatar.cc/150?img=9'" />
                        <div class="user-meta-text">
                            <span class="username">${user.username}</span>
                            <span class="email">${user.email}</span>
                        </div>
                    </div>
                </td>
                <td>
                    <span class="role-badge ${roleClass}">${user.role}</span>
                </td>
                <td>
                    <span class="plan-badge ${planClass}">${user.plan}</span>
                </td>
                <td>
                    ${scoreHtml}
                </td>
                <td>${user.joinDate}</td>
                <td>
                    <div class="status-switch ${statusClass}">
                        <label class="switch">
                            <input type="checkbox" class="toggle-status-chk" ${isChecked} />
                            <span class="slider round"></span>
                        </label>
                        <span class="status-text">${user.status}</span>
                    </div>
                </td>
            </tr>
        `;
    }



    /**
     * Renders Buyers list in the Buyer table body.
     */
    renderBuyers(users) {
        if (users.length === 0) {
            this.bodies.buyers.innerHTML = `
                <tr>
                    <td colspan="6" style="text-align: center; padding: 30px; color: #888;">
                        No buyers found matching your criteria.
                    </td>
                </tr>
            `;
            return;
        }
        this.bodies.buyers.innerHTML = users.map(user => this._createUserRowHtml(user)).join('');
    }



    /**
     * Renders Banned list in the Banned table body.
     */
    renderBanned(users) {
        if (users.length === 0) {
            this.bodies.banned.innerHTML = `
                <tr>
                    <td colspan="6" style="text-align: center; padding: 30px; color: #888;">
                        No banned users found matching your criteria.
                    </td>
                </tr>
            `;
            return;
        }
        this.bodies.banned.innerHTML = users.map(user => this._createUserRowHtml(user)).join('');
    }

    /**
     * Toggle status switch visuals on the target element.
     */
    updateRowStatusVisual(rowElement, isChecked, statusText) {
        const switchDiv = rowElement.querySelector('.status-switch');
        const textSpan = rowElement.querySelector('.status-text');

        if (isChecked) {
            switchDiv.className = 'status-switch active';
            textSpan.textContent = 'Active';
        } else {
            switchDiv.className = 'status-switch banned';
            textSpan.textContent = 'Banned';
        }
    }

    /**
     * Dynamically swaps filter dropdown selections based on active tab requirements.
     */
    updateFiltersForTab(tabName) {
        if (tabName === 'buyers') {
            /* Commented out per request: Bỏ luôn filter theo role
            this.roleFilter.innerHTML = `<option value="">Role: Buyer</option>`;
            this.roleFilter.disabled = true; // No alternative roles for buyers tab
            */
            
            this.scoreFilter.innerHTML = `
                <option value="">Reputation Score</option>
                <option value="A">Under 80 (Muted)</option>
                <option value="B">Under 60 (No COD)</option>
                <option value="C">Under 30 (Banned)</option>
            `;
        } else if (tabName === 'banned') {
            /* Commented out per request: Bỏ luôn filter theo role
            this.roleFilter.disabled = false;
            this.roleFilter.innerHTML = `
                <option value="">Role: All Banned</option>
                <option value="Admin">Admin</option>
                <option value="Moderator">Moderator</option>
                <option value="User">User</option>
            `;
            */
            
            this.scoreFilter.innerHTML = `
                <option value="">Reputation Score</option>
                <option value="A">Under 80 (Muted)</option>
                <option value="B">Under 60 (No COD)</option>
                <option value="C">Under 30 (Banned)</option>
            `;
        }
    }



    /**
     * Opens the Buyer Log Notes modal overlay.
     */
    openBuyerNoteModal(username, noteText) {
        this.noteModalUsername.textContent = `@${username}`;
        this.buyerNoteText.value = noteText || '';
        this.buyerNoteModal.classList.add('active');
        
        // Auto focus for smooth input
        setTimeout(() => this.buyerNoteText.focus(), 150);
    }

    /**
     * Closes the Buyer Log Notes modal.
     */
    closeBuyerNoteModal() {
        this.buyerNoteModal.classList.remove('active');
        this.buyerNoteText.value = '';
    }


}
