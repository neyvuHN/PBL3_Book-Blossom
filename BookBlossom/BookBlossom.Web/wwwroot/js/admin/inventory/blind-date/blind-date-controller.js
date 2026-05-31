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
                    image: createBtn.dataset.mainimage
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
                this.view.renderTable(this.model.getBlindDates());
                
                // Automatically navigate to Blind Date tab if not already there
                const blindDateTabBtn = document.getElementById('blinddate-tab');
                if (blindDateTabBtn && !blindDateTabBtn.classList.contains('active')) {
                    const tab = new bootstrap.Tab(blindDateTabBtn);
                    tab.show();
                }
            });
        }
        
        // Initial render for the blind date table
        this.view.renderTable(this.model.getBlindDates());
    }
}

// Bootstrap the MVC components when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    const blindDateModel = new BlindDateModel();
    const blindDateView = new BlindDateView();
    const blindDateController = new BlindDateController(blindDateModel, blindDateView);
});
