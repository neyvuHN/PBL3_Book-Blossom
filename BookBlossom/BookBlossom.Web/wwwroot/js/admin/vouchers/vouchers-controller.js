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

        // Initial render
        this.init();
    }

    async init() {
        await this.model.init();

        // Bind Selection Modals
        this.view.bindSelectCategories(() => {
            this.view.openSelectionModal('Categories', this.model.mockCategories);
        });
        this.view.bindSelectBooks(() => {
            this.view.openSelectionModal('Books', this.model.mockBooks);
        });

        await this.refreshGrid(true); // Force fetch on startup
    }

    async refreshGrid(forceFetch = false) {
        let vouchers = await this.model.getAllVouchers(forceFetch);
        const filters = this.view.getFilterValues();

        // Apply Status Filter
        if (filters.status !== 'All') {
            vouchers = vouchers.filter(v => v.status === filters.status);
        }

        // Apply Search Filter
        if (filters.searchTerm) {
            vouchers = vouchers.filter(v => v.code.toLowerCase().includes(filters.searchTerm));
        }

        this.view.renderVouchers(vouchers, this.model.mockCategories, this.model.mockBooks);
    }

    async handleSaveVoucher(voucher) {
        try {
            if (voucher.id) {
                await this.model.updateVoucher(voucher);
            } else {
                await this.model.addVoucher(voucher);
            }
            this.view.closeModal();
            await this.refreshGrid(true); // Force fetch after modification
        } catch (e) {
            console.error("Failed to save voucher", e);
        }
    }

    async handleEditVoucher(id) {
        const voucher = await this.model.getVoucherById(id);
        if (voucher) {
            this.view.openEditModal(voucher);
        }
    }

    async handleDeleteVoucher(id) {
        if (await this.model.deleteVoucher(id)) {
            await this.refreshGrid(true); // Force fetch after deletion
        }
    }

    async handleFilters() {
        await this.refreshGrid(false); // Local caching when filtering
    }
}
