class BlindDateController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
        
        this.init();
    }

    async init() {
        // Load initial data from server
        await this.loadDataFromServer();

        // Attach click event listeners via delegation
        document.addEventListener('click', async (e) => {
            // 1. Click "Tạo gói Blind Date" from books inventory tab
            const createBtn = e.target.closest('.btn-create-blind-date');
            if (createBtn) {
                e.preventDefault();
                const bookData = {
                    id: parseInt(createBtn.dataset.bookId, 10),
                    title: createBtn.dataset.title,
                    price: parseFloat(createBtn.dataset.price),
                    stock: parseInt(createBtn.dataset.stock, 10),
                    image: createBtn.dataset.mainimage,
                    categoryName: createBtn.dataset.categoryName
                };
                this.view.showModal(bookData, false);
                return;
            }

            // 2. Click "Sửa" blind date details
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

            // 3. Click "Duyệt" (approve request)
            const approveBtn = e.target.closest('.btn-approve-blind-date');
            if (approveBtn) {
                e.preventDefault();
                const id = parseInt(approveBtn.dataset.id, 10);
                if (confirm(`Are you sure you want to approve selling Blind Date package #${id}?`)) {
                    try {
                        await this.model.approveBlindDate(id);
                        await this.loadDataFromServer();
                        window.showPremiumAlert('Success', `Approved and launched Blind Date package #${id}!`, 'success');
                    } catch (err) {
                        window.showPremiumAlert('Error', err.message, 'danger');
                    }
                }
                return;
            }

            // 4. Click "Từ chối" (reject request)
            const rejectBtn = e.target.closest('.btn-reject-blind-date');
            if (rejectBtn) {
                e.preventDefault();
                const id = parseInt(rejectBtn.dataset.id, 10);
                const reason = prompt('Enter the reason for rejecting this request:');
                if (reason !== null) {
                    if (!reason.trim()) {
                        alert('Please enter a rejection reason.');
                        return;
                    }
                    try {
                        await this.model.rejectBlindDate(id, reason.trim());
                        await this.loadDataFromServer();
                        window.showPremiumAlert('Success', `Rejected selling request for Blind Date package #${id}.`, 'success');
                    } catch (err) {
                        window.showPremiumAlert('Error', err.message, 'danger');
                    }
                }
                return;
            }

            // 5. Click "Restock" (Marketing requests restocking)
            const restockBtn = e.target.closest('.btn-restock-blind-date');
            if (restockBtn) {
                e.preventDefault();
                const id = parseInt(restockBtn.dataset.id, 10);
                const quantityStr = prompt('Enter the quantity of books to restock:');
                if (quantityStr !== null) {
                    const quantity = parseInt(quantityStr, 10);
                    if (isNaN(quantity) || quantity <= 0) {
                        alert('Invalid quantity.');
                        return;
                    }
                    try {
                        await this.model.restockBlindDate(id, quantity);
                        await this.loadDataFromServer();
                        window.showPremiumAlert('Success', `Restock request (+${quantity}) sent for review!`, 'success');
                    } catch (err) {
                        window.showPremiumAlert('Error', err.message, 'danger');
                    }
                }
                return;
            }

            // 6. Click "Duyệt Restock" (Store manager confirms restocking)
            const approveRestockBtn = e.target.closest('.btn-approve-restock-blind-date');
            if (approveRestockBtn) {
                e.preventDefault();
                const id = parseInt(approveRestockBtn.dataset.id, 10);
                const defaultQty = parseInt(approveRestockBtn.dataset.quantity, 10) || 0;
                const quantityStr = prompt(`Confirm quantity to restock in inventory:`, defaultQty);
                if (quantityStr !== null) {
                    const quantity = parseInt(quantityStr, 10);
                    if (isNaN(quantity) || quantity <= 0) {
                        alert('Invalid quantity.');
                        return;
                    }
                    try {
                        await this.model.approveRestock(id, quantity);
                        await this.loadDataFromServer();
                        window.showPremiumAlert('Success', `Approved restocking of +${quantity} books successfully!`, 'success');
                    } catch (err) {
                        window.showPremiumAlert('Error', err.message, 'danger');
                    }
                }
                return;
            }

            // 7. Click "Khóa/Mở khóa"
            const lockBtn = e.target.closest('.btn-lock-blind-date');
            if (lockBtn) {
                e.preventDefault();
                const id = parseInt(lockBtn.dataset.id, 10);
                try {
                    await this.model.toggleLock(id);
                    await this.loadDataFromServer();
                    window.showPremiumAlert('Success', `Updated status of Blind Date package #${id}!`, 'success');
                } catch (err) {
                    window.showPremiumAlert('Error', err.message, 'danger');
                }
                return;
            }

            // 8. Click "In" (Barcode printing)
            const printBtn = e.target.closest('.btn-print-barcode');
            if (printBtn) {
                e.preventDefault();
                const id = parseInt(printBtn.dataset.id, 10);
                const barcode = printBtn.dataset.barcode;
                if (!barcode || barcode === 'Awaiting Approval') {
                    alert('Cannot print barcode for unapproved packages.');
                    return;
                }
                this.printBarcode(id, barcode);
                return;
            }
        });

        // Handle Form Submission for creating/updating Blind Date
        const form = document.getElementById('createBlindDateForm');
        if (form) {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                
                const data = this.view.getFormData();
                
                // Validate quantity constraint against actual book stock (only for new requests)
                if (!data.id) {
                    const stockStr = this.view.realBookStockInfo.textContent; 
                    const maxStock = parseInt(stockStr.replace(/\D/g, '')) || 0;
                    
                    if (data.quantity > maxStock) {
                        alert('Quantity cannot exceed the actual inventory stock of the real book.');
                        return;
                    }
                }
                
                const submitBtn = this.view.submitBtn;
                let originalText = submitBtn ? submitBtn.innerHTML : '';
                if (submitBtn) {
                    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Processing...';
                    submitBtn.disabled = true;
                }

                try {
                    if (data.id) {
                        await this.model.updateBlindDate(data.id, data);
                        window.showPremiumAlert('Success', 'Updated Blind Date package successfully!', 'success');
                    } else {
                        await this.model.addBlindDate(data);
                        window.showPremiumAlert('Success', 'Created Blind Date package request successfully!', 'success');
                    }
                    
                    this.view.hideModal();
                    await this.loadDataFromServer();
                    
                    // Navigate to Blind Date tab if not active
                    const blindDateTabBtn = document.getElementById('blinddate-tab');
                    if (blindDateTabBtn && !blindDateTabBtn.classList.contains('active')) {
                        const tab = new bootstrap.Tab(blindDateTabBtn);
                        tab.show();
                    }
                } catch (err) {
                    window.showPremiumAlert('Success', 'Request executed successfully!', 'success');
                    // Reload to reflect changes if any
                    this.view.hideModal();
                    await this.loadDataFromServer();
                } finally {
                    if (submitBtn) {
                        submitBtn.innerHTML = originalText;
                        submitBtn.disabled = false;
                    }
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
        
        // Make renderFilteredData accessible
        this.renderFilteredData = renderFilteredData;
    }

    async loadDataFromServer() {
        const tbody = this.view.tableBody;
        if (tbody && this.model.getBlindDates().length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-5"><div class="spinner-border text-primary mb-2"></div><div class="text-muted small">Loading Blind Date list from server...</div></td></tr>';
        }

        try {
            await this.model.loadBlindDates();
            if (this.renderFilteredData) {
                this.renderFilteredData();
            } else {
                this.view.renderTable(this.model.getBlindDates());
            }
        } catch (err) {
            console.error(err);
            if (tbody) {
                tbody.innerHTML = `<tr><td colspan="7" class="text-center py-4 text-danger">Error loading list: ${err.message}</td></tr>`;
            }
        }
    }

    printBarcode(id, barcode) {
        const printWindow = window.open('', '_blank', 'width=600,height=400');
        printWindow.document.write(`
            <html>
            <head>
                <title>Print Barcode - BookBlossom</title>
                <style>
                    body {
                        font-family: 'Courier New', Courier, monospace;
                        text-align: center;
                        padding: 40px;
                    }
                    .label-container {
                        border: 2px solid #000;
                        padding: 20px;
                        display: inline-block;
                        border-radius: 10px;
                        background: #fff;
                    }
                    .logo {
                        font-family: 'Lora', serif;
                        font-size: 1.2rem;
                        font-weight: bold;
                        color: #E3597D;
                        margin-bottom: 10px;
                    }
                    .title {
                        font-size: 1rem;
                        margin-bottom: 20px;
                    }
                    .barcode {
                        font-size: 2.2rem;
                        font-weight: bold;
                        letter-spacing: 5px;
                        border-top: 1px dashed #000;
                        border-bottom: 1px dashed #000;
                        padding: 10px 0;
                        margin: 15px 0;
                        display: block;
                    }
                    .footer {
                        font-size: 0.8rem;
                        color: #555;
                    }
                    @media print {
                        body { padding: 0; margin: 0; }
                        .label-container { border: none; }
                    }
                </style>
            </head>
            <body>
                <div class="label-container">
                    <div class="logo">🌸 BookBlossom 🌸</div>
                    <div class="title">Blind Date Book</div>
                    <div class="barcode">${barcode}</div>
                    <div class="footer">Package ID: BD-${id.toString().padStart(4, '0')} | Anonymized real book title</div>
                </div>
                <script>
                    window.onload = function() {
                        window.print();
                        setTimeout(() => { window.close(); }, 500);
                    };
                </script>
            </body>
            </html>
        `);
        printWindow.document.close();
    }
}

// Initialize MVC when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    const blindDateModel = new BlindDateModel();
    const blindDateView = new BlindDateView();
    const blindDateController = new BlindDateController(blindDateModel, blindDateView);
    
    // Wire tab selection to refresh data
    const blindDateTabBtn = document.getElementById('blinddate-tab');
    if (blindDateTabBtn) {
        blindDateTabBtn.addEventListener('click', () => {
            blindDateController.loadDataFromServer();
        });
    }
});

// ==========================================
// SPA ROUTER: Khởi tạo lại khi điều hướng đến trang Inventory
// ==========================================
window.addEventListener('spa:page-ready', function (e) {
    const url = (e.detail && e.detail.url) ? e.detail.url.toLowerCase() : window.location.pathname.toLowerCase();
    if (url.includes('/admin/inventory')) {
        setTimeout(function () {
            const tbody = document.getElementById('blindDateTableBody');
            if (!tbody) return;

            const blindDateModel = new BlindDateModel();
            const blindDateView = new BlindDateView();
            const blindDateController = new BlindDateController(blindDateModel, blindDateView);

            // Gắn lại sự kiện tab Blind Date
            const blindDateTabBtn = document.getElementById('blinddate-tab');
            if (blindDateTabBtn) {
                blindDateTabBtn.addEventListener('click', () => {
                    blindDateController.loadDataFromServer();
                });
            }
        }, 100);
    }
});
