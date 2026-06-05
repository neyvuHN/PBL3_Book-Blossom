(function (window) {
    class ExploreModel {
        constructor() {
            this.allBooks = [];
            this.categories = [];
        }

        async fetchCategories() {
            try {
                this.categories = await window.apiClient.apiGet('/api/category?status=Active');
                return this.categories;
            } catch (error) {
                console.error('Failed to load categories', error);
                return [];
            }
        }

        async fetchAllBooks() {
            try {
                // Fetch all books once for client-side filtering
                const url = '/api/realbook';
                this.allBooks = await window.apiClient.apiGet(url) || [];
                return this.allBooks;
            } catch (error) {
                console.error('Failed to load books', error);
                return [];
            }
        }

        getFilteredBooks(filters) {
            const {
                searchTerm,
                category,
                authors,
                genres,
                publishers,
                minPrice,
                maxPrice,
                fromYear,
                toYear
            } = filters;

            return this.allBooks.filter(book => {
                if (!book.isContinued) return false;

                // 1. Search Term Filter (Title, Author, Category)
                if (searchTerm) {
                    const term = searchTerm.toLowerCase();
                    const title = (book.title || '').toLowerCase();
                    const bookAuthors = (book.authors || book.Authors || '').toLowerCase();
                    const bookCat = (book.categoryName || '').toLowerCase();

                    if (!title.includes(term) && !bookAuthors.includes(term) && !bookCat.includes(term)) {
                        return false;
                    }
                }

                // 2. Category Filter (from top tags)
                if (category) {
                    const bookCat = (book.categoryName || '').toLowerCase();
                    if (bookCat !== category.toLowerCase()) return false;
                }

                // 3. Sidebar Author Filter
                if (authors && authors.length > 0) {
                    const bookAuthorsList = (book.authors || book.Authors || 'Unknown Author').split(/,+/).map(a => a.trim().toLowerCase());
                    const hasAuthorMatch = authors.some(auth => bookAuthorsList.includes(auth.toLowerCase()));
                    if (!hasAuthorMatch) return false;
                }

                // 4. Sidebar Genre Filter
                if (genres && genres.length > 0) {
                    const bookGenre = (book.categoryName || '').trim().toLowerCase();
                    const hasGenreMatch = genres.some(g => bookGenre === g.toLowerCase());
                    if (!hasGenreMatch) return false;
                }

                // 5. Sidebar Publisher Filter
                if (publishers && publishers.length > 0) {
                    const bookPublisher = (book.publisher || book.Publisher || 'Unknown Publisher').trim().toLowerCase();
                    const hasPublisherMatch = publishers.some(p => bookPublisher === p.toLowerCase());
                    if (!hasPublisherMatch) return false;
                }

                // 6. Price Filter
                const bookPrice = Number(book.price || book.Price || 0);
                if (bookPrice < minPrice || bookPrice > maxPrice) return false;

                // 7. Publish Year Filter
                const bookYear = parseInt(book.publishYear || book.PublishYear, 10);
                if (bookYear) {
                    if (fromYear && bookYear < fromYear) return false;
                    if (toYear && bookYear > toYear) return false;
                } else {
                    if (fromYear || toYear) return false;
                }

                return true;
            });
        }
    }

    window.ExploreModel = ExploreModel;
})(window);
