/**
 * Frontend MVC - Controller
 * Glues the Model and View together. Registers events, intercepts actions, and synchronizes state updates.
 */
class UsersController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
    }

    /**
     * Entry point to wire event listeners and render the initial page view.
     */
    init() {
        this.registerTabEvents();
        this.registerFilterEvents();
        this.registerTableEvents();
        this.registerPopoverEvents();
        this.registerGlobalEvents();

        // Initialize dynamic filters for default tab ('buyers')
        this.view.updateFiltersForTab(this.model.activeTab);

        // Initial paint
        this.redrawActiveTable();
    }

    /**
     * Refreshes the content of the currently active tab.
     */
    redrawActiveTable() {
        if (this.model.activeTab === 'buyers') {
            this.view.renderBuyers(this.model.getFilteredBuyers());
        } else if (this.model.activeTab === 'staff') {
            this.view.renderStaff(this.model.getFilteredStaff());
        } else if (this.model.activeTab === 'banned') {
            this.view.renderBanned(this.model.getFilteredBanned());
        }
    }

    /**
     * Binds tab-switching clicks.
     */
    registerTabEvents() {
        this.view.tabs.forEach(tab => {
            tab.addEventListener('click', () => {
                const tabName = tab.getAttribute('data-tab');
                this.model.activeTab = tabName;
                
                // Clear query and filter selections in Model to avoid crossing criteria
                this.model.filterRole = '';
                this.model.filterScore = '';
                this.view.roleFilter.value = '';
                this.view.scoreFilter.value = '';

                // Dynamically load correct options for this tab
                this.view.updateFiltersForTab(tabName);

                this.view.showTab(tabName);
                this.redrawActiveTable();
            });
        });
    }

    /**
     * Binds search input keystrokes and filtering selections.
     */
    registerFilterEvents() {
        // Search bar typing redraw
        this.view.searchInput.addEventListener('input', (e) => {
            this.model.searchQuery = e.target.value.trim();
            this.redrawActiveTable();
        });

        // Role dropdown filter selection
        this.view.roleFilter.addEventListener('change', (e) => {
            this.model.filterRole = e.target.value;
            this.redrawActiveTable();
        });

        // Score dropdown filter selection
        this.view.scoreFilter.addEventListener('change', (e) => {
            this.model.filterScore = e.target.value;
            this.redrawActiveTable();
        });
    }

    /**
     * Binds actions and events on the dynamic tables using event delegation.
     */
    registerTableEvents() {
        const handleTableClick = (e) => {
            const tr = e.target.closest('tr');
            if (!tr) return;

            const userId = tr.getAttribute('data-user-id');
            const user = this.model.findUserById(userId);
            if (!user) return;

            // 1. Check if they clicked status switch toggle
            if (e.target.classList.contains('toggle-status-chk')) {
                const isChecked = e.target.checked;
                const newStatus = isChecked ? 'Active' : 'Banned';
                
                // Commit to model
                this.model.updateUserStatus(userId, newStatus);
                
                // Trigger view updates
                this.view.updateRowStatusVisual(tr, isChecked, newStatus);
                
                // Row moves tabs dynamically: redraw table after small visual toggle transition
                setTimeout(() => {
                    this.redrawActiveTable();
                }, 250);
                return;
            }

            // 2. Check if they clicked the Actions Dots Trigger Button
            const dotsBtn = e.target.closest('.btn-dots-action');
            if (dotsBtn) {
                e.stopPropagation();

                // Requirement check: "nếu là staff thì có thêm edit role nữa"
                // If active tab is staff (or user has Admin/Moderator staff role), open edit role panel
                const isStaff = user.role === 'Admin' || user.role === 'Moderator';
                
                if (isStaff) {
                    this.model.selectedUserId = userId;
                    this.model.selectedUserRole = user.role;
                    this.view.openEditRolePopover(user.username, user.role, dotsBtn);
                } else {
                    // Regular Buyer actions (e.g. view orders, adjust scores)
                    const actionMenuHtml = `
                        <div class="buyer-actions-toast" style="position:fixed; bottom:20px; right:20px; background:#2C2630; color:#FFF; padding:12px 24px; border-radius:8px; z-index:9999; box-shadow:0 4px 12px rgba(0,0,0,0.2); font-size:0.9rem;">
                            ℹ️ Buyer @${user.username} doesn't have Staff privileges. Only Staff can have their Role adjusted.
                        </div>
                    `;
                    const existingToast = document.querySelector('.buyer-actions-toast');
                    if (existingToast) existingToast.remove();
                    
                    document.body.insertAdjacentHTML('beforeend', actionMenuHtml);
                    setTimeout(() => {
                        const toast = document.querySelector('.buyer-actions-toast');
                        if (toast) toast.remove();
                    }, 3500);
                }
            }
        };

        // Delegate to all table container panes
        this.view.panes.buyers.addEventListener('click', handleTableClick);
        this.view.panes.staff.addEventListener('click', handleTableClick);
        this.view.panes.banned.addEventListener('click', handleTableClick);
    }

    /**
     * Binds popover controls: Close buttons, Role Card Selections, password checking.
     */
    registerPopoverEvents() {
        // Popover Close Click
        this.view.popoverCloseBtn.addEventListener('click', () => {
            this.view.closeEditRolePopover();
        });

        // Selecting a new Role Card Option in the popover grid
        this.view.roleCardOptions.forEach(card => {
            card.addEventListener('click', (e) => {
                const clickedRole = card.getAttribute('data-role');
                this.model.selectedUserRole = clickedRole;
                this.view.updateRoleCardSelection(clickedRole);
                this.validatePopoverSaveButton();
            });
        });

        // Password input validation
        this.view.popoverPassword.addEventListener('input', () => {
            this.validatePopoverSaveButton();
        });

        // Submit Save changes click
        this.view.btnSaveRole.addEventListener('click', () => {
            const userId = this.model.selectedUserId;
            const newRole = this.model.selectedUserRole;
            const enteredPassword = this.view.popoverPassword.value;

            if (userId && newRole && enteredPassword.length > 0) {
                // Call model update
                const success = this.model.updateUserRole(userId, newRole);

                if (success) {
                    const user = this.model.findUserById(userId);
                    this.view.closeEditRolePopover();
                    this.redrawActiveTable();

                    // Beautiful premium notification banner
                    const successHtml = `
                        <div class="buyer-actions-toast" style="position:fixed; bottom:20px; right:20px; background:#27AE60; color:#FFF; padding:12px 24px; border-radius:8px; z-index:9999; box-shadow:0 4px 12px rgba(0,0,0,0.2); font-size:0.9rem; font-weight:600; display:flex; align-items:center; gap:8px;">
                            ✅ Success! Role for @${user.username} has been updated to ${newRole}.
                        </div>
                    `;
                    document.body.insertAdjacentHTML('beforeend', successHtml);
                    setTimeout(() => {
                        const toast = document.querySelector('.buyer-actions-toast');
                        if (toast) toast.remove();
                    }, 4000);
                }
            }
        });
    }

    /**
     * Determines whether the "Save Changes" popover button is active.
     * Checks if password has been re-entered to bypass the safety lock.
     */
    validatePopoverSaveButton() {
        const hasPassword = this.view.popoverPassword.value.trim().length > 0;
        const targetUser = this.model.findUserById(this.model.selectedUserId);
        
        let roleChanged = false;
        if (targetUser) {
            roleChanged = targetUser.role !== this.model.selectedUserRole;
        }

        // Safety lock require: password entered AND there is a valid selected role
        const canSave = hasPassword && this.model.selectedUserRole;
        this.view.toggleSaveRoleButton(canSave);
    }

    /**
     * Handles external element clicks like dismissals and add-staff.
     */
    registerGlobalEvents() {
        // Clicking outside the popover closes it
        document.addEventListener('click', (e) => {
            if (this.view.popover.style.display === 'block') {
                const insidePopover = e.target.closest('#editRolePopover');
                const insideActionsBtn = e.target.closest('.btn-dots-action');
                
                if (!insidePopover && !insideActionsBtn) {
                    this.view.closeEditRolePopover();
                }
            }
        });

        // Top Add Staff button trigger
        this.view.btnAddStaff.addEventListener('click', () => {
            const staffName = prompt("Enter username of the Buyer to promote to Staff/Moderator:");
            if (!staffName) return;

            // Search if user exists under active Buyers
            const userObj = this.model.buyers.find(b => b.username.toLowerCase() === staffName.toLowerCase());
            if (userObj) {
                // Promote to Moderator standard
                this.model.updateUserRole(userObj.id, "Moderator");
                this.redrawActiveTable();
                alert(`Successfully promoted @${staffName} to Moderator! Check the Staff tab.`);
            } else {
                alert(`Could not find active Buyer with username "${staffName}". Please verify and try again.`);
            }
        });
    }
}
