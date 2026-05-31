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

        this.vType.addEventListener('change', () => {
            this.toggleMaxDiscountGroup();
        });
    }

    toggleMaxDiscountGroup() {
        if (this.vType.value === 'Percentage') {
            this.maxDiscountGroup.style.display = 'flex';
        } else {
            this.maxDiscountGroup.style.display = 'none';
        }
    }

    bindCreateVoucher(handler) {
        this.btnCreate.addEventListener('click', () => {
            this.form.reset();
            document.getElementById('vId').value = '';
            document.getElementById('modalTitle').textContent = 'Create New Voucher';
            this.toggleMaxDiscountGroup();
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

            const voucher = {
                id: document.getElementById('vId').value,
                code: document.getElementById('vCode').value,
                type: document.getElementById('vType').value,
                value: parseFloat(document.getElementById('vValue').value),
                maxDiscount: parseFloat(document.getElementById('vMaxDiscount').value || 0),
                minOrder: parseFloat(document.getElementById('vMinOrder').value),
                minPlan: document.getElementById('vMinPlan').value,
                minScore: parseInt(document.getElementById('vMinScore').value || 0),
                minBadges: parseInt(document.getElementById('vMinBadges').value || 0),
                budget: parseInt(document.getElementById('vBudget').value),
                scope: document.getElementById('vScope').value,
                startDate: document.getElementById('vStartDate').value,
                endDate: document.getElementById('vEndDate').value,
                stackable: document.getElementById('vStackable').checked,
                revocable: document.getElementById('vRevocable').checked,
                status: document.getElementById('vStatus').value
            };

            handler(voucher);
        });
    }

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

    renderVouchers(vouchers) {
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

            card.innerHTML = `
                <div class="voucher-status ${statusClass}">${v.status}</div>
                <div class="voucher-code">${v.code}</div>
                <div class="voucher-type">${v.type} - ${displayValue}</div>
                
                <div style="font-size: 13px; color: #4b5563; margin-bottom: 8px;">
                    <div><i class="ph ph-shopping-cart"></i> Min Order: ${formatCurrency(v.minOrder)}</div>
                    <div><i class="ph ph-users"></i> Min Plan: ${v.minPlan} | Score: ${v.minScore}</div>
                    <div><i class="ph ph-calendar"></i> ${new Date(v.startDate).toLocaleDateString()} - ${new Date(v.endDate).toLocaleDateString()}</div>
                </div>

                <div class="voucher-stats">
                    <div class="stat-item">
                        <span class="stat-label">Usage Rate</span>
                        <span class="stat-value">${usageRate}% (${v.used}/${v.budget})</span>
                    </div>
                    <div class="stat-item" style="text-align: right;">
                        <span class="stat-label">Est. ROI</span>
                        <span class="stat-value text-green-600">${v.roi}</span>
                    </div>
                </div>

                <div class="voucher-actions">
                    <button class="btn-action btn-edit" data-id="${v.id}"><i class="ph ph-pencil"></i> Edit</button>
                    <button class="btn-action btn-delete" data-id="${v.id}" style="color: #ef4444;"><i class="ph ph-trash"></i> Delete</button>
                </div>
            `;
            this.grid.appendChild(card);
        });
    }

    openEditModal(voucher) {
        document.getElementById('modalTitle').textContent = 'Edit Voucher';
        document.getElementById('vId').value = voucher.id;
        document.getElementById('vCode').value = voucher.code;
        document.getElementById('vType').value = voucher.type;
        document.getElementById('vValue').value = voucher.value;
        document.getElementById('vMaxDiscount').value = voucher.maxDiscount;
        document.getElementById('vMinOrder').value = voucher.minOrder;
        document.getElementById('vMinPlan').value = voucher.minPlan;
        document.getElementById('vMinScore').value = voucher.minScore;
        document.getElementById('vMinBadges').value = voucher.minBadges;
        document.getElementById('vBudget').value = voucher.budget;
        document.getElementById('vScope').value = voucher.scope;
        document.getElementById('vStartDate').value = voucher.startDate;
        document.getElementById('vEndDate').value = voucher.endDate;
        document.getElementById('vStackable').checked = voucher.stackable;
        document.getElementById('vRevocable').checked = voucher.revocable;
        document.getElementById('vStatus').value = voucher.status;
        
        this.toggleMaxDiscountGroup();
        this.modal.style.display = 'flex';
    }

    closeModal() {
        this.modal.style.display = 'none';
        this.form.reset();
    }
}
