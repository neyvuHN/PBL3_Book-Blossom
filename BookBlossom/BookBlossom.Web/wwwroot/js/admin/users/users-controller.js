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
        this.registerPopoverEvents();
        this.registerGlobalEvents();
        this.registerAddStaffEvents();
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
                this.model.updateUserStatus(userId, newStatus);
                
                // Trigger view updates
                this.view.updateRowStatusVisual(tr, isChecked, newStatus);
                
                // Row moves tabs dynamically: redraw table after small visual toggle transition
                setTimeout(() => {
                    this.redrawActiveTable();
                }, 250);
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

            // 3. Check if they clicked the Update Staff Button
            const updateStaffBtn = e.target.closest('.btn-update-staff');
            if (updateStaffBtn) {
                e.stopPropagation();
                this.isUpdateMode = true;
                this.selectedStaffId = userId;
                this.view.openEditStaffModal(user);
                return;
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

        // Top Add Staff button trigger - Show custom full-screen popup modal
        this.view.btnAddStaff.addEventListener('click', () => {
            this.isUpdateMode = false;
            this.view.openAddStaffModal();
        });
    }

    /**
     * Binds all input changes, presets, cancellations, and submissions inside the Add Staff modal.
     */
    registerAddStaffEvents() {
        // Modal Cancellations
        const closeModal = () => this.view.closeAddStaffModal();
        this.view.btnCloseAddStaffModal.addEventListener('click', closeModal);
        this.view.btnCancelAddStaff.addEventListener('click', closeModal);

        // Escape key to dismiss
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && this.view.addStaffModal.classList.contains('active')) {
                closeModal();
            }
        });

        // Form Validation Utility
        const validateField = (inputEl, isValid) => {
            const formGroup = inputEl.closest('.form-group');
            if (!formGroup) return;

            const value = inputEl.value.trim();
            if (value.length === 0) {
                formGroup.classList.remove('field-valid', 'field-invalid');
            } else if (isValid) {
                formGroup.classList.remove('field-invalid');
                formGroup.classList.add('field-valid');
            } else {
                formGroup.classList.remove('field-valid');
                formGroup.classList.add('field-invalid');
            }
        };

        const validateForm = () => {
            const username = this.view.newStaffUsername.value.trim();
            const email = this.view.newStaffEmail.value.trim();
            const password = this.view.newStaffPassword.value.trim();
            const lastName = this.view.newStaffLastName.value.trim();
            const firstName = this.view.newStaffFirstName.value.trim();
            const phone = this.view.newStaffPhone.value.trim();
            const birthday = this.view.newStaffBirthday.value;
            // const position = this.view.newStaffPosition.value.trim(); /* Commented out per request */
            const position = 'Staff';
            const salary = this.view.newStaffSalary.value.trim();
            const bankAccount = this.view.newStaffBankAccount.value.trim();
            const qualifications = this.view.newStaffQualifications.value.trim();

            const isUsernameValid = username.length >= 3;
            
            // Strict email validation regex check
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            const isEmailValid = emailRegex.test(email);
            
            const isPasswordValid = password.length >= 6;
            const isLastNameValid = lastName.length > 0;
            const isFirstNameValid = firstName.length > 0;

            // Numeric 9 to 11 digit check for phone number
            const phoneRegex = /^\d{9,11}$/;
            const isPhoneValid = phoneRegex.test(phone);

            // Birthday selected and in the past
            const isBirthdayValid = birthday !== "" && new Date(birthday) < new Date();

            // const isPositionValid = position.length > 0; /* Commented out per request */
            const isPositionValid = true;
            const isSalaryValid = salary !== "" && parseFloat(salary) > 0;

            // Numeric bank account check (at least 6 digits)
            const bankRegex = /^\d{6,20}$/;
            const isBankValid = bankRegex.test(bankAccount);

            const isQualificationsValid = qualifications.length > 0;

            // Apply real-time visual feedback styles
            validateField(this.view.newStaffUsername, isUsernameValid);
            validateField(this.view.newStaffEmail, isEmailValid);
            validateField(this.view.newStaffPassword, isPasswordValid);
            validateField(this.view.newStaffLastName, isLastNameValid);
            validateField(this.view.newStaffFirstName, isFirstNameValid);
            validateField(this.view.newStaffPhone, isPhoneValid);
            validateField(this.view.newStaffBirthday, isBirthdayValid);
            // validateField(this.view.newStaffPosition, isPositionValid); /* Commented out per request */
            validateField(this.view.newStaffSalary, isSalaryValid);
            validateField(this.view.newStaffBankAccount, isBankValid);
            validateField(this.view.newStaffQualifications, isQualificationsValid);

            const isValid = isUsernameValid && isEmailValid && isPasswordValid && 
                            isLastNameValid && isFirstNameValid && isPhoneValid && 
                            isBirthdayValid && isPositionValid && isSalaryValid && 
                            isBankValid && isQualificationsValid;

            this.view.toggleSaveStaffButton(isValid);
        };

        // Bind input keystrokes and changes for real-time validation
        this.view.newStaffUsername.addEventListener('input', validateForm);
        this.view.newStaffEmail.addEventListener('input', validateForm);
        this.view.newStaffPassword.addEventListener('input', validateForm);
        this.view.newStaffLastName.addEventListener('input', validateForm);
        this.view.newStaffFirstName.addEventListener('input', validateForm);
        this.view.newStaffPhone.addEventListener('input', validateForm);
        this.view.newStaffGender.addEventListener('change', validateForm);
        this.view.newStaffBirthday.addEventListener('change', validateForm);
        // this.view.newStaffDepartment.addEventListener('change', validateForm); /* Commented out per request */
        // this.view.newStaffPosition.addEventListener('input', validateForm); /* Commented out per request */
        this.view.newStaffContractType.addEventListener('change', validateForm);
        this.view.newStaffSalary.addEventListener('input', validateForm);
        this.view.newStaffBankAccount.addEventListener('input', validateForm);
        this.view.newStaffQualifications.addEventListener('input', validateForm);

        // Bind Preset Avatar options
        this.view.avatarPresetOpts.forEach(preset => {
            preset.addEventListener('click', () => {
                // Clear custom URL
                this.view.newStaffCustomAvatar.value = '';
                
                // Toggle active class
                this.view.avatarPresetOpts.forEach(o => o.classList.remove('active'));
                preset.classList.add('active');

                // Update Preview Image
                this.view.updateAvatarPreview(preset.getAttribute('data-url'));
                validateForm();
            });
        });

        // Custom Avatar URL Input
        this.view.newStaffCustomAvatar.addEventListener('input', (e) => {
            const url = e.target.value.trim();
            if (url.length > 0) {
                // Deactivate presets
                this.view.avatarPresetOpts.forEach(o => o.classList.remove('active'));
            } else {
                // Re-activate first preset if URL is empty
                this.view.avatarPresetOpts[0].classList.add('active');
            }
            this.view.updateAvatarPreview(url);
            validateForm();
        });

        // Role Card Selections
        this.view.staffRoleCards.forEach(card => {
            card.addEventListener('click', () => {
                // Deselect other cards
                this.view.staffRoleCards.forEach(c => c.classList.remove('selected'));
                card.classList.add('selected');

                const radio = card.querySelector('input[type="radio"]');
                if (radio) radio.checked = true;
                
                validateForm();
            });
        });

        // Submit Action
        this.view.btnSaveNewStaff.addEventListener('click', () => {
            const formData = this.view.getAddStaffFormData();
            
            if (this.isUpdateMode) {
                // STAFF UPDATE FLOW
                const staffId = this.selectedStaffId;
                if (staffId) {
                    const updatedStaffObj = this.model.updateStaffMember(staffId, formData);
                    if (updatedStaffObj) {
                        this.redrawActiveTable();
                        this.view.closeAddStaffModal();

                        // Premium update notification toast
                        const toastHtml = `
                            <div class="buyer-actions-toast" style="position:fixed; bottom:20px; right:20px; background:#2F80ED; color:#FFF; padding:16px 28px; border-radius:12px; z-index:9999; box-shadow:0 10px 30px rgba(47,128,237,0.25); font-size:0.92rem; font-weight:600; display:flex; align-items:center; gap:10px; animation: slideInUp 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);">
                                <i class="ph ph-note-pencil" style="font-size:1.3rem;"></i>
                                <div>
                                    <div style="font-weight:700; margin-bottom:2px;">Staff Profile Updated!</div>
                                    <div style="font-size:0.78rem; opacity:0.9; font-weight:400;">
                                        Records for @${updatedStaffObj.username} successfully modified.
                                    </div>
                                </div>
                            </div>
                        `;
                        
                        const styleId = 'success-toast-animation';
                        if (!document.getElementById(styleId)) {
                            const style = document.createElement('style');
                            style.id = styleId;
                            style.innerHTML = `
                                @keyframes slideInUp {
                                    from { transform: translateY(100%) scale(0.9); opacity: 0; }
                                    to { transform: translateY(0) scale(1); opacity: 1; }
                                }
                            `;
                            document.head.appendChild(style);
                        }

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
                }
            } else {
                // STAFF CREATE FLOW
                const newStaffObj = this.model.addStaffMember(formData);

                if (newStaffObj) {
                    // Switch model tab & redraw
                    this.model.activeTab = 'staff';
                    this.view.updateFiltersForTab('staff');
                    this.view.showTab('staff');
                    this.redrawActiveTable();

                    // Close Modal
                    this.view.closeAddStaffModal();

                    // Gorgeous Premium Success Toast
                    const successToastHtml = `
                        <div class="buyer-actions-toast" style="position:fixed; bottom:20px; right:20px; background:#27AE60; color:#FFF; padding:16px 28px; border-radius:12px; z-index:9999; box-shadow:0 10px 30px rgba(39,174,96,0.3); font-size:0.92rem; font-weight:600; display:flex; align-items:center; gap:10px; animation: slideInUp 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);">
                            <i class="ph ph-check-circle" style="font-size:1.3rem;"></i>
                            <div>
                                <div style="font-weight:700; margin-bottom:2px;">Staff Profile Created!</div>
                                <div style="font-size:0.78rem; opacity:0.9; font-weight:400;">
                                    Account @${newStaffObj.username} initialized with 100 KPI Score.
                                </div>
                            </div>
                        </div>
                    `;
                    
                    const styleId = 'success-toast-animation';
                    if (!document.getElementById(styleId)) {
                        const style = document.createElement('style');
                        style.id = styleId;
                        style.innerHTML = `
                            @keyframes slideInUp {
                                from { transform: translateY(100%) scale(0.9); opacity: 0; }
                                to { transform: translateY(0) scale(1); opacity: 1; }
                            }
                        `;
                        document.head.appendChild(style);
                    }

                    document.body.insertAdjacentHTML('beforeend', successToastHtml);
                    setTimeout(() => {
                        const toast = document.querySelector('.buyer-actions-toast');
                        if (toast) {
                            toast.style.transition = 'all 0.4s ease';
                            toast.style.opacity = '0';
                            toast.style.transform = 'translateY(20px)';
                            setTimeout(() => toast.remove(), 400);
                        }
                    }, 4500);
                }
            }
        });
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
        this.view.btnSaveBuyerNote.addEventListener('click', () => {
            const noteText = this.view.buyerNoteText.value.trim();
            const buyerId = this.selectedBuyerId;

            if (buyerId) {
                const success = this.model.updateBuyerNote(buyerId, noteText);
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
            }
        });
    }
}
