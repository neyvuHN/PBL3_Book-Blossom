class BookView {
    constructor() {
        this.tbody = document.getElementById('booksTableBody');
        this.addBookForm = document.getElementById('addBookForm');
        this.addBookModalElement = document.getElementById('addBookModal');
        this.addBookModalLabel = document.getElementById('addBookModalLabel');
        this.bookImagesInput = document.getElementById('bookImagesInput');
        this.bookImagesPreviewContainer = document.getElementById('bookImagesPreviewContainer');
        this.currentBookCoverContainer = document.getElementById('currentBookCoverContainer');
        this.currentBookCoverImg = document.getElementById('currentBookCoverImg');
        this.currentSampleFileContainer = document.getElementById('currentSampleFileContainer');
        this.sampleFileInput = document.getElementById('sampleFileInput');
        
        if (this.addBookModalElement) {
            this.bookModal = new bootstrap.Modal(this.addBookModalElement);
        }
        
        this.selectedBookImages = [];
    }

    renderLoading() {
        if (!this.tbody) return;
        this.tbody.innerHTML = '<tr><td colspan="8" class="text-center py-5"><div class="spinner-border text-primary mb-2"></div><div class="text-muted small">Loading book inventory data from API...</div></td></tr>';
    }

    renderEmpty() {
        if (!this.tbody) return;
        this.tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No books found matching the filter criteria.</td></tr>';
    }

    renderError() {
        if (!this.tbody) return;
        this.tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-danger">An error occurred while loading book data.</td></tr>';
    }

    renderBooks(books) {
        if (!this.tbody) return;
        let htmlContent = '';
        books.forEach(book => {
            let stockBadgeHtml = '';
            if (!book.isContinued) {
                stockBadgeHtml = '<span class="badge bg-secondary status-badge">Discontinued</span>';
            } else if (book.unitsInStock === 0) {
                stockBadgeHtml = '<span class="badge bg-danger status-badge">Out of Stock</span>';
            } else if (book.unitsInStock > 0 && book.unitsInStock < 15) {
                stockBadgeHtml = '<span class="badge bg-warning text-dark status-badge">Low Stock</span>';
            } else {
                stockBadgeHtml = '<span class="badge bg-success status-badge">In Stock</span>';
            }

            const imgUrl = `/images/Book/cover_${book.bookID}.jpg`;
            const formattedPrice = new Intl.NumberFormat('vi-VN').format(book.price) + ' ₫';

            htmlContent += `
                <tr>
                    <td class="align-middle text-muted">${book.bookID}</td>
                    <td>
                        <div class="d-flex align-items-center">
                            <img src="${imgUrl}" class="rounded shadow-sm me-3" style="width: 45px; height: 60px; object-fit: cover;" onerror="this.onerror=null; this.src='/images/Book/book${(book.bookID % 6) + 1}.jpg';"/>
                            <div>
                                <div class="fw-bold text-dark text-truncate" style="max-width: 250px;" title="${book.title}">${book.title}</div>
                                <div class="text-muted small">ISBN: ${book.isbn} | Author: ${book.authors || 'N/A'}</div>
                                <div class="book-authors-list text-muted small" title="${book.publisher || ''}">Publisher: ${book.publisher || 'N/A'}</div>
                            </div>
                        </div>
                    </td>
                    <td class="align-middle">${book.categoryName}</td>
                    <td class="align-middle fw-bold ${book.unitsInStock === 0 ? 'text-danger' : ''}">${book.unitsInStock}</td>
                    <td class="align-middle">${book.reservedQuantity}</td>
                    <td class="align-middle text-danger fw-semibold">${formattedPrice}</td>
                    <td class="align-middle">${stockBadgeHtml}</td>
                    <td class="align-middle text-end text-nowrap">
                        <button class="btn btn-sm btn-outline-danger btn-action-sm btn-create-blind-date me-1" 
                                data-book-id="${book.bookID}"
                                data-title="${book.title.replace(/"/g, '&quot;')}"
                                data-price="${book.price}"
                                data-stock="${book.unitsInStock}"
                                data-reserved="${book.reservedQuantity}"
                                data-mainimage="${imgUrl}"
                                data-category-name="${book.categoryName}"
                                title="Create Blind Date Package">
                            <i class="ph ph-heart"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-primary btn-action-sm btn-edit-book me-1" 
                                data-book-id="${book.bookID}"
                                data-title="${book.title.replace(/"/g, '&quot;')}"
                                data-category="${book.categoryID}"
                                data-publisher="${book.publisher || ''}"
                                data-isbn="${book.isbn}"
                                data-publishyear="${book.publishYear}"
                                data-price="${book.price}"
                                data-weight="${book.weight}"
                                data-stock="${book.unitsInStock}"
                                data-reserved="${book.reservedQuantity}"
                                data-description="${book.description || ''}"
                                data-iscontinued="${book.isContinued}"
                                data-authors="${(book.authors || '').replace(/"/g, '&quot;')}"
                                data-mainimage="${imgUrl}"
                                data-imageurls='${JSON.stringify(book.imageUrls || []).replace(/'/g, "&apos;")}'
                                data-samplefile="${book.sampleFilePath || ''}"
                                title="Edit Book">
                            <i class="ph ph-pencil-simple"></i>
                        </button>
                        <button class="btn btn-sm ${book.isContinued ? 'btn-outline-danger' : 'btn-outline-success'} btn-action-sm btn-delete-item" 
                                data-url="/api/realbook/${book.bookID}" 
                                data-title="${book.isContinued ? 'Discontinue selling book?' : 'Resume selling book?'}" 
                                data-message="Are you sure you want to ${book.isContinued ? 'discontinue' : 'resume'} selling this book?"
                                title="${book.isContinued ? 'Discontinue Selling' : 'Resume Selling'}">
                            <i class="ph ${book.isContinued ? 'ph-minus-circle' : 'ph-check-circle'}"></i>
                        </button>
                    </td>
                </tr>
            `;
        });
        this.tbody.innerHTML = htmlContent;
    }

    renderBookImagePreviews() {
        if (!this.bookImagesPreviewContainer) return;
        this.bookImagesPreviewContainer.innerHTML = "";

        this.selectedBookImages.forEach((file, index) => {
            const reader = new FileReader();
            reader.onload = (e) => {
                const wrapper = document.createElement('div');
                wrapper.className = "image-preview-wrapper";

                const img = document.createElement('img');
                img.src = e.target.result;

                const removeBtn = document.createElement('button');
                removeBtn.type = "button";
                removeBtn.className = "remove-btn";
                removeBtn.innerHTML = "&times;";
                removeBtn.addEventListener('click', (event) => {
                    event.stopPropagation();
                    this.selectedBookImages.splice(index, 1);
                    this.renderBookImagePreviews();
                });

                wrapper.appendChild(img);
                wrapper.appendChild(removeBtn);
                this.bookImagesPreviewContainer.appendChild(wrapper);
            };
            reader.readAsDataURL(file);
        });

        this.updateInputFiles();
    }

    updateInputFiles() {
        if (!this.bookImagesInput) return;
        const dt = new DataTransfer();
        this.selectedBookImages.forEach(file => dt.items.add(file));
        this.bookImagesInput.files = dt.files;
    }

    showAddModal() {
        this.addBookForm.reset();
        this.selectedBookImages = [];
        if (this.bookImagesPreviewContainer) this.bookImagesPreviewContainer.innerHTML = "";
        if (this.currentBookCoverContainer) this.currentBookCoverContainer.style.display = 'none';
        if (this.currentSampleFileContainer) this.currentSampleFileContainer.style.display = 'none';
        if (this.sampleFileInput) this.sampleFileInput.value = '';

        this.addBookForm.dataset.mode = 'add';
        delete this.addBookForm.dataset.bookId;
        this.addBookForm.dataset.reserved = '0';
        this.addBookModalLabel.innerText = "Add New Book";
        if (this.bookImagesInput) this.bookImagesInput.required = true;
        this.bookModal.show();
    }

    showEditModal(bookData) {
        document.getElementById('bookTitle').value = bookData.title;
        document.getElementById('bookCategory').value = bookData.category;
        document.getElementById('bookPublisher').value = bookData.publisher || '';
        document.getElementById('bookIsbn').value = bookData.isbn || '';
        document.getElementById('bookPublishYear').value = bookData.publishyear || '';
        document.getElementById('bookPrice').value = bookData.price;
        document.getElementById('bookWeight').value = bookData.weight || '';
        document.getElementById('bookStock').value = bookData.stock;
        document.getElementById('bookDescription').value = bookData.description || '';
        document.getElementById('bookAuthors').value = bookData.authors || '';
        document.getElementById('isContinued').checked = bookData.iscontinued === 'true';

        this.selectedBookImages = [];
        if (this.bookImagesPreviewContainer) this.bookImagesPreviewContainer.innerHTML = "";
        if (this.bookImagesInput) {
            this.bookImagesInput.required = false;
            this.bookImagesInput.value = "";
        }

        if (this.currentBookCoverContainer) {
            let urls = [];
            try {
                if (bookData.imageurls) {
                    urls = JSON.parse(bookData.imageurls);
                } else if (bookData.mainimage) {
                    urls = [bookData.mainimage];
                }
            } catch (e) {
                if (bookData.mainimage) urls = [bookData.mainimage];
            }

            if (urls.length > 0) {
                let html = '<label class="form-label text-muted small d-block">Current Images:</label><div class="d-flex flex-wrap gap-2">';
                urls.forEach(url => {
                    html += `
                        <div class="image-preview-wrapper" style="width: 80px; height: 100px; border: 1px solid #dee2e6; padding: 2px; border-radius: 4px;">
                            <img src="${url}" style="width: 100%; height: 100%; object-fit: cover; border-radius: 2px;" onerror="this.style.display='none'; this.parentElement.style.display='none';" />
                        </div>
                    `;
                });
                html += '</div>';
                this.currentBookCoverContainer.innerHTML = html;
                this.currentBookCoverContainer.style.display = 'block';
            } else {
                this.currentBookCoverContainer.style.display = 'none';
            }
        }

        if (this.currentSampleFileContainer) {
            if (bookData.samplefile) {
                this.currentSampleFileContainer.innerHTML = `Hiện tại: <a href="${bookData.samplefile}" target="_blank" class="text-primary text-decoration-none"><i class="ph ph-file-pdf"></i> Xem file Sample</a>`;
                this.currentSampleFileContainer.style.display = 'block';
            } else {
                this.currentSampleFileContainer.innerHTML = '';
                this.currentSampleFileContainer.style.display = 'none';
            }
        }
        if (this.sampleFileInput) this.sampleFileInput.value = '';

        this.addBookForm.dataset.mode = 'edit';
        this.addBookForm.dataset.bookId = bookData.bookId;
        this.addBookForm.dataset.reserved = bookData.reserved || '0';
        this.addBookModalLabel.innerText = "Edit Book";

        this.bookModal.show();
    }

    hideModal() {
        if (this.bookModal) this.bookModal.hide();
    }

    setSavingState(isSaving) {
        const submitBtn = document.querySelector(`button[form="addBookForm"]`);
        if (submitBtn) {
            if (isSaving) {
                this.originalBtnText = submitBtn.innerHTML;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Saving...';
                submitBtn.disabled = true;
            } else {
                submitBtn.innerHTML = this.originalBtnText || "Save Book";
                submitBtn.disabled = false;
            }
        }
    }
}

window.BookView = BookView;
