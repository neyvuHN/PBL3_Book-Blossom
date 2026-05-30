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
            staff: document.getElementById('staffContent'),
            banned: document.getElementById('bannedContent')
        };

        // Table Bodies
        this.bodies = {
            buyers: this.panes.buyers.querySelector('tbody'),
            staff: this.panes.staff.querySelector('tbody'),
            banned: this.panes.banned.querySelector('tbody')
        };

        // Filter Elements
        this.searchInput = document.getElementById('searchInput');
        this.roleFilter = document.getElementById('roleFilter');
        this.scoreFilter = document.getElementById('scoreFilter');

        // Popover Elements
        this.popover = document.getElementById('editRolePopover');
        this.popoverCloseBtn = document.getElementById('btnPopoverClose');
        this.popoverUsername = document.getElementById('popoverUsername');
        this.popoverPassword = document.getElementById('popoverAdminPassword');
        this.btnSaveRole = document.getElementById('btnSaveRole');
        this.roleCardOptions = document.querySelectorAll('.role-card-option');

        // Top Buttons
        this.btnAddStaff = document.getElementById('btnAddStaff');
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

        // Hide role popover when changing tabs to prevent floating orphaned modals
        this.closeEditRolePopover();
    }

    /**
     * Helper to render a specific row for a User (both Buyers and Banned tables).
     */
    _createUserRowHtml(user) {
        const roleClass = user.role === 'Admin' ? 'role-admin' :
            user.role === 'Moderator' ? 'role-moderator' : 'role-user';

        const planClass = user.plan === 'Basic' ? 'plan-basic' :
            user.plan === 'Pro' ? 'plan-pro' : 'plan-free';

        const scoreHtml = user.internalScore < 50
            ? `<span class="score-value caution" title="Critical low internal score!">${user.internalScore} <i class="ph ph-warning-octagon"></i></span>`
            : `<span class="score-value high">${user.internalScore}</span>`;

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
                <td style="position: relative;">
                    <button class="action-trigger-btn btn-dots-action" title="View Options">
                        <i class="ph ph-dots-three-outline-fill"></i>
                    </button>
                </td>
            </tr>
        `;
    }

    /**
     * Helper to render a specific row for Staff (excludes plan column).
     */
    _createStaffRowHtml(user) {
        const roleClass = user.role === 'Admin' ? 'role-admin' :
            user.role === 'Moderator' ? 'role-moderator' : 'role-user';

        const scoreHtml = user.internalScore < 50
            ? `<span class="score-value caution" title="Critical low internal score!">${user.internalScore} <i class="ph ph-warning-octagon"></i></span>`
            : `<span class="score-value high">${user.internalScore}</span>`;

        const isChecked = user.status === 'Active' ? 'checked' : '';
        const statusClass = user.status === 'Active' ? 'active' : 'banned';

        return `
            <tr data-user-id="${user.id}">
                <td>
                    <div class="user-info-cell">
                        <img src="${user.avatarUrl}" alt="${user.username}" class="user-avatar" onerror="this.src='https://i.pravatar.cc/150?img=1'" />
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
                <td style="position: relative;">
                    <button class="action-trigger-btn btn-dots-action" title="View Options">
                        <i class="ph ph-dots-three-outline-fill"></i>
                    </button>
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
                    <td colspan="7" style="text-align: center; padding: 30px; color: #888;">
                        No buyers found matching your criteria.
                    </td>
                </tr>
            `;
            return;
        }
        this.bodies.buyers.innerHTML = users.map(user => this._createUserRowHtml(user)).join('');
    }

    /**
     * Renders Staff list in the Staff table body.
     */
    renderStaff(users) {
        if (users.length === 0) {
            this.bodies.staff.innerHTML = `
                <tr>
                    <td colspan="6" style="text-align: center; padding: 30px; color: #888;">
                        No staff members found matching your criteria.
                    </td>
                </tr>
            `;
            return;
        }
        this.bodies.staff.innerHTML = users.map(user => this._createStaffRowHtml(user)).join('');
    }

    /**
     * Renders Banned list in the Banned table body.
     */
    renderBanned(users) {
        if (users.length === 0) {
            this.bodies.banned.innerHTML = `
                <tr>
                    <td colspan="7" style="text-align: center; padding: 30px; color: #888;">
                        No banned users found matching your criteria.
                    </td>
                </tr>
            `;
            return;
        }
        this.bodies.banned.innerHTML = users.map(user => this._createUserRowHtml(user)).join('');
    }

    /**
     * Positions and reveals the floating Edit Role popover menu.
     */
    openEditRolePopover(username, currentRole, buttonElement) {
        this.popoverUsername.textContent = `@${username}`;
        this.popoverPassword.value = ''; // Reset password field
        this.toggleSaveRoleButton(false); // Default disabled until validation

        // Highlight active role card option
        this.roleCardOptions.forEach(opt => {
            const role = opt.getAttribute('data-role');
            const radio = opt.querySelector('input[type="radio"]');

            if (role === currentRole) {
                opt.classList.add('selected');
                if (radio) radio.checked = true;
            } else {
                opt.classList.remove('selected');
                if (radio) radio.checked = false;
            }
        });

        // Smart dynamic positioning relative to trigger button inside management card
        const rect = buttonElement.getBoundingClientRect();
        const cardRect = document.querySelector('.management-card').getBoundingClientRect();

        const topPosition = (rect.bottom - cardRect.top) + 8;
        const leftPosition = (rect.left - cardRect.left) - 250;

        this.popover.style.top = `${topPosition}px`;
        this.popover.style.left = `${leftPosition}px`;
        this.popover.style.display = 'block';
    }

    /**
     * Hides the Edit Role popover menu.
     */
    closeEditRolePopover() {
        this.popover.style.display = 'none';
        this.popoverPassword.value = '';
    }

    /**
     * Visually updates role card selection inside popover.
     */
    updateRoleCardSelection(selectedRole) {
        this.roleCardOptions.forEach(opt => {
            const role = opt.getAttribute('data-role');
            const radio = opt.querySelector('input[type="radio"]');

            if (role === selectedRole) {
                opt.classList.add('selected');
                if (radio) radio.checked = true;
            } else {
                opt.classList.remove('selected');
                if (radio) radio.checked = false;
            }
        });
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
     * Enables or disables the primary popover Save Changes button.
     */
    toggleSaveRoleButton(enabled) {
        this.btnSaveRole.disabled = !enabled;
    }
}
