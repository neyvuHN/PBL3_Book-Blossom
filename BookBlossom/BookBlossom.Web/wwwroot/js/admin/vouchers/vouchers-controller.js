class VouchersController {
    constructor(model, view) {
        this.model = model;
        this.view = view;

        // Bind view events to controller handlers
        this.view.bindCreateVoucher();
        this.view.bindCloseModal();
        this.view.bindSaveVoucher(this.handleSaveVoucher.bind(this));
        this.view.bindEditVoucher(this.handleEditVoucher.bind(this));
        this.view.bindDeleteVoucher(this.handleDeleteVoucher.bind(this));
        this.view.bindFilters(this.handleFilters.bind(this));

        // Bind Selection Modals
        this.view.bindSelectCategories(() => {
            this.view.openSelectionModal('Categories', this.model.mockCategories);
        });
        this.view.bindSelectBooks(() => {
            this.view.openSelectionModal('Books', this.model.mockBooks);
        });

        // Initial render
        this.refreshGrid();
    }

    refreshGrid() {
        let vouchers = this.model.getAllVouchers();
        const filters = this.view.getFilterValues();

        // Apply Status Filter
        if (filters.status !== 'All') {
            vouchers = vouchers.filter(v => v.status === filters.status);
        }

        // Apply Search Filter
        if (filters.searchTerm) {
            vouchers = vouchers.filter(v => v.code.toLowerCase().includes(filters.searchTerm));
        }

        this.view.renderVouchers(vouchers);
    }

    handleSaveVoucher(voucher) {
        if (voucher.id) {
            this.model.updateVoucher(voucher);
        } else {
            this.model.addVoucher(voucher);
        }
        this.view.closeModal();
        this.refreshGrid();
    }

    handleEditVoucher(id) {
        const voucher = this.model.getVoucherById(id);
        if (voucher) {
            this.view.openEditModal(voucher);
        }
    }

    handleDeleteVoucher(id) {
        if (this.model.deleteVoucher(id)) {
            this.refreshGrid();
        }
    }

    handleFilters() {
        this.refreshGrid();
    }
}
