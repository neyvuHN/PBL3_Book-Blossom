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
                        image: '/images/Book/book1.jpg',
                        isBlind: false // [UPDATED]
                    }
                ],
                totalPrice: 150000,
                shipReceiverName: 'Jane Doe',
                shipPhoneNumber: '0901234567',
                shipDetailAddress: '123 Nguyen Van Linh, Da Nang',
                paymentMethod: 'Cash on Delivery (COD)',
                note: 'Call me before delivery',
                subTotal: 150000,
                shippingFee: 0,
                discountAmount: 0,
                orderDate: '2026-05-29 10:00'
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
                        image: '/images/Book/book2.webp',
                        isBlind: false // [UPDATED]
                    }
                ],
                totalPrice: 240000,
                tracking: 'Preparing for shipment',
                shipReceiverName: 'John Smith',
                shipPhoneNumber: '0912345678',
                shipDetailAddress: '456 Le Loi, District 1, HCM',
                paymentMethod: 'Bank Transfer',
                note: 'Deliver during office hours',
                subTotal: 240000,
                shippingFee: 20000,
                discountAmount: 20000,
                orderDate: '2026-05-28 14:30',
                shippedDate: '2026-05-29 08:00',
                trackingMilestones: [
                    {
                        title: "Order Placed",
                        time: "Today, 14:30",
                        description: "",
                        status: "completed"
                    },
                    {
                        title: "Seller Shipped",
                        time: "Today, 14:30",
                        description: "",
                        status: "completed"
                    },
                    {
                        title: "Arrived at Central Hub - Da Nang",
                        time: "Today, 14:30",
                        description: "Package is being sorted for dispatch.",
                        status: "current"
                    },
                    {
                        title: "Out for Delivery",
                        time: "",
                        description: "",
                        status: "pending"
                    },
                    {
                        title: "Delivered",
                        time: "",
                        description: "",
                        status: "pending"
                    }
                ]
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
                        image: '/images/Book/book3.avif',
                        isBlind: false // [UPDATED]
                    }
                ],
                totalPrice: 200000,
                tracking: 'Out for delivery',
                shipReceiverName: 'Alice Green',
                shipPhoneNumber: '0923456789',
                shipDetailAddress: '789 Tran Hung Dao, Hoan Kiem, Hanoi',
                paymentMethod: 'Momo E-Wallet',
                note: 'Leave at the reception desk',
                subTotal: 200000,
                shippingFee: 15000,
                discountAmount: 15000,
                orderDate: '2026-05-27 09:15',
                shippedDate: '2026-05-28 10:00',
                deliveredDate: '2026-05-29 11:30',
                trackingMilestones: [
                    {
                        title: "Order Placed",
                        time: "2026-05-27 09:15",
                        description: "",
                        status: "completed"
                    },
                    {
                        title: "Seller Shipped",
                        time: "2026-05-28 10:00",
                        description: "",
                        status: "completed"
                    },
                    {
                        title: "Arrived at Central Hub - Da Nang",
                        time: "2026-05-29 08:00",
                        description: "",
                        status: "completed"
                    },
                    {
                        title: "Out for Delivery",
                        time: "2026-05-29 11:30",
                        description: "Package is out for delivery with shipper David.",
                        status: "current"
                    },
                    {
                        title: "Delivered",
                        time: "",
                        description: "",
                        status: "pending"
                    }
                ]
            },
            {
                id: 'ORD-12348',
                shopName: 'Blind Date Books', // [UPDATED] Shop changed for Blind Book
                status: 'to-receive',
                items: [
                    {
                        title: '#SpaceOpera #AI #FirstContact', // [UPDATED] Hashtags as blind key title
                        author: 'Hidden Author',
                        price: 250000,
                        quantity: 1,
                        image: '/images/BlindDateBook/BlindBook3.jpg', // [UPDATED]
                        isBlind: true // [UPDATED]
                    }
                ],
                totalPrice: 250000,
                tracking: 'Arrived at local facility',
                shipReceiverName: 'Bob Brown',
                shipPhoneNumber: '0934567890',
                shipDetailAddress: '101 Nguyen Hue, District 1, HCM',
                paymentMethod: 'Cash on Delivery (COD)',
                note: '',
                subTotal: 250000,
                shippingFee: 30000,
                discountAmount: 30000,
                orderDate: '2026-05-27 16:40',
                shippedDate: '2026-05-28 15:20',
                trackingMilestones: [
                    {
                        title: "Order Placed",
                        time: "2026-05-27 16:40",
                        description: "",
                        status: "completed"
                    },
                    {
                        title: "Seller Shipped",
                        time: "2026-05-28 15:20",
                        description: "",
                        status: "completed"
                    },
                    {
                        title: "Arrived at Central Hub - Da Nang",
                        time: "2026-05-29 09:00",
                        description: "",
                        status: "completed"
                    },
                    {
                        title: "Arrived at Local Facility",
                        time: "2026-05-29 14:00",
                        description: "Package has arrived at the delivery hub near you.",
                        status: "current"
                    },
                    {
                        title: "Out for Delivery",
                        time: "",
                        description: "",
                        status: "pending"
                    },
                    {
                        title: "Delivered",
                        time: "",
                        description: "",
                        status: "pending"
                    }
                ]
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
                        image: '/images/Book/book5.jpg',
                        isBlind: false // [UPDATED]
                    }
                ],
                totalPrice: 110000,
                isRated: false,
                shipReceiverName: 'Emma Wilson',
                shipPhoneNumber: '0945678901',
                shipDetailAddress: '202 Dien Bien Phu, Binh Thanh, HCM',
                paymentMethod: 'Momo E-Wallet',
                note: '',
                subTotal: 110000,
                shippingFee: 15000,
                discountAmount: 15000,
                orderDate: '2026-05-25 11:20',
                shippedDate: '2026-05-26 09:00',
                deliveredDate: '2026-05-27 14:00',
                completedDate: '2026-05-27 15:30'
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
                        image: '/images/Book/book6.webp',
                        isBlind: false // [UPDATED]
                    }
                ],
                totalPrice: 150000,
                isRated: true,
                shipReceiverName: 'James Watson',
                shipPhoneNumber: '0956789012',
                shipDetailAddress: '303 Bach Dang, Da Nang',
                paymentMethod: 'Bank Transfer',
                note: 'Please pack carefully',
                subTotal: 150000,
                shippingFee: 0,
                discountAmount: 0,
                orderDate: '2026-05-24 10:00',
                shippedDate: '2026-05-25 14:00',
                deliveredDate: '2026-05-26 10:00',
                completedDate: '2026-05-26 12:00'
            },
            {
                id: 'ORD-12351',
                shopName: 'Book Blossom',
                status: 'cancelled',
                items: [
                    {
                        title: 'Moby Dick',
                        author: 'Herman Melville',
                        price: 180000,
                        quantity: 1,
                        image: '/images/Book/book1.jpg',
                        isBlind: false // [UPDATED]
                    }
                ],
                totalPrice: 180000,
                shipReceiverName: 'David Miller',
                shipPhoneNumber: '0967890123',
                shipDetailAddress: '404 Le Duan, Da Nang',
                paymentMethod: 'Cash on Delivery (COD)',
                note: '',
                subTotal: 180000,
                shippingFee: 20000,
                discountAmount: 20000,
                orderDate: '2026-05-28 18:00',
                cancelReason: 'Changed mind'
            },
            {
                id: 'ORD-12352',
                shopName: 'Book Blossom',
                status: 'returned',
                items: [
                    {
                        title: 'The Alchemist',
                        author: 'Paulo Coelho',
                        price: 90000,
                        quantity: 2,
                        image: '/images/Book/book2.webp',
                        isBlind: false // [UPDATED]
                    }
                ],
                totalPrice: 180000,
                shipReceiverName: 'David Miller',
                shipPhoneNumber: '0967890123',
                shipDetailAddress: '404 Le Duan, Da Nang',
                paymentMethod: 'Bank Transfer',
                note: '',
                subTotal: 180000,
                shippingFee: 15000,
                discountAmount: 15000,
                orderDate: '2026-05-27 10:00',
                shippedDate: '2026-05-28 09:00',
                deliveredDate: '2026-05-29 14:00',
                cancelReason: 'Damaged book cover (Returned)'
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

    // [NEW] Submit Return/Refund request and update order status in local dummy model
    submitReturnRefund(orderId, requestData) {
        const order = this.orders.find(o => o.id === orderId);
        if (!order) return;

        order.status = 'returned';
        
        const proposalLabel = requestData.proposal === 'keep' 
            ? `Keep Item (Refund Request: ${requestData.refundAmount.toLocaleString('vi-VN')}đ)` 
            : 'Return & Refund Item';

        order.cancelReason = `${requestData.reason} - ${proposalLabel}`;
        
        // Add timeline record if trackingMilestones is defined
        if (order.trackingMilestones) {
            order.trackingMilestones.push({
                title: "Return/Refund Requested",
                time: new Date().toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', year: 'numeric', month: '2-digit', day: '2-digit' }),
                description: `Reason: ${requestData.reason}. Proposal: ${proposalLabel}.`,
                status: "completed"
            });
        }
    }
}
