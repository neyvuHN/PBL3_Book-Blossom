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
        this.inputQuantity = document.getElementById('bdQuantity');
        this.inputPrice = document.getElementById('bdPrice');
        
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
            
            // Check if file is a File object or an existing URL string (for edit mode mock)
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
        
        // Reset images
        this.selectedImages = isEditMode && bookData.images ? [...bookData.images] : [];
        this.renderImagePreviews();
        
        // Fill real book info
        this.inputRealBookId.value = bookData.id || bookData.realBookId;
        this.realBookImg.src = bookData.image || bookData.realBookImage || '/images/Book/book1.jpg';
        this.realBookTitle.textContent = bookData.title || bookData.realBookTitle;
        
        if (bookData.stockInfo) {
            this.realBookStockInfo.textContent = bookData.stockInfo;
        } else {
            this.realBookStockInfo.textContent = `Current stock: ${bookData.stock}`;
        }
        
        this.realBookPrice.textContent = `Current price: ${parseFloat(bookData.realBookPrice || bookData.price).toLocaleString()} ₫`;
        
        // Set validations and defaults
        this.inputQuantity.max = bookData.stock || 999;
        this.inputQuantity.value = isEditMode ? bookData.quantity : 1;
        this.inputPrice.value = isEditMode ? bookData.price : (bookData.price || 0);
        
        if (isEditMode) {
            this.form.elements['Keywords'].value = bookData.keywords || '';
            this.form.elements['Quotes'].value = bookData.quotes || '';
            this.form.elements['Hashtags'].value = bookData.hashtags || '';
            
            if (this.modalTitle) this.modalTitle.textContent = "Edit Blind Date Package";
            if (this.submitBtn) this.submitBtn.textContent = "Save Changes";
            this.form.dataset.editId = bookData.id;
        } else {
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
            realBookId: formData.get('RealBookID'),
            realBookTitle: this.realBookTitle.textContent,
            realBookImage: this.realBookImg.src,
            realBookPrice: parseFloat(this.realBookPrice.textContent.replace(/\D/g, '')),
            stockInfo: this.realBookStockInfo.textContent,
            quantity: parseInt(formData.get('Quantity'), 10),
            price: parseFloat(formData.get('Price')),
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
            this.tableBody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">No blind date packages created yet.</td></tr>';
            return;
        }

        dataList.forEach(item => {
            const tr = document.createElement('tr');
            
            // Check if there are user uploaded images, use the first one, else use default icon
            let packageImageHtml = `
                <div class="d-flex align-items-center justify-content-center rounded inventory-icon-box" style="width: 48px; height: 60px; background-color: #fce7f3; border: 1px dashed #f472b6;">
                    <i class="bi bi-box2-heart fs-4 inventory-icon" style="color: #e83e8c;"></i>
                </div>
            `;
            
            if (item.images && item.images.length > 0) {
                const firstImg = item.images[0];
                let src = '';
                if (firstImg instanceof File) {
                    src = URL.createObjectURL(firstImg);
                } else if (typeof firstImg === 'string') {
                    src = firstImg;
                } else if (firstImg.url) {
                    src = firstImg.url;
                }
                packageImageHtml = `<img src="${src}" class="rounded shadow-sm" style="width: 48px; height: 60px; object-fit: cover;" />`;
            }

            tr.innerHTML = `
                <td>
                    ${packageImageHtml}
                </td>
                <td>
                    <div class="fw-bold inventory-title" style="color: #2C2630;">Mystery Book #${item.id}</div>
                    <div class="text-muted small inventory-id">ID: BD-${item.id.toString().padStart(4, '0')}</div>
                </td>
                <td>
                    <div class="d-flex align-items-center">
                        <img src="${item.realBookImage}" class="rounded shadow-sm me-2" style="width: 32px; height: 40px; object-fit: cover;" onerror="this.onerror=null; this.src='/images/Book/book1.jpg';" />
                        <span class="small text-truncate" style="max-width: 150px;" title="${item.realBookTitle}">${item.realBookTitle}</span>
                    </div>
                </td>
                <td class="fw-bold" style="color: #E3597D;">${item.price.toLocaleString()} ₫</td>
                <td><span class="badge bg-light text-dark border">${item.quantity} available</span></td>
                <td><span class="badge bg-secondary font-monospace bg-opacity-10 text-secondary border border-secondary">${item.barcode}</span></td>
                <td>
                    <button class="btn-action-edit me-1 btn-edit-blind-date" data-id="${item.id}">
                        <i class="bi bi-pencil-square me-1"></i> Edit
                    </button>
                    <button class="btn-action-edit text-info border-info" style="background-color: rgba(13, 202, 240, 0.1);" onclick="window.showPremiumAlert('Success', 'Printing barcode: ${item.barcode}', 'success')">
                        <i class="bi bi-printer me-1"></i> Print
                    </button>
                </td>
            `;
            this.tableBody.appendChild(tr);
        });
    }
}
