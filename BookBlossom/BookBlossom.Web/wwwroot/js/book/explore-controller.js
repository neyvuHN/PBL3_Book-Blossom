(function (window, $) {
    if (!$) {
        console.error('explore-controller.js requires jQuery.');
        return;
    }

    class ExploreController {
        constructor(model, view) {
            this.model = model;
            this.view = view;
            
            this.currentSearchTerm = '';
            this.currentCategory = '';
        }

        async init() {
            this.view.init();
            
            this.bindEvents();

            const categories = await this.model.fetchCategories();
            this.view.renderCategoryTags(categories);

            const books = await this.model.fetchAllBooks();
            this.view.generateDynamicFilters(books);
            this.updateView();
        }

        bindEvents() {
            this.view.bindCategoryClick(this.handleCategoryClick.bind(this));
            this.view.bindSearchInput(this.handleSearchInput.bind(this));
            this.view.bindApplyFilters(this.handleApplyFilters.bind(this));
            this.view.bindClearFilters(this.handleClearFilters.bind(this));
            this.view.bindBookClick(this.handleBookClick.bind(this));
        }

        handleCategoryClick(categoryName) {
            this.currentCategory = categoryName === 'All' ? '' : categoryName;
            this.updateView();
        }

        handleSearchInput(searchTerm) {
            this.currentSearchTerm = (searchTerm || '').trim();
            this.updateView();
        }

        handleApplyFilters() {
            this.updateView();
        }

        handleClearFilters() {
            this.updateView();
        }

        handleBookClick(bookId) {
            if (window.BookBlossomProductDetails && bookId) {
                window.BookBlossomProductDetails.showProductById(bookId);
            }
        }

        updateView() {
            const filterValues = this.view.getFilterValues();
            const allFilters = {
                ...filterValues,
                searchTerm: this.currentSearchTerm,
                category: this.currentCategory
            };

            const filteredBooks = this.model.getFilteredBooks(allFilters);
            this.view.renderBooks(filteredBooks);
        }
    }

    $(document).ready(function () {
        const model = new window.ExploreModel();
        const view = new window.ExploreView();
        const controller = new ExploreController(model, view);
        
        controller.init();
    });
})(window, window.jQuery);
