document.addEventListener('DOMContentLoaded', function () {
    const addBookBtn = document.getElementById('btn-add-book');
    const addBookModalElement = document.getElementById('addBookModal');
    const addBookForm = document.getElementById('addBookForm');
    const addBookModalLabel = document.getElementById('addBookModalLabel');
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
    const searchBooksInput = document.getElementById('searchBooksInput');
    const filterCategory = document.getElementById('filterCategory');
    const filterBookStatus = document.getElementById('filterBookStatus');
    const booksNoResultsRow = document.createElement('tr');

    if (searchBooksInput) {
        // Create dynamic "No matching books" row
        booksNoResultsRow.id = 'booksNoResultsRow';
        booksNoResultsRow.style.display = 'none';
        booksNoResultsRow.innerHTML = '<td colspan="8" class="text-center text-muted py-4"><i class="bi bi-search me-2"></i>No books match your filters.</td>';
        const tbody = document.querySelector('#books tbody');
        if (tbody) tbody.appendChild(booksNoResultsRow);

        function filterBooks() {
            const query = searchBooksInput.value.toLowerCase().trim();
            const categoryFilter = filterCategory ? filterCategory.value.toLowerCase().trim() : "";
            const statusFilter = filterBookStatus ? filterBookStatus.value.toLowerCase().trim() : "";

            const rows = document.querySelectorAll('#books tbody tr:not(#booksNoResultsRow)');
            let visibleCount = 0;
            let totalCount = 0;

            rows.forEach(row => {
                // If it is the default backend "No books found" placeholder, hide it if any filter is set
                if (row.cells.length === 1 && row.cells[0].colSpan === 8 && !row.id) {
                    row.style.display = (query || categoryFilter || statusFilter) ? "none" : "";
                    return;
                }
                totalCount++;

                const titleElement = row.querySelector('.fw-bold.text-dark');
                const isbnElement = row.querySelector('.text-muted.small');
                const authorElement = row.querySelector('.book-authors-list');
                const categoryCell = row.cells[2];
                const statusBadge = row.querySelector('.status-badge');
                const bookIdCell = row.cells[0];

                const title = titleElement ? titleElement.textContent.toLowerCase() : "";
                const isbn = isbnElement ? isbnElement.textContent.toLowerCase() : "";
                const author = authorElement ? authorElement.getAttribute('title').toLowerCase() : "";
                const category = categoryCell ? categoryCell.textContent.toLowerCase().trim() : "";
                const status = statusBadge ? statusBadge.textContent.toLowerCase().trim() : "";
                const bookId = bookIdCell ? bookIdCell.textContent.toLowerCase() : "";

                const matchesQuery = !query || title.includes(query) || isbn.includes(query) || category.includes(query) || bookId.includes(query) || author.includes(query);
                const matchesCategory = !categoryFilter || category === categoryFilter;
                const matchesStatus = !statusFilter || status === statusFilter;

                if (matchesQuery && matchesCategory && matchesStatus) {
                    row.style.display = "";
                    visibleCount++;
                } else {
                    row.style.display = "none";
                }
            });

            if ((query || categoryFilter || statusFilter) && visibleCount === 0 && totalCount > 0) {
                booksNoResultsRow.style.display = '';
            } else {
                booksNoResultsRow.style.display = 'none';
            }
        }

        searchBooksInput.addEventListener('input', filterBooks);
        if (filterCategory) filterCategory.addEventListener('change', filterBooks);
        if (filterBookStatus) filterBookStatus.addEventListener('change', filterBooks);
    }

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
});

