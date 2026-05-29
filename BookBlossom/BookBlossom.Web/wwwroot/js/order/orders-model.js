class OrdersModel {
    constructor() {
        this.orders = this.getDummyData();
        this.currentTab = 'to-confirm';
        this.searchQuery = '';
    }

    getDummyData() {
        return [
            {
                id: 'ORD-12345',
                shopName: 'Book Blossom',
                status: 'to-confirm',
                items: [
                    {
                        title: 'The Great Gatsby',
                        author: 'F. Scott Fitzgerald',
                        price: 150000,
                        quantity: 1,
                        image: '/images/Book/book1.jpg'
                    }
                ],
                totalPrice: 150000
            },
            {
                id: 'ORD-12346',
                shopName: 'Book Blossom',
                status: 'to-ship',
                items: [
                    {
                        title: '1984',
                        author: 'George Orwell',
                        price: 120000,
                        quantity: 2,
                        image: '/images/Book/book2.webp'
                    }
                ],
                totalPrice: 240000,
                tracking: 'Preparing for shipment'
            },
            {
                id: 'ORD-12347',
                shopName: 'Book Blossom',
                status: 'to-receive',
                items: [
                    {
                        title: 'The Hobbit',
                        author: 'J.R.R. Tolkien',
                        price: 200000,
                        quantity: 1,
                        image: '/images/Book/book3.avif'
                    }
                ],
                totalPrice: 200000,
                tracking: 'Out for delivery'
            },
            {
                id: 'ORD-12348',
                shopName: 'Book Blossom',
                status: 'to-receive',
                items: [
                    {
                        title: 'Harry Potter',
                        author: 'J.K. Rowling',
                        price: 250000,
                        quantity: 1,
                        image: '/images/Book/book4.jpg'
                    }
                ],
                totalPrice: 250000,
                tracking: 'Arrived at local facility'
            },
            {
                id: 'ORD-12349',
                shopName: 'Book Blossom',
                status: 'completed',
                items: [
                    {
                        title: 'Pride and Prejudice',
                        author: 'Jane Austen',
                        price: 110000,
                        quantity: 1,
                        image: '/images/Book/book5.jpg'
                    }
                ],
                totalPrice: 110000,
                isRated: false
            },
            {
                id: 'ORD-12350',
                shopName: 'Book Blossom',
                status: 'completed',
                items: [
                    {
                        title: 'To Kill a Mockingbird',
                        author: 'Harper Lee',
                        price: 150000,
                        quantity: 1,
                        image: '/images/Book/book6.webp'
                    }
                ],
                totalPrice: 150000,
                isRated: true
            },
            {
                id: 'ORD-12351',
                shopName: 'Book Blossom',
                status: 'cancelled-return',
                items: [
                    {
                        title: 'Moby Dick',
                        author: 'Herman Melville',
                        price: 180000,
                        quantity: 1,
                        image: '/images/Book/book1.jpg'
                    }
                ],
                totalPrice: 180000,
                cancelReason: 'Changed mind'
            }
        ];
    }

    setTab(tab) {
        this.currentTab = tab;
    }

    setSearchQuery(query) {
        this.searchQuery = query.toLowerCase();
    }

    getFilteredOrders() {
        return this.orders.filter(order => {
            const matchesTab = order.status === this.currentTab;
            const matchesSearch = order.shopName.toLowerCase().includes(this.searchQuery) ||
                                  order.id.toLowerCase().includes(this.searchQuery) ||
                                  order.items.some(item => item.title.toLowerCase().includes(this.searchQuery));
            return matchesTab && matchesSearch;
        });
    }

    getToReceiveCount() {
        return this.orders.filter(order => order.status === 'to-receive').length;
    }
}
