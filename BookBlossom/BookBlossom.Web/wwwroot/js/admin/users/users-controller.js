/**
 * Frontend MVC - Controller
 * Glues the Model and View together. Registers events, intercepts actions, and synchronizes state updates.
 */
class UsersController {
    constructor(model, view) {
        this.model = model;
        this.view = view;

        // Custom operational states for Buyer Notes and Staff Editing
        this.isUpdateMode = false;
        this.selectedStaffId = null;
        this.selectedBuyerId = null;
    }

    /**
     * Entry point to wire event listeners and render the initial page view.
     */
    init() {
        this.registerTabEvents();
        this.registerFilterEvents();
        this.registerTableEvents();
        this.registerGlobalEvents();
        this.registerBuyerNoteEvents();

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
                // this.view.roleFilter.value = ''; /* Commented out per request: Bỏ filter theo role */
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

        /* Commented out per request: Bỏ luôn filter theo role
        // Role dropdown filter selection
        this.view.roleFilter.addEventListener('change', (e) => {
            this.model.filterRole = e.target.value;
            this.redrawActiveTable();
        });
        */

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
                this.model.updateUserStatus(userId, newStatus).then(success => {
                    if (success) {
                        // Trigger view updates
                        this.view.updateRowStatusVisual(tr, isChecked, newStatus);
                        
                        // Row moves tabs dynamically: redraw table after small visual toggle transition
                        setTimeout(() => {
                            this.redrawActiveTable();
                        }, 250);
                    } else {
                        e.target.checked = !isChecked;
                    }
                }).catch(err => {
                    e.target.checked = !isChecked;
                    if (window.apiClient && window.apiClient.showToast) {
                        window.apiClient.showToast("Failed to update status: " + (err.message || err), "error");
                    }
                });
                return;
            }

            // 2. Check if they clicked the Buyer Note Button
            const noteBtn = e.target.closest('.btn-buyer-note');
            if (noteBtn) {
                e.stopPropagation();
                this.selectedBuyerId = userId;
                this.view.openBuyerNoteModal(user.username, user.note || '');
                return;
            }


        };

        // Delegate to all table container panes
        this.view.panes.buyers.addEventListener('click', handleTableClick);
        this.view.panes.banned.addEventListener('click', handleTableClick);
    }

    /**


    /**
     * Handles external element clicks like dismissals and add-staff.
     */
    registerGlobalEvents() {
    }



    /**
     * Binds all event listeners for the Buyer Log Note modal dialog.
     */
    registerBuyerNoteEvents() {
        const closeNoteModal = () => this.view.closeBuyerNoteModal();
        this.view.btnCloseBuyerNoteModal.addEventListener('click', closeNoteModal);
        this.view.btnCancelBuyerNote.addEventListener('click', closeNoteModal);

        // Escape key press to dismiss
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.view.buyerNoteModal.classList.contains('active')) {
                closeNoteModal();
            }
        });

        // Click outside the card to close
        this.view.buyerNoteModal.addEventListener('click', (e) => {
            if (e.target === this.view.buyerNoteModal) {
                closeNoteModal();
            }
        });

        // Save note event
        this.view.btnSaveBuyerNote.addEventListener('click', async () => {
            const noteText = this.view.buyerNoteText.value.trim();
            const buyerId = this.selectedBuyerId;

            if (buyerId) {
                try {
                    const success = await this.model.updateBuyerNote(buyerId, noteText);
                    if (success) {
                        const buyerObj = this.model.findUserById(buyerId);
                        this.redrawActiveTable();
                        closeNoteModal();

                        // Beautiful premium notification toast
                        const toastHtml = `
                            <div class="buyer-actions-toast" style="position:fixed; bottom:20px; right:20px; background:#27AE60; color:#FFF; padding:16px 28px; border-radius:12px; z-index:9999; box-shadow:0 10px 30px rgba(39,174,96,0.25); font-size:0.92rem; font-weight:600; display:flex; align-items:center; gap:10px; animation: slideInUp 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);">
                                <i class="ph ph-check-circle" style="font-size:1.3rem;"></i>
                                <div>
                                    <div style="font-weight:700; margin-bottom:2px;">Notes Saved Successfully!</div>
                                    <div style="font-size:0.78rem; opacity:0.9; font-weight:400;">
                                        Observations for @${buyerObj.username} have been committed.
                                    </div>
                                </div>
                            </div>
                        `;
                        document.body.insertAdjacentHTML('beforeend', toastHtml);
                        setTimeout(() => {
                            const toast = document.querySelector('.buyer-actions-toast');
                            if (toast) {
                                toast.style.transition = 'all 0.4s ease';
                                toast.style.opacity = '0';
                                toast.style.transform = 'translateY(20px)';
                                setTimeout(() => toast.remove(), 400);
                            }
                        }, 4000);
                    }
                } catch (err) {
                    console.error("Error saving buyer note:", err);
                    if (window.apiClient && window.apiClient.showToast) {
                        window.apiClient.showToast("Failed to save note: " + (err.message || err), "error");
                    } else {
                        alert("Failed to save note: " + err.message);
                    }
                }
            }
        });
    }
}
