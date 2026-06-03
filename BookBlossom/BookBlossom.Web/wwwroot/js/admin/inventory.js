function initInventorySetup() {
    const addBookBtn = document.getElementById('btn-add-book');
    const addBookModalElement = document.getElementById('addBookModal');
    const addBookForm = document.getElementById('addBookForm');
    const addBookModalLabel = document.getElementById('addBookModalLabel');
    const searchBooksInput = document.getElementById('searchBooksInput');
    let bookModal = null;
    if (addBookModalElement) {
        bookModal = new bootstrap.Modal(addBookModalElement);
    }

    // In-memory file array to manage uploaded images dynamically
    let selectedBookImages = [];
    const bookImagesInput = document.getElementById('bookImagesInput');
    const bookImagesPreviewContainer = document.getElementById('bookImagesPreviewContainer');

    function renderBookImagePreviews() {
        if (!bookImagesPreviewContainer) return;
        bookImagesPreviewContainer.innerHTML = "";

        selectedBookImages.forEach((file, index) => {
            const reader = new FileReader();
            reader.onload = function (e) {
                const wrapper = document.createElement('div');
                wrapper.className = "image-preview-wrapper";

                const img = document.createElement('img');
                img.src = e.target.result;

                const removeBtn = document.createElement('button');
                removeBtn.type = "button";
                removeBtn.className = "remove-btn";
                removeBtn.innerHTML = "&times;";
                removeBtn.addEventListener('click', function (event) {
                    event.stopPropagation();
                    selectedBookImages.splice(index, 1);
                    renderBookImagePreviews();
                });

                wrapper.appendChild(img);
                wrapper.appendChild(removeBtn);
                bookImagesPreviewContainer.appendChild(wrapper);
            };
            reader.readAsDataURL(file);
        });

        updateInputFiles();
    }

    function updateInputFiles() {
        if (!bookImagesInput) return;
        const dt = new DataTransfer();
        selectedBookImages.forEach(file => dt.items.add(file));
        bookImagesInput.files = dt.files;
    }

    if (bookImagesInput) {
        bookImagesInput.addEventListener('change', function () {
            for (let i = 0; i < this.files.length; i++) {
                selectedBookImages.push(this.files[i]);
            }
            renderBookImagePreviews();
        });
    }

    if (addBookBtn) {
        addBookBtn.addEventListener('click', function () {
            // Reset form for ADDING new book
            addBookForm.reset();
            selectedBookImages = [];
            if (bookImagesPreviewContainer) bookImagesPreviewContainer.innerHTML = "";

            // Hide current cover image container when adding
            const currentBookCoverContainer = document.getElementById('currentBookCoverContainer');
            if (currentBookCoverContainer) currentBookCoverContainer.style.display = 'none';

            addBookForm.action = "/Admin/AddBook";
            addBookForm.dataset.mode = 'add';
            delete addBookForm.dataset.bookId;
            addBookForm.dataset.reserved = '0';
            addBookModalLabel.innerText = "Add New Book";
            if (bookImagesInput) bookImagesInput.required = true; // Required for new book
            bookModal.show();
        });
    }

    // Handle Edit Book Button clicks
    document.querySelectorAll('.btn-edit-book').forEach(btn => {
        btn.addEventListener('click', function () {
            // Fill values from data attributes
            document.getElementById('bookTitle').value = this.dataset.title;
            document.getElementById('bookCategory').value = this.dataset.category;
            document.getElementById('bookPublisher').value = this.dataset.publisher || '';
            document.getElementById('bookIsbn').value = this.dataset.isbn || '';
            document.getElementById('bookPublishYear').value = this.dataset.publishyear || '';
            document.getElementById('bookPrice').value = this.dataset.price;
            document.getElementById('bookWeight').value = this.dataset.weight || '';
            document.getElementById('bookStock').value = this.dataset.stock;
            document.getElementById('bookDescription').value = this.dataset.description || '';
            document.getElementById('bookAuthors').value = this.dataset.authors || '';

            const isContinuedChecked = this.dataset.iscontinued === 'true';
            document.getElementById('isContinued').checked = isContinuedChecked;

            // Reset image selections on edit
            selectedBookImages = [];
            if (bookImagesPreviewContainer) bookImagesPreviewContainer.innerHTML = "";
            if (bookImagesInput) {
                bookImagesInput.required = false;
                bookImagesInput.value = "";
            }

            // Display current cover image inside edit form
            const currentBookCoverContainer = document.getElementById('currentBookCoverContainer');
            const currentBookCoverImg = document.getElementById('currentBookCoverImg');
            if (currentBookCoverContainer && currentBookCoverImg) {
                const mainImage = this.dataset.mainimage;
                if (mainImage) {
                    currentBookCoverImg.src = mainImage;
                    currentBookCoverContainer.style.display = 'block';
                } else {
                    currentBookCoverContainer.style.display = 'none';
                }
            }

            // Update form action and modal header
            addBookForm.action = `/Admin/EditBook?id=${this.dataset.bookId}`;
            addBookForm.dataset.mode = 'edit';
            addBookForm.dataset.bookId = this.dataset.bookId;
            addBookForm.dataset.reserved = this.dataset.reserved || '0';
            addBookModalLabel.innerText = "Edit Book";

            bookModal.show();
        });
    });

    const addCategoryBtn = document.getElementById('btn-add-category');
    const addCategoryModalElement = document.getElementById('addCategoryModal');
    const addCategoryForm = document.getElementById('addCategoryForm');
    const addCategoryModalLabel = document.getElementById('addCategoryModalLabel');
    let categoryModal = null;
    if (addCategoryModalElement) {
        categoryModal = new bootstrap.Modal(addCategoryModalElement);
    }

    if (addCategoryBtn) {
        addCategoryBtn.addEventListener('click', function () {
            // Reset form for ADDING new category
            addCategoryForm.reset();
            addCategoryForm.action = "/Admin/AddCategory";
            addCategoryModalLabel.innerText = "Add Category";
            categoryModal.show();
        });
    }

    // Handle Edit Category Button clicks
    document.querySelectorAll('.btn-edit-category').forEach(btn => {
        btn.addEventListener('click', function () {
            document.getElementById('categoryNameInput').value = this.dataset.name;
            document.getElementById('categoryDescInput').value = this.dataset.desc || '';
            document.getElementById('categoryStatusInput').value = this.dataset.status || 'Active';

            // Update form action and modal header
            addCategoryForm.action = `/Admin/EditCategory?id=${this.dataset.catId}`;
            addCategoryModalLabel.innerText = "Edit Category";

            categoryModal.show();
        });
    });

    // Initialize Delete Confirmation Modal
    const deleteConfirmModalElement = document.getElementById('deleteConfirmModal');
    let deleteConfirmModal = null;
    if (deleteConfirmModalElement) {
        deleteConfirmModal = new bootstrap.Modal(deleteConfirmModalElement);
    }

    // Handle Delete Item clicks
    document.querySelectorAll('.btn-delete-item').forEach(btn => {
        btn.addEventListener('click', function (e) {
            e.preventDefault();
            const deleteUrl = this.dataset.url;
            const deleteTitle = this.dataset.title || 'Are you sure?';
            const deleteMessage = this.dataset.message || 'Do you really want to delete this item? This action cannot be undone.';

            const titleElem = document.getElementById('deleteConfirmTitle');
            const messageElem = document.getElementById('deleteConfirmMessage');
            const confirmBtnElem = document.getElementById('deleteConfirmBtn');

            if (titleElem) titleElem.innerText = deleteTitle;
            if (messageElem) messageElem.innerHTML = deleteMessage; // Use innerHTML to support quotes/HTML
            if (confirmBtnElem) confirmBtnElem.setAttribute('href', deleteUrl);

            if (deleteConfirmModal) {
                deleteConfirmModal.show();
            }
        });
    });

    // Auto-dismiss alert notifications after 4 seconds with fadeOutUp animation
    document.querySelectorAll('.custom-alert').forEach(alert => {
        setTimeout(() => {
            alert.style.animation = "fadeOutUp 0.4s ease-out forwards";
            setTimeout(() => {
                alert.remove();
            }, 400);
        }, 4000);
    });

    // --- Live Instant Filtering for Books (Search + Category + Status) ---
    // const searchBooksInput = document.getElementById('searchBooksInput');
    // const filterCategory = document.getElementById('filterCategory');
    // const filterBookStatus = document.getElementById('filterBookStatus');
    // const booksNoResultsRow = document.createElement('tr');

    // if (searchBooksInput) {
    //     // Create dynamic "No matching books" row
    //     booksNoResultsRow.id = 'booksNoResultsRow';
    //     booksNoResultsRow.style.display = 'none';
    //     booksNoResultsRow.innerHTML = '<td colspan="8" class="text-center text-muted py-4"><i class="bi bi-search me-2"></i>No books match your filters.</td>';
    //     const tbody = document.querySelector('#books tbody');
    //     if (tbody) tbody.appendChild(booksNoResultsRow);

    //     function filterBooks() {
    //         const query = searchBooksInput.value.toLowerCase().trim();
    //         const categoryFilter = filterCategory ? filterCategory.value.toLowerCase().trim() : "";
    //         const statusFilter = filterBookStatus ? filterBookStatus.value.toLowerCase().trim() : "";

    //         const rows = document.querySelectorAll('#books tbody tr:not(#booksNoResultsRow)');
    //         let visibleCount = 0;
    //         let totalCount = 0;

    //         rows.forEach(row => {
    //             // If it is the default backend "No books found" placeholder, hide it if any filter is set
    //             if (row.cells.length === 1 && row.cells[0].colSpan === 8 && !row.id) {
    //                 row.style.display = (query || categoryFilter || statusFilter) ? "none" : "";
    //                 return;
    //             }
    //             totalCount++;

    //             const titleElement = row.querySelector('.fw-bold.text-dark');
    //             const isbnElement = row.querySelector('.text-muted.small');
    //             const authorElement = row.querySelector('.book-authors-list');
    //             const categoryCell = row.cells[2];
    //             const statusBadge = row.querySelector('.status-badge');
    //             const bookIdCell = row.cells[0];

    //             const title = titleElement ? titleElement.textContent.toLowerCase() : "";
    //             const isbn = isbnElement ? isbnElement.textContent.toLowerCase() : "";
    //             const author = authorElement ? authorElement.getAttribute('title').toLowerCase() : "";
    //             const category = categoryCell ? categoryCell.textContent.toLowerCase().trim() : "";
    //             const status = statusBadge ? statusBadge.textContent.toLowerCase().trim() : "";
    //             const bookId = bookIdCell ? bookIdCell.textContent.toLowerCase() : "";

    //             const matchesQuery = !query || title.includes(query) || isbn.includes(query) || category.includes(query) || bookId.includes(query) || author.includes(query);
    //             const matchesCategory = !categoryFilter || category === categoryFilter;
    //             const matchesStatus = !statusFilter || status === statusFilter;

    //             if (matchesQuery && matchesCategory && matchesStatus) {
    //                 row.style.display = "";
    //                 visibleCount++;
    //             } else {
    //                 row.style.display = "none";
    //             }
    //         });

    //         if ((query || categoryFilter || statusFilter) && visibleCount === 0 && totalCount > 0) {
    //             booksNoResultsRow.style.display = '';
    //         } else {
    //             booksNoResultsRow.style.display = 'none';
    //         }
    //     }

    //     searchBooksInput.addEventListener('input', filterBooks);
    //     if (filterCategory) filterCategory.addEventListener('change', filterBooks);
    //     if (filterBookStatus) filterBookStatus.addEventListener('change', filterBooks);
    // }

    // --- Live Instant Filtering for Categories (Search + Status) ---
    const searchCategoriesInput = document.getElementById('searchCategoriesInput');
    const filterCategoryStatus = document.getElementById('filterCategoryStatus');
    const categoriesNoResultsRow = document.createElement('tr');

    if (searchCategoriesInput) {
        // Create dynamic "No matching categories" row
        categoriesNoResultsRow.id = 'categoriesNoResultsRow';
        categoriesNoResultsRow.style.display = 'none';
        categoriesNoResultsRow.innerHTML = '<td colspan="6" class="text-center text-muted py-4"><i class="bi bi-search me-2"></i>No categories match your filters.</td>';
        const tbody = document.querySelector('#categories tbody');
        if (tbody) tbody.appendChild(categoriesNoResultsRow);

        function filterCategories() {
            const query = searchCategoriesInput.value.toLowerCase().trim();
            const statusFilter = filterCategoryStatus ? filterCategoryStatus.value.toLowerCase().trim() : "";

            const rows = document.querySelectorAll('#categories tbody tr:not(#categoriesNoResultsRow)');
            let visibleCount = 0;
            let totalCount = 0;

            rows.forEach(row => {
                // If it is the default backend "No categories found" placeholder, hide it if any filter is set
                if (row.cells.length === 1 && row.cells[0].colSpan === 6 && !row.id) {
                    row.style.display = (query || statusFilter) ? "none" : "";
                    return;
                }
                totalCount++;

                const idCell = row.cells[0];
                const nameCell = row.cells[1];
                const descCell = row.cells[2];
                const statusBadge = row.querySelector('.badge'); // Active or Inactive badge

                const id = idCell ? idCell.textContent.toLowerCase() : "";
                const name = nameCell ? nameCell.textContent.toLowerCase() : "";
                const desc = descCell ? descCell.textContent.toLowerCase() : "";
                const status = statusBadge ? statusBadge.textContent.toLowerCase().trim() : "";

                const matchesQuery = !query || id.includes(query) || name.includes(query) || desc.includes(query);
                const matchesStatus = !statusFilter || status === statusFilter;

                if (matchesQuery && matchesStatus) {
                    row.style.display = "";
                    visibleCount++;
                } else {
                    row.style.display = "none";
                }
            });

            if ((query || statusFilter) && visibleCount === 0 && totalCount > 0) {
                categoriesNoResultsRow.style.display = '';
            } else {
                categoriesNoResultsRow.style.display = 'none';
            }
        }

        searchCategoriesInput.addEventListener('input', filterCategories);
        if (filterCategoryStatus) filterCategoryStatus.addEventListener('change', filterCategories);
    }

    // --- Handle Direct Navigation to Book via URL (e.g., from Content Reports) ---
    const urlParams = new URLSearchParams(window.location.search);
    const bookTitleFromUrl = urlParams.get('book');
    if (bookTitleFromUrl && searchBooksInput) {
        // Switch to the books tab
        const booksTabBtn = document.getElementById('books-tab');
        if (booksTabBtn) {
            booksTabBtn.click();
        }

        // Set search input and trigger filter
        searchBooksInput.value = bookTitleFromUrl;
        searchBooksInput.dispatchEvent(new Event('input'));

        // Find the visible row that matches this title exactly and add a highlight effect
        setTimeout(() => {
            const rows = document.querySelectorAll('#books tbody tr:not(#booksNoResultsRow)');
            for (let row of rows) {
                if (row.style.display !== 'none') {
                    const titleElement = row.querySelector('.fw-bold.text-dark');
                    if (titleElement && titleElement.textContent.toLowerCase() === bookTitleFromUrl.toLowerCase()) {
                        // Scroll into view
                        row.scrollIntoView({ behavior: 'smooth', block: 'center' });
                        // Add highlight effect (light red)
                        row.style.transition = 'background-color 0.8s ease, box-shadow 0.3s ease';
                        row.style.backgroundColor = 'rgba(220, 38, 38, 0.15)'; // red-600 with 15% opacity
                        row.style.boxShadow = '0 0 10px rgba(220, 38, 38, 0.3)';
                        row.style.position = 'relative';
                        row.style.zIndex = '10';

                        setTimeout(() => {
                            row.style.backgroundColor = '';
                            row.style.boxShadow = '';
                            row.style.zIndex = '';
                        }, 3000); // Effect lasts 3 seconds
                        break;
                    }
                }
            }
        }, 150);
    }

    // Bind Submit Event Listeners to fresh DOM elements
    const confirmBtnElem = document.getElementById('deleteConfirmBtn');
    if (confirmBtnElem) {
        confirmBtnElem.addEventListener('click', async function (e) {
            e.preventDefault();
            const url = this.dataset.targetUrl;
            if (!url) return;

            try {
                const result = await window.apiClient.apiDelete(url);
                const modal = getSafeModal(document.getElementById('deleteConfirmModal'));
                if (modal) modal.hide();
                loadInventoryBooks();
                loadInventoryCategories();

                if (window.apiClient) {
                    window.apiClient.showToast("Status updated successfully!", "success");
                }
            } catch (error) {
                console.error(error);
                alert("Error: " + error.message);
            }
        });
    }

    // Real book restock workflow removed

    if (addBookForm) {
        addBookForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const fileInput = document.querySelector('input[name="SampleFile"]');
            if (fileInput && fileInput.files.length > 0) {
                if (fileInput.files[0].size > 10 * 1024 * 1024) {
                    alert("Error: Sample PDF file size cannot exceed 10MB!");
                    return;
                }
            }

            const priceVal = parseFloat(document.getElementById('bookPrice').value);
            if (priceVal <= 0) {
                alert("Error: Book price must be greater than 0!");
                return;
            }

            const stockVal = parseInt(document.getElementById('bookStock').value);
            const reservedVal = parseInt(this.dataset.reserved || '0');
            const mode = this.dataset.mode || 'add';

            if (mode === 'edit' && stockVal < reservedVal) {
                alert(`Error: New stock quantity (${stockVal}) cannot be less than the reserved quantity (${reservedVal})!`);
                return;
            }

            const formData = new FormData(this);
            const bookId = this.dataset.bookId;
            const url = mode === 'edit' ? `/api/realbook/${bookId}` : '/api/realbook';
            const method = mode === 'edit' ? 'PUT' : 'POST';

            const submitBtn = document.querySelector(`button[form="addBookForm"]`);
            let originalBtnText = "Save Book";
            if (submitBtn) {
                originalBtnText = submitBtn.innerHTML;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Saving...';
                submitBtn.disabled = true;
            }

            try {
                const result = await window.apiClient.apiUpload(url, formData, method);
                const modal = getSafeModal(addBookModalElement);
                if (modal) modal.hide();
                loadInventoryBooks();
                loadInventoryCategories();

                if (window.apiClient) {
                    window.apiClient.showToast("Saved book details successfully!", "success");
                }
            } catch (error) {
                console.error("Save Book Error:", error);
                alert("Error: " + error.message);
            } finally {
                if (submitBtn) {
                    submitBtn.innerHTML = originalBtnText;
                    submitBtn.disabled = false;
                }
            }
        });
    }

    const addCategoryFormElement = document.getElementById('addCategoryForm');
    if (addCategoryFormElement) {
        addCategoryFormElement.addEventListener('submit', async function (e) {
            e.preventDefault();

            const catName = document.getElementById('categoryNameInput').value.trim();
            const catDesc = document.getElementById('categoryDescInput').value.trim();
            const catStatusText = document.getElementById('categoryStatusInput').value;
            const catStatus = catStatusText === 'Active' ? 1 : 0;

            if (!catName) {
                alert("Please enter a category name.");
                return;
            }

            const mode = this.dataset.mode || 'add';
            const catId = this.dataset.catId;

            const url = mode === 'edit' ? `/api/category/${catId}` : '/api/category';
            const method = mode === 'edit' ? 'PUT' : 'POST';

            const submitBtn = document.querySelector(`button[form="addCategoryForm"]`);
            let originalText = "Save Category";
            if (submitBtn) {
                originalText = submitBtn.innerHTML;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Saving...';
                submitBtn.disabled = true;
            }

            try {
                const payload = {
                    CategoryName: catName,
                    Description: catDesc,
                    Status: catStatus
                };
                const result = method === 'PUT' ? await window.apiClient.apiPut(url, payload) : await window.apiClient.apiPost(url, payload);

                const modal = getSafeModal(document.getElementById('addCategoryModal'));
                if (modal) modal.hide();
                loadInventoryCategories();
                loadInventoryBooks();

                if (window.apiClient) {
                    window.apiClient.showToast("Saved category successfully!", "success");
                }
            } catch (error) {
                console.error("Save Category Error:", error);
                alert("Error: " + error.message);
            } finally {
                if (submitBtn) {
                    submitBtn.innerHTML = originalText;
                    submitBtn.disabled = false;
                }
            }
        });
    }
}


// Helper function to get or create bootstrap Modal instance safely
function getSafeModal(element) {
    if (!element) return null;
    return bootstrap.Modal.getInstance(element) || new bootstrap.Modal(element);
}

// ==========================================
// INTEGRATE CATEGORIES AND RESTOCK WORKFLOWS
// ==========================================

function initInventoryData() {
    // Tải dữ liệu ban đầu
    loadInventoryBooks(true);
    loadInventoryCategories(true);

    // Lắng nghe sự kiện thay đổi trên các ô Search và Filter của Sách
    const searchInput = document.getElementById('searchBooksInput');
    const categoryFilter = document.getElementById('filterCategory');
    const statusFilter = document.getElementById('filterBookStatus');

    if (searchInput) searchInput.addEventListener('input', debounce(() => loadInventoryBooks(false), 500));
    if (categoryFilter) categoryFilter.addEventListener('change', () => loadInventoryBooks(false));
    if (statusFilter) statusFilter.addEventListener('change', () => loadInventoryBooks(false));
}

// Hàm hỗ trợ delay việc gọi API khi đang gõ chữ
function debounce(func, delay) {
    let timeout;
    return function (...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(this, args), delay);
    };
}

// 1. TẢI DANH SÁCH SÁCH TỪ API THẬT
async function loadInventoryBooks(isInitial = false) {
    const tbody = document.getElementById('booksTableBody');
    if (!tbody) return;

    const searchTerm = document.getElementById('searchBooksInput')?.value || '';
    const category = document.getElementById('filterCategory')?.value || '';
    const filterStatus = document.getElementById('filterBookStatus')?.value || '';

    // Check if we already have rows from SWR cache
    const hasExistingData = tbody.children.length > 0 && !tbody.querySelector('.spinner-border') && !tbody.querySelector('.text-muted');
    if (!isInitial || !hasExistingData) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center py-5"><div class="spinner-border text-primary mb-2"></div><div class="text-muted small">Loading book inventory data from API...</div></td></tr>';
    }

    try {
        // Calling API including discontinued books (includeDiscontinued=true)
        const url = `/api/realbook?searchTerm=${encodeURIComponent(searchTerm)}&category=${encodeURIComponent(category)}&includeDiscontinued=true`;

        let books = await window.apiClient.apiGet(url);

        // Client-side filtering by status
        if (filterStatus) {
            books = books.filter(book => {
                if (filterStatus === 'Discontinued') return !book.isContinued;
                if (filterStatus === 'Out of Stock') return book.isContinued && book.unitsInStock === 0;
                if (filterStatus === 'Low Stock') return book.isContinued && book.unitsInStock > 0 && book.unitsInStock < 15;
                if (filterStatus === 'In Stock') return book.isContinued && book.unitsInStock >= 15;
                return true;
            });
        }

        tbody.innerHTML = '';

        if (books.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No books found matching the filter criteria.</td></tr>';
            return;
        }

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
                                <div class="text-muted small">ISBN: ${book.isbn} | Tác giả: ${book.authors || 'N/A'}</div>
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

        tbody.innerHTML = htmlContent;

    } catch (error) {
        console.error("Error loading books:", error);
        tbody.innerHTML = `<tr><td colspan="8" class="text-center py-4 text-danger">An error occurred while loading book data.</td></tr>`;
    }
}

// 2. LOAD CATEGORIES LIST FROM REAL API
async function loadInventoryCategories(isInitial = false) {
    const tbody = document.getElementById('categoriesTableBody');
    if (!tbody) return;

    // Check if we already have rows from SWR cache
    const hasExistingData = tbody.children.length > 0 && !tbody.querySelector('.spinner-border');
    if (!isInitial || !hasExistingData) {
        tbody.innerHTML = '<tr><td colspan="6" class="text-center py-5"><div class="spinner-border" style="color: #E3597D;" role="status"></div><div class="text-muted small mt-2">Loading categories from API...</div></td></tr>';
    }

    try {
        const categories = await window.apiClient.apiGet('/api/category');

        // Synchronize category dropdowns across the page
        updateCategoryDropdowns(categories);

        tbody.innerHTML = '';
        if (categories.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">No categories found.</td></tr>';
            return;
        }

        let htmlContent = '';
        categories.forEach(cat => {
            const statusText = cat.status === 1 ? 'Active' : (cat.status === 2 ? 'Archived' : 'Inactive');
            const badgeClass = cat.status === 1 ? 'bg-success' : 'bg-secondary';

            htmlContent += `
                <tr>
                    <td class="align-middle text-muted">${cat.categoryID}</td>
                    <td class="align-middle fw-bold text-dark">${cat.categoryName}</td>
                    <td class="align-middle text-muted" style="max-width: 250px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" title="${cat.description || ''}">${cat.description || 'N/A'}</td>
                    <td class="align-middle"><span class="badge ${badgeClass}">${statusText}</span></td>
                    <td class="align-middle fw-bold">${cat.bookCount}</td>
                    <td class="align-middle text-end text-nowrap">
                        <button class="btn btn-sm btn-outline-primary btn-action-sm btn-edit-category me-1" 
                                data-cat-id="${cat.categoryID}"
                                data-name="${cat.categoryName.replace(/"/g, '&quot;')}"
                                data-desc="${(cat.description || '').replace(/"/g, '&quot;')}"
                                data-status="${statusText}"
                                title="Edit Category">
                            <i class="ph ph-pencil-simple"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger btn-action-sm btn-delete-item" 
                                data-url="/api/category/${cat.categoryID}" 
                                data-title="Delete category?" 
                                data-message="Are you sure you want to delete category &quot;${cat.categoryName}&quot;? This action will set the category to Inactive if valid."
                                title="Delete Category">
                            <i class="ph ph-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
        });

        tbody.innerHTML = htmlContent;

    } catch (error) {
        console.error("Error loading categories:", error);
        tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-danger">An error occurred while loading category data from API.</td></tr>';
    }
}

// HÀM ĐỒNG BỘ DỮ LIỆU ĐỘNG CHO CÁC DROPDOWN THỂ LOẠI
function updateCategoryDropdowns(categories) {
    // 1. Dropdown lọc ở tab sách
    const filterCategory = document.getElementById('filterCategory');
    if (filterCategory) {
        const currentVal = filterCategory.value;
        filterCategory.innerHTML = '<option value="">All Categories</option>';
        categories.forEach(cat => {
            filterCategory.innerHTML += `<option value="${cat.categoryName}">${cat.categoryName}</option>`;
        });
        filterCategory.value = currentVal;
    }

    // 2. Dropdown lọc ở tab Blind Date
    const filterBlindDateCategory = document.getElementById('filterBlindDateCategory');
    if (filterBlindDateCategory) {
        const currentVal = filterBlindDateCategory.value;
        filterBlindDateCategory.innerHTML = '<option value="">All Categories</option>';
        categories.forEach(cat => {
            filterBlindDateCategory.innerHTML += `<option value="${cat.categoryName}">${cat.categoryName}</option>`;
        });
        filterBlindDateCategory.value = currentVal;
    }

    // 3. Dropdown chọn thể loại trong modal Thêm/Sửa sách
    const bookCategory = document.getElementById('bookCategory');
    if (bookCategory) {
        const currentVal = bookCategory.value;
        bookCategory.innerHTML = '<option value="">Select Category</option>';
        categories.forEach(cat => {
            if (cat.status === 1) { // Chỉ cho phép chọn danh mục đang Active
                bookCategory.innerHTML += `<option value="${cat.categoryID}">${cat.categoryName}</option>`;
            }
        });
        bookCategory.value = currentVal;
    }
}

// 3. EVENT DELEGATION LẮNG NGHE CLICK TRÊN TOÀN TRANG
document.addEventListener('click', function (e) {
    // Bắt sự kiện bấm nút Edit sách
    const editBtn = e.target.closest('.btn-edit-book');
    if (editBtn) {
        document.getElementById('bookTitle').value = editBtn.dataset.title;
        document.getElementById('bookCategory').value = editBtn.dataset.category;
        document.getElementById('bookPublisher').value = editBtn.dataset.publisher || '';
        document.getElementById('bookIsbn').value = editBtn.dataset.isbn || '';
        document.getElementById('bookPublishYear').value = editBtn.dataset.publishyear || '';
        document.getElementById('bookPrice').value = editBtn.dataset.price;
        document.getElementById('bookWeight').value = editBtn.dataset.weight || '';
        document.getElementById('bookStock').value = editBtn.dataset.stock;
        document.getElementById('bookDescription').value = editBtn.dataset.description || '';
        document.getElementById('bookAuthors').value = editBtn.dataset.authors || '';
        document.getElementById('isContinued').checked = editBtn.dataset.iscontinued === 'true';

        const form = document.getElementById('addBookForm');
        form.dataset.mode = 'edit';
        form.dataset.bookId = editBtn.dataset.bookId;
        form.dataset.reserved = editBtn.dataset.reserved || '0';

        const currentBookCoverContainer = document.getElementById('currentBookCoverContainer');
        const currentBookCoverImg = document.getElementById('currentBookCoverImg');
        if (currentBookCoverContainer && currentBookCoverImg) {
            currentBookCoverImg.src = editBtn.dataset.mainimage;
            currentBookCoverImg.onerror = function () {
                this.onerror = null;
                this.src = `/images/Book/book${(editBtn.dataset.bookId % 6) + 1}.jpg`;
            };
            currentBookCoverContainer.style.display = 'block';
        }

        document.getElementById('addBookModalLabel').innerText = "Edit Book";
        const modal = getSafeModal(document.getElementById('addBookModal'));
        if (modal) modal.show();
    }

    // Bắt sự kiện bấm nút Edit thể loại
    const editCatBtn = e.target.closest('.btn-edit-category');
    if (editCatBtn) {
        document.getElementById('categoryNameInput').value = editCatBtn.dataset.name;
        document.getElementById('categoryDescInput').value = editCatBtn.dataset.desc || '';
        document.getElementById('categoryStatusInput').value = editCatBtn.dataset.status || 'Active';

        const form = document.getElementById('addCategoryForm');
        form.dataset.mode = 'edit';
        form.dataset.catId = editCatBtn.dataset.catId;

        document.getElementById('addCategoryModalLabel').innerText = "Edit Category";
        const modal = getSafeModal(document.getElementById('addCategoryModal'));
        if (modal) modal.show();
    }



    // Bắt sự kiện bấm nút Delete (dùng chung cho cả Sách và Thể Loại)
    const deleteBtn = e.target.closest('.btn-delete-item');
    if (deleteBtn) {
        e.preventDefault();
        document.getElementById('deleteConfirmTitle').innerText = deleteBtn.dataset.title;
        document.getElementById('deleteConfirmMessage').innerHTML = deleteBtn.dataset.message;
        document.getElementById('deleteConfirmBtn').dataset.targetUrl = deleteBtn.dataset.url;

        const modal = getSafeModal(document.getElementById('deleteConfirmModal'));
        if (modal) modal.show();
    }
});

// Global Event Delegation and helpers are kept. Event listeners are bound on setup.
