window.showPremiumAlert = function(title, message, type = 'success') {
    const alertClass = type === 'success' ? 'alert-success-premium' : 'alert-danger-premium';
    const iconClass = type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-circle-fill';
    
    let container = document.getElementById('dynamic-alert-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'dynamic-alert-container';
        container.style.position = 'fixed';
        container.style.top = '20px';
        container.style.right = '20px';
        container.style.zIndex = '9999';
        container.style.minWidth = '300px';
        document.body.appendChild(container);
    }
    
    const alertEl = document.createElement('div');
    alertEl.className = 'custom-alert-container';
    
    const alertInner = document.createElement('div');
    alertInner.className = `custom-alert ${alertClass}`;
    alertInner.innerHTML = `
        <div class="alert-icon-box"><i class="bi ${iconClass}"></i></div>
        <div class="alert-content-box">
            <h5 class="alert-heading">${title}</h5>
            <p class="alert-message">${message}</p>
        </div>
        <button class="btn-close-alert" onclick="this.parentElement.parentElement.remove()" style="background: none; border: none;"><i class="bi bi-x"></i></button>
    `;
    
    alertEl.appendChild(alertInner);
    container.appendChild(alertEl);
    
    setTimeout(() => {
        if (document.body.contains(alertEl)) {
            alertEl.style.animation = "fadeOutUp 0.4s ease-out forwards";
            setTimeout(() => {
                if (document.body.contains(alertEl)) alertEl.remove();
            }, 400);
        }
    }, 4000);
};

class BlindDateView {
    constructor() {
        // Modal elements
        this.modalEl = document.getElementById('createBlindDateModal');
        this.modal = this.modalEl ? new bootstrap.Modal(this.modalEl) : null;
        this.form = document.getElementById('createBlindDateForm');
        this.modalTitle = document.getElementById('createBlindDateModalLabel');
        this.submitBtn = this.form ? this.form.querySelector('button[type="submit"]') : null;
        
        // Display elements for book info
        this.realBookImg = document.getElementById('bdRealBookImage');
        this.realBookTitle = document.getElementById('bdRealBookTitle');
        this.realBookPrice = document.getElementById('bdRealBookPrice');
        this.realBookStockInfo = document.getElementById('bdRealBookStockInfo');
        
        // Inputs
        this.inputRealBookId = document.getElementById('bdRealBookID');
        this.inputRealBookCategoryName = document.getElementById('bdRealBookCategoryName');
        this.inputQuantity = document.getElementById('bdQuantity');
        this.inputPrice = document.getElementById('bdPrice');
        
        // Search & Filter
        this.searchInput = document.getElementById('searchBlindDateInput');
        this.categoryFilter = document.getElementById('filterBlindDateCategory');
        
        // Table element
        this.tableBody = document.getElementById('blindDateTableBody');

        // Image upload logic
        this.selectedImages = [];
        this.imagesInput = document.getElementById('bdImagesInput');
        this.imagesPreviewContainer = document.getElementById('bdImagesPreviewContainer');
        
        this.initImageUploads();
    }

    initImageUploads() {
        if (this.imagesInput) {
            this.imagesInput.addEventListener('change', (e) => {
                for (let i = 0; i < e.target.files.length; i++) {
                    this.selectedImages.push(e.target.files[i]);
                }
                this.renderImagePreviews();
            });
        }
    }

    renderImagePreviews() {
        if (!this.imagesPreviewContainer) return;
        this.imagesPreviewContainer.innerHTML = "";
        
        this.selectedImages.forEach((file, index) => {
            const wrapper = document.createElement('div');
            wrapper.className = "image-preview-wrapper";
            
            const img = document.createElement('img');
            
            if (file instanceof File) {
                const reader = new FileReader();
                reader.onload = (e) => {
                    img.src = e.target.result;
                };
                reader.readAsDataURL(file);
            } else if (typeof file === 'string') {
                img.src = file;
            } else if (file.url) {
                img.src = file.url;
            }
            
            const removeBtn = document.createElement('button');
            removeBtn.type = "button";
            removeBtn.className = "remove-btn";
            removeBtn.innerHTML = "&times;";
            removeBtn.addEventListener('click', (event) => {
                event.stopPropagation();
                this.selectedImages.splice(index, 1);
                this.renderImagePreviews();
            });
            
            wrapper.appendChild(img);
            wrapper.appendChild(removeBtn);
            this.imagesPreviewContainer.appendChild(wrapper);
        });
        
        this.updateInputFiles();
    }

    updateInputFiles() {
        if (!this.imagesInput) return;
        const dt = new DataTransfer();
        this.selectedImages.forEach(file => {
            if (file instanceof File) {
                dt.items.add(file);
            }
        });
        this.imagesInput.files = dt.files;
    }

    showModal(bookData, isEditMode = false) {
        if (!this.modal || !this.form) return;
        
        this.form.reset();
        
        // Remove existing warnings
        const oldWarning = document.getElementById('bdPriceWarning');
        if (oldWarning) oldWarning.remove();
        
        // Reset images
        this.selectedImages = isEditMode && bookData.images ? [...bookData.images] : [];
        this.renderImagePreviews();
        
        // Fill real book info
        this.inputRealBookId.value = bookData.realBookId || bookData.id;
        if (this.inputRealBookCategoryName) {
            this.inputRealBookCategoryName.value = bookData.categoryName || bookData.realBookCategoryName || '';
        }
        this.realBookImg.src = bookData.image || bookData.realBookImage || '/images/Book/book1.jpg';
        this.realBookTitle.textContent = bookData.title || bookData.realBookTitle;
        
        if (bookData.stockInfo) {
            this.realBookStockInfo.textContent = bookData.stockInfo;
        } else {
            this.realBookStockInfo.textContent = `Current stock: ${bookData.stock}`;
        }
        
        const originalPrice = bookData.realBookPrice || bookData.price || 0;
        this.realBookPrice.textContent = `Current price: ${parseFloat(originalPrice).toLocaleString()} ₫`;
        
        // Set validations and defaults
        this.inputQuantity.max = bookData.stock || 999;
        this.inputQuantity.value = isEditMode ? bookData.quantity : 1;
        this.inputPrice.value = isEditMode ? bookData.price : (bookData.price || 0);
        
        if (isEditMode) {
            this.form.elements['Keywords'].value = bookData.keywords || '';
            this.form.elements['Quotes'].value = bookData.quotes || '';
            this.form.elements['Hashtags'].value = bookData.hashtags || '';
            
            // Disable quantity in edit mode (stocks must be updated via Restock workflow)
            this.inputQuantity.disabled = true;
            this.inputQuantity.removeAttribute('required');
            
            // Handle Price Lock if book has orders
            if (bookData.hasOrders) {
                this.inputPrice.disabled = true;
                this.inputPrice.removeAttribute('required');
                const warningDiv = document.createElement('div');
                warningDiv.id = 'bdPriceWarning';
                warningDiv.className = 'text-danger small mt-1';
                warningDiv.innerHTML = '<i class="bi bi-lock-fill"></i> Price cannot be modified because orders have been placed for this package.';
                this.inputPrice.parentNode.appendChild(warningDiv);
            } else {
                this.inputPrice.disabled = false;
                this.inputPrice.setAttribute('required', 'required');
            }
            
            if (this.modalTitle) this.modalTitle.textContent = "Edit Blind Date Package";
            if (this.submitBtn) this.submitBtn.textContent = "Save Changes";
            this.form.dataset.editId = bookData.id;
        } else {
            this.inputQuantity.disabled = false;
            this.inputQuantity.setAttribute('required', 'required');
            this.inputPrice.disabled = false;
            this.inputPrice.setAttribute('required', 'required');
            
            if (this.modalTitle) this.modalTitle.textContent = "Create Blind Date Package";
            if (this.submitBtn) this.submitBtn.textContent = "Approve & Create";
            delete this.form.dataset.editId;
        }
        
        this.modal.show();
    }

    hideModal() {
        if (this.modal) {
            this.modal.hide();
        }
    }

    getFormData() {
        const formData = new FormData(this.form);
        const editId = this.form.dataset.editId ? parseInt(this.form.dataset.editId, 10) : null;
        
        return {
            id: editId,
            realBookId: formData.get('RealBookID') || this.inputRealBookId.value,
            realBookCategoryName: formData.get('RealBookCategoryName') || this.inputRealBookCategoryName.value,
            realBookTitle: this.realBookTitle.textContent,
            realBookImage: this.realBookImg.src,
            realBookPrice: parseFloat(this.realBookPrice.textContent.replace(/\D/g, '')),
            stockInfo: this.realBookStockInfo.textContent,
            quantity: this.inputQuantity.disabled ? 0 : parseInt(formData.get('Quantity') || this.inputQuantity.value, 10),
            price: parseFloat(formData.get('Price') || this.inputPrice.value),
            keywords: formData.get('Keywords'),
            quotes: formData.get('Quotes'),
            hashtags: formData.get('Hashtags'),
            images: this.selectedImages
        };
    }

    renderTable(dataList) {
        if (!this.tableBody) return;
        
        this.tableBody.innerHTML = '';
        if (dataList.length === 0) {
            this.tableBody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">No Blind Date packages created yet.</td></tr>';
            return;
        }

        dataList.forEach(item => {
            const tr = document.createElement('tr');
            
            let packageImageHtml = `
                <div class="d-flex align-items-center justify-content-center rounded inventory-icon-box" style="width: 48px; height: 60px; background-color: #fce7f3; border: 1px dashed #f472b6;">
                    <i class="bi bi-box2-heart fs-4 inventory-icon" style="color: #e83e8c;"></i>
                </div>
            `;
            
            // Render Status badge
            let statusBadge = '';
            let actionButtons = '';
            let barcodeHtml = '';
            let qtyHtml = '';

            if (item.status === 0) { // Pending
                statusBadge = '<span class="badge bg-warning text-dark border border-warning bg-opacity-10 mb-1">Pending</span>';
                qtyHtml = `<span class="badge bg-light text-warning border border-warning bg-opacity-10">${item.requestQuantity} requested</span>`;
                barcodeHtml = '<span class="text-muted small font-monospace">Pending</span>';
                
                actionButtons = `
                    <button class="btn btn-sm btn-outline-success me-1 btn-approve-blind-date" data-id="${item.id}" title="Approve selling">
                        <i class="bi bi-check-circle"></i> Approve
                    </button>
                    <button class="btn btn-sm btn-outline-danger me-1 btn-reject-blind-date" data-id="${item.id}" title="Reject request">
                        <i class="bi bi-x-circle"></i> Reject
                    </button>
                `;
            } else if (item.status === 1) { // Approved
                let lockStatusBadge = item.isLocked 
                    ? '<span class="badge bg-danger border border-danger bg-opacity-10 mb-1 text-danger ms-1">Locked (Hidden)</span>' 
                    : '<span class="badge bg-success border border-success bg-opacity-10 mb-1 text-success ms-1">Active</span>';
                
                statusBadge = '<span class="badge bg-success border border-success bg-opacity-10 mb-1 text-success">Approved</span>' + lockStatusBadge;
                qtyHtml = `<span class="badge bg-light text-dark border">${item.stockQuantity} available</span>`;
                
                if (item.requestQuantity > 0) {
                    qtyHtml += `<br/><span class="badge bg-light text-warning border border-warning bg-opacity-10 mt-1">Restock request: +${item.requestQuantity}</span>`;
                }

                barcodeHtml = `<span class="badge bg-secondary font-monospace bg-opacity-10 text-secondary border border-secondary">${item.barcode}</span>`;
                
                let restockAction = '';
                if (item.requestQuantity > 0) {
                    restockAction = `
                        <button class="btn btn-sm btn-outline-warning me-1 btn-approve-restock-blind-date" data-id="${item.id}" data-quantity="${item.requestQuantity}" title="Approve restock request">
                            <i class="bi bi-check2-circle"></i> Approve Restock
                        </button>
                    `;
                } else {
                    restockAction = `
                        <button class="btn btn-sm btn-outline-info me-1 btn-restock-blind-date" data-id="${item.id}" title="Send restock request">
                            <i class="bi bi-plus-circle"></i> Restock
                        </button>
                    `;
                }

                actionButtons = `
                    <button class="btn btn-sm btn-outline-primary me-1 btn-edit-blind-date" data-id="${item.id}" title="Edit details">
                        <i class="bi bi-pencil-square"></i> Edit
                    </button>
                    ${restockAction}
                    <button class="btn btn-sm btn-outline-secondary me-1 btn-print-barcode" data-id="${item.id}" data-barcode="${item.barcode}" title="Print barcode">
                        <i class="bi bi-printer"></i> Print
                    </button>
                    <button class="btn btn-sm ${item.isLocked ? 'btn-outline-warning' : 'btn-outline-danger'} me-1 btn-lock-blind-date" data-id="${item.id}" title="${item.isLocked ? 'Unlock selling' : 'Lock selling'}">
                        <i class="bi ${item.isLocked ? 'bi-unlock' : 'bi-lock'}"></i> ${item.isLocked ? 'Unlock' : 'Lock'}
                    </button>
                `;
            } else { // Rejected
                statusBadge = `<span class="badge bg-danger border border-danger bg-opacity-10 mb-1 text-danger" title="${item.rejectReason || 'No reason specified'}">Rejected</span>`;
                qtyHtml = `<span class="badge bg-light text-muted border">0</span>`;
                barcodeHtml = '<span class="text-danger small">Disapproved</span>';
                actionButtons = `<span class="text-muted small" title="${item.rejectReason || ''}">Reason: ${item.rejectReason || 'N/A'}</span>`;
            }

            tr.innerHTML = `
                <td>
                    ${packageImageHtml}
                </td>
                <td>
                    <div class="fw-bold inventory-title" style="color: #2C2630;">Mystery Book #${item.id}</div>
                    <div class="text-muted small inventory-id">ID: BD-${item.id.toString().padStart(4, '0')}</div>
                    <div class="mt-1">${statusBadge}</div>
                </td>
                <td>
                    <div class="d-flex align-items-center">
                        <img src="${item.realBookImage}" class="rounded shadow-sm me-2" style="width: 32px; height: 40px; object-fit: cover;" onerror="this.onerror=null; this.src='/images/Book/book1.jpg';" />
                        <div>
                            <span class="small text-truncate d-block" style="max-width: 150px;" title="${item.realBookTitle}">${item.realBookTitle}</span>
                            <span class="text-muted small font-monospace">Real ID: ${item.realBookId}</span>
                        </div>
                    </div>
                </td>
                <td class="fw-bold" style="color: #E3597D;">${item.price.toLocaleString()} ₫</td>
                <td>${qtyHtml}</td>
                <td>${barcodeHtml}</td>
                <td class="text-nowrap">
                    ${actionButtons}
                </td>
            `;
            this.tableBody.appendChild(tr);
        });
    }
}
