class BookModel {
    constructor() {
        this.apiClient = window.apiClient;
    }

    async getBooks(searchTerm = '', category = '', statusFilter = '') {
        const url = `/api/realbook?searchTerm=${encodeURIComponent(searchTerm)}&category=${encodeURIComponent(category)}&includeDiscontinued=true`;
        let books = await this.apiClient.apiGet(url);

        if (statusFilter) {
            books = books.filter(book => {
                if (statusFilter === 'Discontinued') return !book.isContinued;
                if (statusFilter === 'Out of Stock') return book.isContinued && book.unitsInStock === 0;
                if (statusFilter === 'Low Stock') return book.isContinued && book.unitsInStock > 0 && book.unitsInStock < 15;
                if (statusFilter === 'In Stock') return book.isContinued && book.unitsInStock >= 15;
                return true;
            });
        }
        
        books.sort((a, b) => a.bookID - b.bookID);
        return books;
    }

    async saveBook(bookId, formData, mode) {
        const url = mode === 'edit' ? `/api/realbook/${bookId}` : '/api/realbook';
        const method = mode === 'edit' ? 'PUT' : 'POST';
        return await this.apiClient.apiUpload(url, formData, method);
    }

    async toggleBookStatus(bookId) {
        const url = `/api/realbook/${bookId}`;
        return await this.apiClient.apiDelete(url);
    }
}

window.BookModel = BookModel;
