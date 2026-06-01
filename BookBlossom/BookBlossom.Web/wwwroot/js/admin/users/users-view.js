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

        // Add Staff Full-Screen Modal Selectors
        this.addStaffModal = document.getElementById('addStaffModal');
        this.btnCloseAddStaffModal = document.getElementById('btnCloseAddStaffModal');
        this.btnCancelAddStaff = document.getElementById('btnCancelAddStaff');
        this.btnSaveNewStaff = document.getElementById('btnSaveNewStaff');
        this.avatarPresetOpts = document.querySelectorAll('.avatar-preset-opt');
        this.newStaffCustomAvatar = document.getElementById('newStaffCustomAvatar');
        this.newStaffAvatarPreview = document.getElementById('newStaffAvatarPreview');
        this.newStaffUsername = document.getElementById('newStaffUsername');
        this.newStaffEmail = document.getElementById('newStaffEmail');
        this.newStaffPassword = document.getElementById('newStaffPassword');
        this.staffRoleCards = document.querySelectorAll('.staff-role-card');
        this.newStaffQualifications = document.getElementById('newStaffQualifications');
        this.newStaffHireDate = document.getElementById('newStaffHireDate');

        // New Personal Profile & Professional HR Form Selectors
        this.newStaffLastName = document.getElementById('newStaffLastName');
        this.newStaffFirstName = document.getElementById('newStaffFirstName');
        this.newStaffPhone = document.getElementById('newStaffPhone');
        this.newStaffGender = document.getElementById('newStaffGender');
        this.newStaffBirthday = document.getElementById('newStaffBirthday');
        this.newStaffDepartment = document.getElementById('newStaffDepartment');
        this.newStaffPosition = document.getElementById('newStaffPosition');
        this.newStaffContractType = document.getElementById('newStaffContractType');
        this.newStaffSalary = document.getElementById('newStaffSalary');
        this.newStaffBankAccount = document.getElementById('newStaffBankAccount');

        // Dynamic Header Elements inside Add Staff Modal
        this.addStaffHeaderTitle = this.addStaffModal.querySelector('.form-header h2');
        this.addStaffHeaderDesc = this.addStaffModal.querySelector('.form-header p');

        // Buyer Notes Modal Selectors
        this.buyerNoteModal = document.getElementById('buyerNoteModal');
        this.btnCloseBuyerNoteModal = document.getElementById('btnCloseBuyerNoteModal');
        this.btnCancelBuyerNote = document.getElementById('btnCancelBuyerNote');
        this.btnSaveBuyerNote = document.getElementById('btnSaveBuyerNote');
        this.buyerNoteText = document.getElementById('buyerNoteText');
        this.noteModalUsername = document.getElementById('noteModalUsername');
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
            user.role === 'Moderator' ? 'role-moderator' :
            user.role === 'Marketing Manager' ? 'role-marketing' :
            user.role === 'Store Manager' ? 'role-store' : 'role-user';

        const planClass = user.plan === 'Basic' ? 'plan-basic' :
            user.plan === 'Pro' ? 'plan-pro' : 'plan-free';

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
                <td style="position: relative; text-align: center;">
                    <button class="action-trigger-btn btn-buyer-note" title="${user.note && user.note.trim().length > 0 ? 'Edit Note' : 'Add Note'}" style="margin-right: 6px;">
                        <i class="ph ${user.note && user.note.trim().length > 0 ? 'ph-note-pencil note-active' : 'ph-note'}"></i>
                    </button>
                    <button class="action-trigger-btn btn-dots-action" title="Edit Role">
                        <i class="ph ph-user-gear"></i>
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
            user.role === 'Moderator' ? 'role-moderator' :
            user.role === 'Marketing Manager' ? 'role-marketing' :
            user.role === 'Store Manager' ? 'role-store' : 'role-user';

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
                <td style="position: relative; text-align: center;">
                    <button class="action-trigger-btn btn-update-staff" title="Update Staff Info" style="margin-right: 6px;">
                        <i class="ph ph-pencil-simple"></i>
                    </button>
                    <button class="action-trigger-btn btn-dots-action" title="Edit Role">
                        <i class="ph ph-user-gear"></i>
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
        } else if (tabName === 'staff') {
            /* Commented out per request: Bỏ luôn filter theo role
            this.roleFilter.disabled = false;
            this.roleFilter.innerHTML = `
                <option value="">Role: All Staff</option>
                <option value="Admin">Admin</option>
                <option value="Moderator">Moderator</option>
                <option value="Marketing Manager">Marketing Manager</option>
                <option value="Store Manager">Store Manager</option>
            `;
            */
            
            this.scoreFilter.innerHTML = `
                <option value="">Internal Score</option>
                <option value="high">High (>80)</option>
                <option value="caution">Critical (<50)</option>
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
     * Opens the Add New Staff Full Screen modal and resets the form to pristine defaults.
     */
    openAddStaffModal() {
        this.addStaffModal.classList.add('active');

        // Restore form headers and save buttons to "Create Mode" defaults
        if (this.addStaffHeaderTitle) this.addStaffHeaderTitle.textContent = "Add New Staff";
        if (this.addStaffHeaderDesc) this.addStaffHeaderDesc.textContent = "Complete the profile information below to create the new account. All fields are required.";
        if (this.btnSaveNewStaff) this.btnSaveNewStaff.innerHTML = '<i class="ph ph-user-plus-bold"></i> Save Staff Profile';

        // Reset validation classes on all form-groups
        const groups = this.addStaffModal.querySelectorAll('.form-group');
        groups.forEach(g => {
            g.classList.remove('field-valid', 'field-invalid');
        });

        // Reset text fields
        this.newStaffUsername.value = '';
        this.newStaffEmail.value = '';
        this.newStaffPassword.value = '';
        this.newStaffQualifications.value = '';
        this.newStaffCustomAvatar.value = '';

        // Reset new personal and professional inputs
        this.newStaffLastName.value = '';
        this.newStaffFirstName.value = '';
        this.newStaffPhone.value = '';
        this.newStaffGender.value = 'Male';
        this.newStaffBirthday.value = '';
        // this.newStaffDepartment.value = 'Administration'; /* Commented out per request */
        // this.newStaffPosition.value = ''; /* Commented out per request */
        this.newStaffContractType.value = 'Full-time';
        this.newStaffSalary.value = '';
        this.newStaffBankAccount.value = '';

        // Reset avatar presets (Select first as default)
        this.avatarPresetOpts.forEach((opt, idx) => {
            if (idx === 0) {
                opt.classList.add('active');
                this.newStaffAvatarPreview.src = opt.getAttribute('data-url');
            } else {
                opt.classList.remove('active');
            }
        });

        // Reset roles grid (Select Admin as default)
        this.staffRoleCards.forEach((card, idx) => {
            const radio = card.querySelector('input[type="radio"]');
            if (idx === 0) {
                card.classList.add('selected');
                if (radio) radio.checked = true;
            } else {
                card.classList.remove('selected');
                if (radio) radio.checked = false;
            }
        });

        // Populate dynamic auto hire date
        const today = new Date();
        const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
        const formattedDate = `${months[today.getMonth()]} ${String(today.getDate()).padStart(2, '0')}, ${today.getFullYear()}`;
        this.newStaffHireDate.textContent = formattedDate;

        // Force disable save button initially
        this.toggleSaveStaffButton(false);
    }

    /**
     * Closes the Add New Staff Modal overlay.
     */
    closeAddStaffModal() {
        this.addStaffModal.classList.remove('active');
    }

    /**
     * Updates the avatar preview source.
     */
    updateAvatarPreview(url) {
        if (url && url.trim().length > 0) {
            this.newStaffAvatarPreview.src = url;
        } else {
            // Revert to selected preset if input cleared
            const activePreset = Array.from(this.avatarPresetOpts).find(opt => opt.classList.contains('active'));
            if (activePreset) {
                this.newStaffAvatarPreview.src = activePreset.getAttribute('data-url');
            }
        }
    }

    /**
     * Enables or disables the Save Staff Profile submit button.
     */
    toggleSaveStaffButton(enabled) {
        this.btnSaveNewStaff.disabled = !enabled;
    }

    /**
     * Gathers all the validated information entered in the Add Staff form.
     */
    getAddStaffFormData() {
        const activePreset = Array.from(this.avatarPresetOpts).find(opt => opt.classList.contains('active'));
        const avatarUrl = this.newStaffCustomAvatar.value.trim() || (activePreset ? activePreset.getAttribute('data-url') : 'https://i.pravatar.cc/150?img=1');
        
        const checkedRadio = Array.from(this.staffRoleCards)
            .map(card => card.querySelector('input[type="radio"]'))
            .find(r => r && r.checked);
        const role = checkedRadio ? checkedRadio.value : 'Admin';

        return {
            username: this.newStaffUsername.value.trim(),
            email: this.newStaffEmail.value.trim(),
            password: this.newStaffPassword.value.trim(),
            lastName: this.newStaffLastName.value.trim(),
            firstName: this.newStaffFirstName.value.trim(),
            phoneNumber: this.newStaffPhone.value.trim(),
            gender: this.newStaffGender.value,
            birthday: this.newStaffBirthday.value,
            department: 'Administration', /* this.newStaffDepartment.value commented out per request */
            position: 'Staff', /* this.newStaffPosition.value.trim() commented out per request */
            contractType: this.newStaffContractType.value,
            salary: parseFloat(this.newStaffSalary.value) || 0,
            bankAccount: this.newStaffBankAccount.value.trim(),
            role: role,
            avatarUrl: avatarUrl,
            qualifications: this.newStaffQualifications.value.trim()
        };
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

    /**
     * Switches the Add Staff modal into "Update Mode" and populates all existing records.
     */
    openEditStaffModal(staffObj) {
        this.addStaffModal.classList.add('active');

        // Reset validation outline classes
        const groups = this.addStaffModal.querySelectorAll('.form-group');
        groups.forEach(g => g.classList.remove('field-valid', 'field-invalid'));

        // Prefill modal header text & submit button
        if (this.addStaffHeaderTitle) this.addStaffHeaderTitle.textContent = "Update Staff Profile";
        if (this.addStaffHeaderDesc) this.addStaffHeaderDesc.textContent = "Modify credentials and profile settings for this staff member.";
        if (this.btnSaveNewStaff) this.btnSaveNewStaff.innerHTML = '<i class="ph ph-note-pencil"></i> Update Staff Profile';

        // Pre-fill text and select inputs
        this.newStaffUsername.value = staffObj.username || '';
        this.newStaffEmail.value = staffObj.email || '';
        this.newStaffPassword.value = staffObj.password || '123456'; // Use existing or valid template placeholder password
        
        this.newStaffLastName.value = staffObj.lastName || '';
        this.newStaffFirstName.value = staffObj.firstName || '';
        this.newStaffPhone.value = staffObj.phoneNumber || '';
        this.newStaffGender.value = staffObj.gender || 'Male';
        this.newStaffBirthday.value = staffObj.birthday || '';
        // this.newStaffDepartment.value = staffObj.department || 'Administration'; /* Commented out per request */
        // this.newStaffPosition.value = staffObj.position || ''; /* Commented out per request */
        this.newStaffContractType.value = staffObj.contractType || 'Full-time';
        this.newStaffSalary.value = staffObj.salary || '';
        this.newStaffBankAccount.value = staffObj.bankAccount || '';
        this.newStaffQualifications.value = staffObj.qualifications || '';
        this.newStaffHireDate.textContent = staffObj.joinDate || '';

        // Pre-select avatar preset or custom URL
        let matchedPreset = false;
        this.avatarPresetOpts.forEach(opt => {
            const url = opt.getAttribute('data-url');
            if (url === staffObj.avatarUrl) {
                opt.classList.add('active');
                matchedPreset = true;
            } else {
                opt.classList.remove('active');
            }
        });

        if (matchedPreset) {
            this.newStaffCustomAvatar.value = '';
        } else {
            this.newStaffCustomAvatar.value = staffObj.avatarUrl || '';
        }
        
        this.updateAvatarPreview(staffObj.avatarUrl);

        // Pre-select roles radio card
        this.staffRoleCards.forEach(card => {
            const role = card.getAttribute('data-role');
            const radio = card.querySelector('input[type="radio"]');
            
            if (role === staffObj.role) {
                card.classList.add('selected');
                if (radio) radio.checked = true;
            } else {
                card.classList.remove('selected');
                if (radio) radio.checked = false;
            }
        });

        // Trigger check so Save button is active immediately
        this.toggleSaveStaffButton(true);
    }
}
