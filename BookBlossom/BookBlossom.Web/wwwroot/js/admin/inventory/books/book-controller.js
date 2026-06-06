class BookController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
        
        this.init();
    }

    init() {
        this.loadBooks();
        this.bindEvents();
    }

    async loadBooks() {
        const searchInput = document.getElementById('searchBooksInput');
        const categoryFilter = document.getElementById('filterCategory');
        const statusFilter = document.getElementById('filterBookStatus');

        const searchTerm = searchInput ? searchInput.value : '';
        const category = categoryFilter ? categoryFilter.value : '';
        const status = statusFilter ? statusFilter.value : '';

        this.view.renderLoading();
        try {
            const books = await this.model.getBooks(searchTerm, category, status);
            if (books.length === 0) {
                this.view.renderEmpty();
            } else {
                this.view.renderBooks(books);
            }
        } catch (error) {
            console.error("Error loading books:", error);
            this.view.renderError();
        }
    }

    bindEvents() {
        // Search and Filter Events
        const searchInput = document.getElementById('searchBooksInput');
        const categoryFilter = document.getElementById('filterCategory');
        const statusFilter = document.getElementById('filterBookStatus');

        const debounce = (func, delay) => {
            let timeout;
            return function (...args) {
                clearTimeout(timeout);
                timeout = setTimeout(() => func.apply(this, args), delay);
            };
        };

        if (searchInput) searchInput.addEventListener('input', debounce(() => this.loadBooks(), 500));
        if (categoryFilter) categoryFilter.addEventListener('change', () => this.loadBooks());
        if (statusFilter) statusFilter.addEventListener('change', () => this.loadBooks());

        // Add Book Button
        const addBookBtn = document.getElementById('btn-add-book');
        if (addBookBtn) {
            addBookBtn.addEventListener('click', () => {
                this.view.showAddModal();
            });
        }

        // Image Input Change
        if (this.view.bookImagesInput) {
            this.view.bookImagesInput.addEventListener('change', (e) => {
                const files = e.target.files;
                for (let i = 0; i < files.length; i++) {
                    this.view.selectedBookImages.push(files[i]);
                }
                this.view.renderBookImagePreviews();
            });
        }

        // Form Submit
        if (this.view.addBookForm) {
            this.view.addBookForm.addEventListener('submit', async (e) => {
                e.preventDefault();
                await this.handleFormSubmit();
            });
        }

        // Event Delegation for Edit and Delete buttons inside the table
        if (this.view.tbody) {
            this.view.tbody.addEventListener('click', (e) => {
                const editBtn = e.target.closest('.btn-edit-book');
                if (editBtn) {
                    this.view.showEditModal(editBtn.dataset);
                }

                const deleteBtn = e.target.closest('.btn-delete-item');
                if (deleteBtn && deleteBtn.dataset.url && deleteBtn.dataset.url.includes('/api/realbook/')) {
                    // We handle delete differently because there's a global delete modal
                    // that inventory.js handles, but for pure MVC we could handle it here.
                    // The generic delete logic in inventory.js might still be used for categories, 
                    // so we'll let it trigger or handle it specifically.
                }
            });
        }
    }

    async handleFormSubmit() {
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
        const reservedVal = parseInt(this.view.addBookForm.dataset.reserved || '0');
        const mode = this.view.addBookForm.dataset.mode || 'add';

        if (mode === 'edit' && stockVal < reservedVal) {
            alert(`Error: New stock quantity (${stockVal}) cannot be less than the reserved quantity (${reservedVal})!`);
            return;
        }

        const formData = new FormData(this.view.addBookForm);
        const bookId = this.view.addBookForm.dataset.bookId;

        this.view.setSavingState(true);
        try {
            await this.model.saveBook(bookId, formData, mode);
            this.view.hideModal();
            this.loadBooks();
            if (window.apiClient) {
                window.apiClient.showToast("Saved book details successfully!", "success");
            }
        } catch (error) {
            console.error("Save Book Error:", error);
            alert("Error: " + error.message);
        } finally {
            this.view.setSavingState(false);
        }
    }
}

window.BookController = BookController;

// Initialize the MVC architecture for Books
function initBookMVC() {
    const model = new BookModel();
    const view = new BookView();
    const controller = new BookController(model, view);
    
    // Store globally if needed for cross-component calls
    window.bookController = controller;
}
