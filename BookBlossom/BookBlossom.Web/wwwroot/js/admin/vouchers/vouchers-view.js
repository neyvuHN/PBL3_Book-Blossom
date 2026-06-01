class VouchersView {
    constructor() {
        this.grid = document.getElementById('vouchersGrid');
        this.modal = document.getElementById('voucherModal');
        this.form = document.getElementById('voucherForm');

        // Buttons
        this.btnCreate = document.getElementById('btnCreateVoucher');
        this.btnCloseModal = document.getElementById('btnCloseModal');
        this.btnCancelModal = document.getElementById('btnCancelModal');
        this.btnSave = document.getElementById('btnSaveVoucher');

        // Filters
        this.filterStatus = document.getElementById('filterStatus');
        this.searchVoucher = document.getElementById('searchVoucher');

        // Type Select
        this.vType = document.getElementById('vType');
        this.maxDiscountGroup = document.getElementById('maxDiscountGroup');

        // Scope
        this.vScope = document.getElementById('vScope');
        this.scopeSelectionButtons = document.getElementById('scopeSelectionButtons');
        this.btnSelectCategories = document.getElementById('btnSelectCategories');
        this.btnSelectBooks = document.getElementById('btnSelectBooks');

        // Selection Modal
        this.selectionModal = document.getElementById('selectionModal');
        this.selectionModalTitle = document.getElementById('selectionModalTitle');
        this.selectionList = document.getElementById('selectionList');
        this.selectionSearch = document.getElementById('selectionSearch');
        this.btnConfirmSelection = document.getElementById('btnConfirmSelection');
        this.btnCloseSelectionModal = document.getElementById('btnCloseSelectionModal');

        this.selectedCategories = [];
        this.selectedBooks = [];
        this.currentSelectionType = null; // 'Categories' or 'Books'
        this.tempSelection = [];

        this.vType.addEventListener('change', () => this.toggleMaxDiscountGroup());
        this.vScope.addEventListener('change', () => this.toggleScopeButtons());

        this.selectionSearch.addEventListener('input', (e) => this.filterSelectionList(e.target.value));

        this.btnCloseSelectionModal.addEventListener('click', () => this.closeSelectionModal());
        this.btnConfirmSelection.addEventListener('click', () => this.confirmSelection());
    }

    toggleMaxDiscountGroup() {
        if (this.vType.value === 'Percentage') {
            this.maxDiscountGroup.style.display = 'flex';
        } else {
            this.maxDiscountGroup.style.display = 'none';
        }
    }

    toggleScopeButtons() {
        const scope = this.vScope.value;
        this.scopeSelectionButtons.style.display = scope === 'All' ? 'none' : 'flex';

        if (scope === 'SpecificCategory') {
            this.btnSelectCategories.style.display = 'block';
            this.btnSelectBooks.style.display = 'none';
        } else if (scope === 'SpecificBook') {
            this.btnSelectCategories.style.display = 'none';
            this.btnSelectBooks.style.display = 'block';
        } else if (scope === 'Both') {
            this.btnSelectCategories.style.display = 'block';
            this.btnSelectBooks.style.display = 'block';
        }
    }

    updateScopeButtonsText() {
        this.btnSelectCategories.innerHTML = `<i class="ph ph-list"></i> Select Categories (${this.selectedCategories.length})`;
        this.btnSelectBooks.innerHTML = `<i class="ph ph-books"></i> Select Books (${this.selectedBooks.length})`;
    }

    bindCreateVoucher(handler) {
        this.btnCreate.addEventListener('click', () => {
            this.form.reset();
            document.getElementById('vId').value = '';
            document.getElementById('modalTitle').textContent = 'Create New Voucher';
            this.selectedCategories = [];
            this.selectedBooks = [];
            this.toggleMaxDiscountGroup();
            this.toggleScopeButtons();
            this.updateScopeButtonsText();
            this.modal.style.display = 'flex';
        });
    }

    bindCloseModal() {
        const closeFn = () => { this.modal.style.display = 'none'; };
        this.btnCloseModal.addEventListener('click', closeFn);
        this.btnCancelModal.addEventListener('click', (e) => {
            e.preventDefault();
            closeFn();
        });
    }

    bindSaveVoucher(handler) {
        this.btnSave.addEventListener('click', (e) => {
            e.preventDefault();
            if (!this.form.checkValidity()) {
                this.form.reportValidity();
                return;
            }

            const type = document.getElementById('vType').value;
            const value = parseFloat(document.getElementById('vValue').value);
            const budget = parseInt(document.getElementById('vBudget').value);
            const minOrder = parseFloat(document.getElementById('vMinOrder').value);
            const maxDiscount = parseFloat(document.getElementById('vMaxDiscount').value || 0);
            const startDate = document.getElementById('vStartDate').value;
            const endDate = document.getElementById('vEndDate').value;

            // Validation rules
            if (value <= 0) {
                if (window.apiClient && window.apiClient.showToast) window.apiClient.showToast("Discount value must be greater than 0.", "error");
                else alert("Discount value must be greater than 0.");
                return;
            }
            if (type === 'Percentage' && value > 100) {
                if (window.apiClient && window.apiClient.showToast) window.apiClient.showToast("Percentage discount cannot exceed 100%.", "error");
                else alert("Percentage discount cannot exceed 100%.");
                return;
            }
            if (budget <= 0) {
                if (window.apiClient && window.apiClient.showToast) window.apiClient.showToast("Total budget/quantity must be greater than 0.", "error");
                else alert("Total budget/quantity must be greater than 0.");
                return;
            }
            if (minOrder < 0) {
                if (window.apiClient && window.apiClient.showToast) window.apiClient.showToast("Minimum order value cannot be negative.", "error");
                else alert("Minimum order value cannot be negative.");
                return;
            }
            if (new Date(startDate) >= new Date(endDate)) {
                if (window.apiClient && window.apiClient.showToast) window.apiClient.showToast("End date must be strictly after the start date.", "error");
                else alert("End date must be strictly after the start date.");
                return;
            }

            const voucher = {
                id: document.getElementById('vId').value ? parseInt(document.getElementById('vId').value) : null,
                campaignName: document.getElementById('vCampaignName').value,
                code: document.getElementById('vCode').value,
                type: type,
                value: value,
                maxDiscount: maxDiscount,
                minOrder: minOrder,
                minPlan: document.getElementById('vMinPlan').value,
                minScore: parseInt(document.getElementById('vMinScore').value || 0),
                minRank: document.getElementById('vMinRank').value,
                budget: budget,
                scope: document.getElementById('vScope').value,
                startDate: startDate,
                endDate: endDate,
                stackable: document.getElementById('vStackable').checked,
                autoRestore: document.getElementById('vAutoRestore').checked,
                status: document.getElementById('vStatus').value,
                selectedCategories: [...this.selectedCategories],
                selectedBooks: [...this.selectedBooks]
            };

            handler(voucher);
        });
    }

    // --- Selection Modal Logic ---

    bindSelectCategories(handler) {
        this.btnSelectCategories.addEventListener('click', () => handler());
    }

    bindSelectBooks(handler) {
        this.btnSelectBooks.addEventListener('click', () => handler());
    }

    openSelectionModal(type, data) {
        this.currentSelectionType = type;
        this.selectionModalTitle.textContent = type === 'Categories' ? 'Select Categories' : 'Select Books';
        this.tempSelection = type === 'Categories' ? [...this.selectedCategories] : [...this.selectedBooks];
        this.selectionSearch.value = '';
        this.renderSelectionList(data, '');
        this.selectionModal.style.display = 'flex';
    }

    closeSelectionModal() {
        this.selectionModal.style.display = 'none';
        this.currentSelectionType = null;
    }

    confirmSelection() {
        if (this.currentSelectionType === 'Categories') {
            this.selectedCategories = [...this.tempSelection];
            this.updateScopeButtonsText();
        } else if (this.currentSelectionType === 'Books') {
            this.selectedBooks = [...this.tempSelection];
            this.updateScopeButtonsText();
        }
        this.closeSelectionModal();
    }

    filterSelectionList(searchTerm) {
        const items = this.selectionList.querySelectorAll('.selection-item');
        const term = searchTerm.toLowerCase();
        items.forEach(item => {
            const text = item.textContent.toLowerCase();
            item.style.display = text.includes(term) ? 'flex' : 'none';
        });
    }

    renderSelectionList(data, searchTerm) {
        this.selectionList.innerHTML = '';
        data.forEach(item => {
            const isChecked = this.tempSelection.includes(item.id);
            const name = item.name || item.title;
            const div = document.createElement('div');
            div.className = 'selection-item';
            div.style.cssText = 'display: flex; align-items: center; padding: 8px; border-bottom: 1px solid #f3f4f6; cursor: pointer;';
            div.innerHTML = `
                <input type="checkbox" id="sel_${item.id}" value="${item.id}" ${isChecked ? 'checked' : ''} style="margin-right: 12px; width: auto;">
                <label for="sel_${item.id}" style="margin: 0; cursor: pointer; flex: 1;">${name}</label>
            `;

            const checkbox = div.querySelector('input');
            checkbox.addEventListener('change', (e) => {
                if (e.target.checked) {
                    this.tempSelection.push(item.id);
                } else {
                    this.tempSelection = this.tempSelection.filter(id => id !== item.id);
                }
            });

            this.selectionList.appendChild(div);
        });
    }

    // --- End Selection Modal Logic ---

    bindEditVoucher(handler) {
        this.grid.addEventListener('click', (e) => {
            if (e.target.closest('.btn-edit')) {
                const id = e.target.closest('.btn-edit').dataset.id;
                handler(id);
            }
        });
    }

    bindDeleteVoucher(handler) {
        this.grid.addEventListener('click', (e) => {
            if (e.target.closest('.btn-delete')) {
                if (confirm('Are you sure you want to delete this voucher?')) {
                    const id = e.target.closest('.btn-delete').dataset.id;
                    handler(id);
                }
            }
        });
    }

    bindFilters(handler) {
        this.filterStatus.addEventListener('change', () => handler());
        this.searchVoucher.addEventListener('input', () => handler());
    }

    getFilterValues() {
        return {
            status: this.filterStatus.value,
            searchTerm: this.searchVoucher.value.toLowerCase()
        };
    }

    renderVouchers(vouchers, categories = [], books = []) {
        this.grid.innerHTML = '';
        if (vouchers.length === 0) {
            this.grid.innerHTML = '<div style="grid-column: 1/-1; text-align: center; padding: 40px; color: #6b7280;">No vouchers found.</div>';
            return;
        }

        vouchers.forEach(v => {
            const usageRate = v.budget > 0 ? Math.round((v.used / v.budget) * 100) : 0;
            const card = document.createElement('div');
            card.className = 'voucher-card';

            const statusClass = `status-${v.status.toLowerCase()}`;
            const formatCurrency = (val) => new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
            const displayValue = v.type === 'Percentage' ? `${v.value}%` : formatCurrency(v.value);

            // Stackable & Auto-Restore badges
            const stackableBadge = v.stackable
                ? '<span class="rules-badge stackable-yes"><i class="ph ph-check-circle"></i> Stackable</span>'
                : '<span class="rules-badge rules-no"><i class="ph ph-prohibit"></i> Non-Stackable</span>';
            const autoRestoreBadge = v.autoRestore
                ? '<span class="rules-badge restore-yes"><i class="ph ph-check-circle"></i> Auto-Restore</span>'
                : '<span class="rules-badge rules-no"><i class="ph ph-prohibit"></i> No Restore</span>';

            // Resolve scope items
            let scopeDisplay = '';
            if (v.scope === 'All') {
                scopeDisplay = '<span class="badge-scope scope-all">All Books</span>';
            } else if (v.scope === 'SpecificCategory') {
                const names = (v.selectedCategories || []).map(id => {
                    const cat = categories.find(c => c.id === id);
                    return cat ? cat.name : `Cat #${id}`;
                });
                scopeDisplay = `<span class="badge-scope scope-cat">Categories:</span> <span style="font-weight: 500;">${names.join(', ') || 'None selected'}</span>`;
            } else if (v.scope === 'SpecificBook') {
                const titles = (v.selectedBooks || []).map(id => {
                    const book = books.find(b => b.id === id);
                    return book ? book.title : `Book #${id}`;
                });
                scopeDisplay = `<span class="badge-scope scope-book">Books:</span> <span style="font-weight: 500;">${titles.join(', ') || 'None selected'}</span>`;
            } else if (v.scope === 'Both') {
                const catNames = (v.selectedCategories || []).map(id => {
                    const cat = categories.find(c => c.id === id);
                    return cat ? cat.name : `Cat #${id}`;
                });
                const bookTitles = (v.selectedBooks || []).map(id => {
                    const book = books.find(b => b.id === id);
                    return book ? book.title : `Book #${id}`;
                });
                scopeDisplay = `
                    <div style="margin-bottom: 4px;"><span class="badge-scope scope-cat">Categories:</span> <span style="font-weight: 500;">${catNames.join(', ') || 'None'}</span></div>
                    <div><span class="badge-scope scope-book">Books:</span> <span style="font-weight: 500;">${bookTitles.join(', ') || 'None'}</span></div>
                `;
            }

            // Progress bar
            const progressPercentage = usageRate > 100 ? 100 : usageRate;
            const progressBarColor = v.status === 'Active' ? '#10b981' : '#9ca3af';

            card.innerHTML = `
                <!-- Top Section -->
                <div>
                    <div class="voucher-status ${statusClass}">${v.status}</div>
                    <div class="voucher-campaign" style="font-size: 11px; font-weight: 700; color: #8b5cf6; text-transform: uppercase; margin-bottom: 4px; letter-spacing: 0.5px;">${v.campaignName || 'General Campaign'}</div>
                    <div class="voucher-code" style="margin-top: 0; line-height: 1.2; font-size: 22px; letter-spacing: 0.5px; font-family: monospace;">${v.code}</div>
                    <div class="voucher-type" style="font-weight: 600; color: #374151; margin-bottom: 12px; font-size: 14px;">${v.type === 'Percentage' ? 'Percentage Discount' : 'Fixed Amount'} - <span style="color: #8b5cf6; font-weight: 700;">${displayValue}</span></div>
                    
                    <!-- Financial Setup -->
                    <div style="font-size: 13px; color: #4b5563; margin-bottom: 10px; background: #f9fafb; padding: 8px 12px; border-radius: 8px; border: 1px solid #f3f4f6;">
                        <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
                            <span><i class="ph ph-shopping-cart" style="color: #6b7280;"></i> Min Order:</span>
                            <span style="font-weight: 600; color: #111827;">${formatCurrency(v.minOrder)}</span>
                        </div>
                        ${v.type === 'Percentage' ? `
                        <div style="display: flex; justify-content: space-between; margin-bottom: 4px;">
                            <span><i class="ph ph-hand-coins" style="color: #6b7280;"></i> Max Discount:</span>
                            <span style="font-weight: 600; color: #111827;">${formatCurrency(v.maxDiscount || 0)}</span>
                        </div>` : ''}
                        <div style="display: flex; justify-content: space-between;">
                            <span><i class="ph ph-ticket" style="color: #6b7280;"></i> Total Quantity:</span>
                            <span style="font-weight: 600; color: #111827;">${v.budget}</span>
                        </div>

                    </div>

                    <!-- Targeting Criteria -->
                    <div style="font-size: 13px; color: #4b5563; margin-bottom: 10px; padding: 4px 6px;">
                        <div style="margin-bottom: 4px;"><i class="ph ph-users-three" style="color: #8b5cf6;"></i> <strong>Targeting:</strong></div>
                        <div style="padding-left: 18px; line-height: 1.5; color: #374151;">
                            Plan &ge; <span style="font-weight: 600;">${v.minPlan}</span> | Score &ge; <span style="font-weight: 600;">${v.minScore}</span> | Rank &ge; <span style="font-weight: 600;">${v.minRank || 'None'}</span>
                        </div>
                    </div>

                    <!-- Rules Badges -->
                    <div style="display: flex; gap: 6px; flex-wrap: wrap; margin-bottom: 12px;">
                        ${stackableBadge}
                        ${autoRestoreBadge}
                    </div>

                    <!-- Scope Section -->
                    <div style="font-size: 13px; color: #4b5563; margin-bottom: 6px; padding: 4px 6px;">
                        <div style="margin-bottom: 4px;"><i class="ph ph-books" style="color: #f59e0b;"></i> <strong>Apply Scope:</strong></div>
                        <div style="padding-left: 18px; line-height: 1.4; color: #374151;">
                            ${scopeDisplay}
                        </div>
                    </div>
                </div>

                <!-- Ticket Divider (Dashed Coupon Effect) -->
                <div class="voucher-ticket-divider"></div>

                <!-- Bottom Section -->
                <div>
                    <!-- Duration -->
                    <div style="font-size: 12px; color: #6b7280; display: flex; align-items: center; gap: 6px; margin-bottom: 12px;">
                        <i class="ph ph-calendar" style="font-size: 14px;"></i>
                        <span>Active: <strong>${new Date(v.startDate).toLocaleDateString()}</strong> - <strong>${new Date(v.endDate).toLocaleDateString()}</strong></span>
                    </div>

                    <!-- Budget Progress Bar -->
                    <div style="margin-bottom: 14px;">
                        <div style="display: flex; justify-content: space-between; font-size: 12px; color: #4b5563; margin-bottom: 2px;">
                            <span>Usage Rate</span>
                            <span style="font-weight: 600;">${usageRate}% (${v.used}/${v.budget})</span>
                        </div>
                        <div style="background: #f3f4f6; border-radius: 999px; height: 6px; width: 100%; overflow: hidden; position: relative;">
                            <div style="background: ${progressBarColor}; width: ${progressPercentage}%; height: 100%; border-radius: 999px; transition: width 0.3s ease;"></div>
                        </div>
                    </div>

                    <!-- Est ROI & Actions -->
                    <div style="display: flex; justify-content: space-between; align-items: center; padding-top: 8px; border-top: 1px solid #f3f4f6;">
                        <div>
                            <div style="font-size: 10px; color: #9ca3af; text-transform: uppercase; font-weight: 600; letter-spacing: 0.5px;">Est. ROI</div>
                            <div style="font-size: 16px; font-weight: 700; color: #10b981;">${v.roi}</div>
                        </div>
                        <div style="display: flex; gap: 8px; width: auto;">
                            <button class="btn-action btn-edit" data-id="${v.id}" style="padding: 6px 12px; border-radius: 6px; font-size: 12px; font-weight: 600; flex: none;"><i class="ph ph-pencil"></i> Edit</button>
                            <button class="btn-action btn-delete" data-id="${v.id}" style="color: #ef4444; padding: 6px 12px; border-radius: 6px; font-size: 12px; font-weight: 600; flex: none;"><i class="ph ph-trash"></i> Delete</button>
                        </div>
                    </div>
                </div>
            `;
            this.grid.appendChild(card);
        });
    }

    openEditModal(voucher) {
        document.getElementById('modalTitle').textContent = 'Edit Voucher';
        document.getElementById('vId').value = voucher.id;
        document.getElementById('vCampaignName').value = voucher.campaignName || '';
        document.getElementById('vCode').value = voucher.code;
        document.getElementById('vType').value = voucher.type;
        document.getElementById('vValue').value = voucher.value;
        document.getElementById('vMaxDiscount').value = voucher.maxDiscount;
        document.getElementById('vMinOrder').value = voucher.minOrder;
        document.getElementById('vMinPlan').value = voucher.minPlan;
        document.getElementById('vMinScore').value = voucher.minScore;
        document.getElementById('vMinRank').value = voucher.minRank || 'None';
        document.getElementById('vBudget').value = voucher.budget;
        document.getElementById('vScope').value = voucher.scope;
        document.getElementById('vStartDate').value = voucher.startDate;
        document.getElementById('vEndDate').value = voucher.endDate;
        document.getElementById('vStackable').checked = voucher.stackable;
        document.getElementById('vAutoRestore').checked = voucher.autoRestore || false;
        document.getElementById('vStatus').value = voucher.status;

        this.selectedCategories = voucher.selectedCategories ? [...voucher.selectedCategories] : [];
        this.selectedBooks = voucher.selectedBooks ? [...voucher.selectedBooks] : [];

        this.toggleMaxDiscountGroup();
        this.toggleScopeButtons();
        this.updateScopeButtonsText();
        this.modal.style.display = 'flex';
    }

    closeModal() {
        this.modal.style.display = 'none';
        this.form.reset();
    }
}
