class OrdersModel {
    constructor() {
        this.orders = [];
        this.currentTab = 'to-confirm';
        this.searchQuery = '';
    }

    async fetchOrders() {
        try {
            const response = await fetch('/api/Order/customer/my-orders', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.getToken()}`
                }
            });
            if (response.ok) {
                const data = await response.json();
                this.orders = this.mapBackendToFrontend(data);
            } else {
                console.error("Failed to fetch orders:", response.status);
                this.orders = [];
            }
        } catch (error) {
            console.error("Error fetching orders:", error);
            this.orders = [];
        }
    }

    async fetchOrderDetails(orderId) {
        try {
            const response = await fetch(`/api/Order/customer/my-orders/${orderId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.getToken()}`
                }
            });
            if (response.ok) {
                return await response.json();
            }
        } catch (error) {
            console.error("Error fetching order details:", error);
        }
        return null;
    }

    async cancelOrderApi(orderId, reason) {
        try {
            const response = await fetch(`/api/Order/customer/my-orders/${orderId}/cancel`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.getToken()}`
                },
                body: JSON.stringify({ reason })
            });
            return response.ok;
        } catch (error) {
            console.error("Error cancelling order:", error);
            return false;
        }
    }

    async confirmReceivedApi(orderId) {
        try {
            const response = await fetch(`/api/Order/customer/my-orders/${orderId}/confirm-received`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.getToken()}`
                }
            });
            return response.ok;
        } catch (error) {
            console.error("Error confirming order:", error);
            return false;
        }
    }

    getToken() {
        return localStorage.getItem('token') || sessionStorage.getItem('token') || '';
    }

    mapBackendToFrontend(data) {
        return data.map(o => {
            const statusMap = {
                0: 'to-confirm', // Pending
                1: 'to-ship', // AwaitingPickup
                2: 'to-ship', // Shipping
                3: 'to-receive', // Delivering
                4: 'completed', // Completed
                5: 'cancelled', // Cancelled
                6: 'returned' // Returning
            };

            const items = o.orderItems ? o.orderItems.map(i => {
                const isBlind = i.blindBookID != null;
                const completed = o.orderStatus === 4; // 4: Completed
                
                let realBook = null;
                if (isBlind && completed && i.realBookTitle) {
                    realBook = {
                        title: i.realBookTitle,
                        author: i.publisher || 'BookBlossom Edition',
                        image: i.sampleFilePath || '/images/placeholder.jpg',
                        description: `Cuốn sách tuyệt vời ẩn sau gói Sách Mù thuộc thể loại "${i.title.replace("[Sách Mù] ", "")}". Chúc bạn có những giờ phút đọc sách thật thú vị!`
                    };
                }
                
                return {
                    id: i.bookID || i.blindBookID,
                    title: i.title,
                    author: i.publisher || 'N/A', // Simple fallback
                    price: i.unitPrice,
                    quantity: i.quantity,
                    image: i.sampleFilePath || (isBlind ? '/images/BlindDateBook/BlindBook1.jpg' : '/images/placeholder.jpg'),
                    isBlind: isBlind,
                    realBook: realBook
                };
            }) : [];

            return {
                id: o.orderID,
                shopName: 'Book Blossom', // Constant for now
                status: statusMap[o.orderStatus] || 'to-confirm',
                items: items,
                totalPrice: o.totalAmount,
                shipReceiverName: o.shipReceiverName,
                shipPhoneNumber: o.shipPhoneNumber,
                shipDetailAddress: o.note || '',
                paymentMethod: this.mapPaymentMethod(o.paymentMethod),
                paymentStatus: o.paymentStatus,
                note: o.note,
                subTotal: o.totalAmount, // This is an approximation since OrderListItemDTO doesn't have subtotal, we can rely on detail API later
                shippingFee: 0,
                discountAmount: 0,
                orderDate: new Date(o.orderDate).toLocaleString('vi-VN'),
                cancelReason: o.cancelReason || ''
            };
        });
    }

    mapPaymentMethod(method) {
        switch (method) {
            case 0: return 'Cash on Delivery (COD)';
            case 1: return 'Momo E-Wallet';
            case 2: return 'VNPay';
            default: return 'Khác';
        }
    }

    setTab(tab) {
        this.currentTab = tab;
    }

    setSearchQuery(query) {
        this.searchQuery = query.toLowerCase();
    }

    getFilteredOrders() {
        try {
            return this.orders.filter(order => {
                const matchesTab = order.status === this.currentTab;
                const shopNameSearch = order.shopName ? order.shopName.toLowerCase() : '';
                const titleMatch = order.items && order.items.some(item => item.title && item.title.toLowerCase().includes(this.searchQuery));
                
                const matchesSearch = shopNameSearch.includes(this.searchQuery) ||
                    (order.id != null ? order.id.toString().toLowerCase().includes(this.searchQuery) : false) ||
                    titleMatch;
                return matchesTab && matchesSearch;
            });
        } catch (e) {
            console.error("Error filtering orders:", e);
            return [];
        }
    }

    getToReceiveCount() {
        return this.orders.filter(order => order.status === 'to-receive').length;
    }

    submitReturnRefund(orderId, requestData) {
        const order = this.orders.find(o => o.id.toString() === orderId.toString());
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
