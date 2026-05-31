class BlindDateController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
        
        this.init();
    }

    init() {
        // Attach click event for "Blind Date" buttons on the main inventory table
        // We use document delegation since the rows might be dynamically filtered or added
        document.addEventListener('click', (e) => {
            const createBtn = e.target.closest('.btn-create-blind-date');
            if (createBtn) {
                e.preventDefault();
                const bookData = {
                    id: createBtn.dataset.bookId, // realBookId
                    title: createBtn.dataset.title,
                    price: createBtn.dataset.price,
                    stock: createBtn.dataset.stock,
                    image: createBtn.dataset.mainimage,
                    categoryName: createBtn.dataset.categoryName
                };
                this.view.showModal(bookData, false);
                return;
            }

            const editBtn = e.target.closest('.btn-edit-blind-date');
            if (editBtn) {
                e.preventDefault();
                const id = parseInt(editBtn.dataset.id, 10);
                const packageData = this.model.getBlindDate(id);
                if (packageData) {
                    this.view.showModal(packageData, true);
                }
                return;
            }

            const deleteBtn = e.target.closest('.btn-delete-blind-date');
            if (deleteBtn) {
                e.preventDefault();
                const id = parseInt(deleteBtn.dataset.id, 10);
                const name = deleteBtn.dataset.name;
                
                // Use global delete modal if exists
                const deleteModalEl = document.getElementById('deleteConfirmModal');
                if (deleteModalEl && window.bootstrap) {
                    document.getElementById('deleteConfirmTitle').textContent = 'Delete Blind Date';
                    document.getElementById('deleteConfirmMessage').textContent = `Are you sure you want to delete ${name}?`;
                    const confirmBtn = document.getElementById('deleteConfirmBtn');
                    
                    // Remove old event listeners by replacing the button
                    const newConfirmBtn = confirmBtn.cloneNode(true);
                    confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);
                    
                    newConfirmBtn.addEventListener('click', (ev) => {
                        ev.preventDefault();
                        this.model.deleteBlindDate(id);
                        this.view.renderTable(this.model.getBlindDates());
                        bootstrap.Modal.getInstance(deleteModalEl)?.hide();
                        if (window.showPremiumAlert) {
                            window.showPremiumAlert('Deleted', `${name} has been removed.`, 'success');
                        }
                    });
                    
                    const bsModal = bootstrap.Modal.getOrCreateInstance(deleteModalEl);
                    bsModal.show();
                } else if (confirm(`Are you sure you want to delete ${name}?`)) {
                    this.model.deleteBlindDate(id);
                    this.view.renderTable(this.model.getBlindDates());
                    if (window.showPremiumAlert) {
                        window.showPremiumAlert('Deleted', `${name} has been removed.`, 'success');
                    }
                }
                return;
            }
        });

        // Handle Form Submission for approving, creating, or editing Blind Date
        const form = document.getElementById('createBlindDateForm');
        if (form) {
            form.addEventListener('submit', (e) => {
                e.preventDefault();
                
                const data = this.view.getFormData();
                
                // Validate stock limit
                const stockStr = this.view.realBookStockInfo.textContent; 
                const maxStock = parseInt(stockStr.replace(/\D/g, '')) || 0;
                
                if (data.quantity > maxStock) {
                    alert('Quantity cannot exceed the actual inventory stock of the real book.');
                    return;
                }
                
                if (data.id) {
                    // Update existing
                    this.model.updateBlindDate(data.id, data);
                } else {
                    // Add new
                    this.model.addBlindDate(data);
                }
                
                // Update view
                this.view.hideModal();
                
                // Refresh data with current filters
                if (this.view.searchInput) {
                    this.view.searchInput.dispatchEvent(new Event('input'));
                } else {
                    this.view.renderTable(this.model.getBlindDates());
                }
                
                // Automatically navigate to Blind Date tab if not already there
                const blindDateTabBtn = document.getElementById('blinddate-tab');
                if (blindDateTabBtn && !blindDateTabBtn.classList.contains('active')) {
                    const tab = new bootstrap.Tab(blindDateTabBtn);
                    tab.show();
                }
            });
        }
        
        // Handle Search and Filter
        const renderFilteredData = () => {
            const searchTerm = (this.view.searchInput?.value || '').toLowerCase();
            const categoryFilter = this.view.categoryFilter?.value || '';

            const filteredData = this.model.getBlindDates().filter(item => {
                const searchMatch = !searchTerm || 
                    (item.keywords || '').toLowerCase().includes(searchTerm) ||
                    (item.quotes || '').toLowerCase().includes(searchTerm) ||
                    (item.hashtags || '').toLowerCase().includes(searchTerm);
                
                const categoryMatch = !categoryFilter || item.realBookCategoryName === categoryFilter;

                return searchMatch && categoryMatch;
            });

            this.view.renderTable(filteredData);
        };

        if (this.view.searchInput) {
            this.view.searchInput.addEventListener('input', renderFilteredData);
        }

        if (this.view.categoryFilter) {
            this.view.categoryFilter.addEventListener('change', renderFilteredData);
        }
        
        // Initial render for the blind date table
        renderFilteredData();
    }
}

// Bootstrap the MVC components when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    const blindDateModel = new BlindDateModel();
    const blindDateView = new BlindDateView();
    const blindDateController = new BlindDateController(blindDateModel, blindDateView);
});
