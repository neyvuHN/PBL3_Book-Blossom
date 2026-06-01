/**
 * Frontend MVC - Model
 * Manages the state, filtering, and data modifications of the admin order management console.
 * Connected to live Backend APIs.
 */
class OrdersModel {
    constructor() {
        this.orders = [];
        this.returnedItems = [];
        this.complaints = [];
        
        // Active Filter States
        this.activeTab = 'all'; // 'all', 'pending', 'toship', 'intransit', 'completed', 'returns', 'complaints'
        this.searchQuery = '';
        this.filterType = ''; // '', 'standard', 'blind-date'
        this.filterKpi = ''; // '', 'high' (<24h), 'warning' (<48h)
        
        // Batch Selection State
        this.selectedOrderIds = new Set();
    }

    /**
     * Helper to get common fetch headers containing the Bearer JWT token.
     */
    _getHeaders() {
        const token = localStorage.getItem('accessToken');
        const headers = {
            'Content-Type': 'application/json'
        };
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }
        return headers;
    }

    /**
     * Map backend OrderStatus (enum 0-6) to frontend user-friendly strings.
     */
    _mapBackendStatus(orderStatus) {
        switch (orderStatus) {
            case 0: return { status: 'Pending Confirmation', subStatus: '' };
            case 1: return { status: 'To Ship', subStatus: 'Packing' };
            case 2: return { status: 'In Transit', subStatus: 'In Transit' };
            case 3: return { status: 'In Transit', subStatus: 'Delivering' };
            case 4: return { status: 'Completed', subStatus: 'Delivered' };
            case 5: return { status: 'Refund/Dispute', subStatus: 'Cancelled' };
            case 6: return { status: 'Refund/Dispute', subStatus: 'Returning' };
            default: return { status: 'Pending Confirmation', subStatus: '' };
        }
    }

    /**
     * Map frontend friendly logistic sub-status back to backend OrderStatus enum.
     */
    _mapSubStatusToEnum(subStatus) {
        switch (subStatus) {
            case 'Packing': return 1; // AwaitingPickup
            case 'Handed to carrier': return 2; // Shipping
            case 'In Transit': return 2; // Shipping
            case 'Delivering': return 3; // Delivering
            case 'Delivered': return 4; // Completed
            default: return 1;
        }
    }

    /**
     * Fetches live order list and detailed records from backend APIs.
     */
    async fetchAllDataFromApi() {
        const headers = this._getHeaders();

        try {
            // 1. Fetch main list of all orders
            const ordersResponse = await fetch('/api/order/store', {
                method: 'GET',
                headers: headers
            });

            if (!ordersResponse.ok) {
                if (ordersResponse.status === 401 || ordersResponse.status === 403) {
                    throw new Error("Unauthorized! Please sign in as Admin.");
                }
                throw new Error(`Failed to load orders: ${ordersResponse.statusText}`);
            }

            const ordersList = await ordersResponse.json() || [];

            // 2. Load detail song song for all orders to populate items, cover images, customer notes, etc.
            const detailsList = await Promise.all(ordersList.map(async (o) => {
                try {
                    const detailRes = await fetch(`/api/order/store/${o.orderID}`, {
                        method: 'GET',
                        headers: headers
                    });
                    if (detailRes.ok) {
                        return await detailRes.json();
                    }
                } catch (e) {
                    console.error(`Error loading detail for order #${o.orderID}:`, e);
                }
                return null;
            }));

            // 3. Map backend details into frontend structures
            this.orders = detailsList.filter(d => d !== null).map(detail => {
                const mapped = this._mapBackendStatus(detail.orderStatus);
                const isBlindDate = detail.orderItems && detail.orderItems.some(item => item.blindBookID !== null);
                
                // Calculate SLA remaining hours (48h confirm limit)
                const orderDate = new Date(detail.orderDate);
                const elapsedMs = new Date() - orderDate;
                const remainingHours = Math.max(0, 48 - (elapsedMs / (1000 * 60 * 60)));
                let remainingTimeText = "";
                if (remainingHours > 0) {
                    const h = Math.floor(remainingHours);
                    const m = Math.floor((remainingHours - h) * 60);
                    remainingTimeText = `${h}h ${m}m`;
                } else if (detail.orderStatus === 0) {
                    remainingTimeText = "SLA Expired";
                }

                // Gather items info
                let bookTitle = "Unknown book";
                let quantity = 1;
                let isbn = "N/A";
                let blindDateHiddenTitle = "";
                let genre = "";
                let ImagePreviewUrl = "/images/Book/book1.jpg";

                if (detail.orderItems && detail.orderItems.length > 0) {
                    const firstItem = detail.orderItems[0];
                    bookTitle = firstItem.title;
                    quantity = firstItem.quantity;
                    isbn = firstItem.isbn || "N/A";
                    
                    if (isBlindDate) {
                        blindDateHiddenTitle = firstItem.realBookTitle || firstItem.title;
                        genre = "#Romance #Mystery"; // Fallback genres for visual styles
                        ImagePreviewUrl = "/images/BlindDateBook/BlindBook.jpg";
                    } else {
                        // Fallback sample file or preview
                        ImagePreviewUrl = firstItem.sampleFilePath || "/images/Book/book1.jpg";
                    }
                }

                return {
                    id: detail.orderID.toString(),
                    orderIDRaw: detail.orderID,
                    buyerName: detail.customerName || "Anonymous Customer",
                    buyerAvatarUrl: `https://i.pravatar.cc/150?img=${(detail.customerID % 70) + 1}`,
                    customerNote: detail.note || "",
                    totalAmount: detail.totalAmount,
                    fundsStatus: detail.paymentStatus === 1 ? "Released to Shop" : "Held in Escrow",
                    status: mapped.status,
                    subStatus: mapped.subStatus,
                    createdAt: new Date(detail.orderDate).toLocaleString('en-US', { hour12: false }),
                    remainingHours: remainingHours,
                    remainingTimeText: remainingTimeText,
                    isBlindDate: isBlindDate,
                    blindDateHiddenTitle: blindDateHiddenTitle,
                    blindDateNote: "This is a Blind Date order. DO NOT write the title on the external packaging!",
                    genre: genre,
                    bookTitle: bookTitle,
                    quantity: quantity,
                    isbn: isbn,
                    ImagePreviewUrl: ImagePreviewUrl,
                    fullDetail: detail // Keep reference if needed
                };
            });

            // 4. Fetch live Return & Refund requests from backend
            const returnResponse = await fetch('/api/return/staff', {
                method: 'GET',
                headers: headers
            });

            if (returnResponse.ok) {
                const returnRequests = await returnResponse.json() || [];

                // Map Return Requests with ReturnAndRefund (resolutionType === 1) to returns
                const returnedItemsList = returnRequests.filter(r => r.resolutionType === 1);
                this.returnedItems = returnedItemsList.map(r => {
                    let restockStatus = "Pending Restock";
                    if (r.returnStatus === 1) restockStatus = "Restocked";
                    if (r.returnStatus === 2) restockStatus = "Rejected";

                    return {
                        id: r.returnRequestID.toString(),
                        orderId: r.orderID.toString(),
                        bookTitle: r.bookTitle || "Unknown book",
                        quantity: r.returnQuantity,
                        refundAmount: r.refundAmount || 0,
                        moderatorDecision: "Approve Return & Refund",
                        returnReason: r.returnReason || "No reason",
                        restockStatus: restockStatus,
                        transferredDate: new Date(r.requestDate).toLocaleDateString('en-US'),
                        unboxVideoPath: r.unboxVideoPath,
                        raw: r
                    };
                });

                // Map Return Requests with RefundOnly (resolutionType === 0) to complaints
                const refundOnlyList = returnRequests.filter(r => r.resolutionType === 0);
                this.complaints = refundOnlyList.map(r => {
                    let status = "Pending Support";
                    if (r.returnStatus === 1) status = "Resolved";
                    if (r.returnStatus === 2) status = "Resolved (Rejected)";

                    return {
                        id: r.returnRequestID.toString(),
                        orderId: r.orderID.toString(),
                        buyerName: r.customerName || "Customer",
                        contactEmail: r.customerPhoneNumber || "support@bookblossom.com",
                        type: "Refund Request",
                        rating: 1, // Negative review simulator
                        content: r.returnReason || "Customer requested direct refund.",
                        moderatorNote: `Customer requested Refund Only. Video proof: ${r.unboxVideoPath || 'None'}.`,
                        status: status,
                        transferredDate: new Date(r.requestDate).toLocaleDateString('en-US'),
                        raw: r
                    };
                });
            }

            return true;
        } catch (error) {
            console.error("Error fetching admin orders data:", error);
            throw error;
        }
    }

    /**
     * Finds an order object by ID.
     */
    findOrderById(orderId) {
        return this.orders.find(o => o.id === orderId) || null;
    }

    /**
     * Finds a returned item by ID.
     */
    findReturnedItemById(itemId) {
        return this.returnedItems.find(item => item.id === itemId) || null;
    }

    /**
     * Finds an escalated complaint by ID.
     */
    findComplaintById(complaintId) {
        return this.complaints.find(c => c.id === complaintId) || null;
    }

    /**
     * Filters the orders list based on current tab, search term, type, and KPI criteria.
     */
    getFilteredOrders() {
        return this.orders.filter(order => {
            // 1. Tab Status Filter
            if (this.activeTab !== 'all') {
                if (this.activeTab === 'pending' && order.status !== 'Pending Confirmation') return false;
                if (this.activeTab === 'toship' && order.status !== 'To Ship') return false;
                if (this.activeTab === 'intransit' && order.status !== 'In Transit') return false;
                if (this.activeTab === 'completed' && order.status !== 'Completed') return false;
            }

            // 2. Search Query (Order ID, Book Title, Buyer Name)
            if (this.searchQuery) {
                const query = this.searchQuery.toLowerCase();
                const matchesId = order.id && order.id.toLowerCase().includes(query);
                const matchesBook = order.bookTitle && order.bookTitle.toLowerCase().includes(query);
                const matchesBuyer = order.buyerName && order.buyerName.toLowerCase().includes(query);
                
                if (!matchesId && !matchesBook && !matchesBuyer) {
                    return false;
                }
            }

            // 3. Order Type Filter (Standard vs Blind Date)
            if (this.filterType) {
                if (this.filterType === 'standard' && order.isBlindDate) return false;
                if (this.filterType === 'blind-date' && !order.isBlindDate) return false;
            }

            // 4. KPI Penalty Risk Filter (applicable to Pending Confirmation orders)
            if (this.filterKpi) {
                if (order.status !== 'Pending Confirmation') return false;
                if (this.filterKpi === 'high' && order.remainingHours > 24) return false;
                if (this.filterKpi === 'warning' && order.remainingHours > 48) return false;
            }

            return true;
        });
    }

    /**
     * Returns filtered list of returned items.
     */
    getFilteredReturns() {
        if (!this.searchQuery) return this.returnedItems;
        const query = this.searchQuery.toLowerCase();
        return this.returnedItems.filter(item => 
            (item.id && item.id.toLowerCase().includes(query)) ||
            (item.orderId && item.orderId.toLowerCase().includes(query)) ||
            (item.bookTitle && item.bookTitle.toLowerCase().includes(query)) ||
            (item.returnReason && item.returnReason.toLowerCase().includes(query))
        );
    }

    /**
     * Returns filtered list of escalated complaints.
     */
    getFilteredComplaints() {
        if (!this.searchQuery) return this.complaints;
        const query = this.searchQuery.toLowerCase();
        return this.complaints.filter(c => 
            (c.id && c.id.toLowerCase().includes(query)) ||
            (c.orderId && c.orderId.toLowerCase().includes(query)) ||
            (c.buyerName && c.buyerName.toLowerCase().includes(query)) ||
            (c.content && c.content.toLowerCase().includes(query)) ||
            (c.type && c.type.toLowerCase().includes(query))
        );
    }

    /**
     * Confirm a single pending order (Transitions: Pending -> AwaitingPickup = 1)
     */
    async confirmOrder(orderId) {
        const order = this.findOrderById(orderId);
        if (!order) return false;

        const headers = this._getHeaders();
        const payload = {
            orderIds: [order.orderIDRaw]
        };

        const response = await fetch('/api/order/store/confirm', {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.message || "Failed to confirm order.");
        }

        // Clean from batch selection
        this.selectedOrderIds.delete(orderId);
        return true;
    }

    /**
     * Cancel an order (Transitions: -> Cancelled = 5)
     */
    async cancelOrder(orderId) {
        const order = this.findOrderById(orderId);
        if (!order) return false;

        const headers = this._getHeaders();
        const payload = {
            orderIds: [order.orderIDRaw],
            status: 5 // Cancelled = 5
        };

        const response = await fetch('/api/order/store/status', {
            method: 'PUT',
            headers: headers,
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.message || "Failed to cancel order.");
        }

        this.selectedOrderIds.delete(orderId);
        return true;
    }

    /**
     * Start shipping an order (Transitions: To Ship -> In Transit/Shipping = 2)
     */
    async startShippingOrder(orderId) {
        const order = this.findOrderById(orderId);
        if (!order) return false;

        const headers = this._getHeaders();
        const payload = {
            orderIds: [order.orderIDRaw],
            status: 2 // Shipping = 2
        };

        const response = await fetch('/api/order/store/status', {
            method: 'PUT',
            headers: headers,
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.message || "Failed to start shipping.");
        }

        this.selectedOrderIds.delete(orderId);
        return true;
    }

    /**
     * Updates the database logistic status of an order.
     */
    async updateOrderStatusInDb(orderId, newSubStatus) {
        const order = this.findOrderById(orderId);
        if (!order) return false;

        const targetStatusEnum = this._mapSubStatusToEnum(newSubStatus);
        const headers = this._getHeaders();
        const payload = {
            orderIds: [order.orderIDRaw],
            status: targetStatusEnum
        };

        const response = await fetch('/api/order/store/status', {
            method: 'PUT',
            headers: headers,
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.message || "Failed to update order status.");
        }

        return true;
    }

    /**
     * Performs a batch confirmation for selected orders.
     */
    async batchConfirmOrders(orderIds) {
        const rawIds = orderIds.map(id => {
            const o = this.findOrderById(id);
            return o ? o.orderIDRaw : null;
        }).filter(id => id !== null);

        if (rawIds.length === 0) return 0;

        const headers = this._getHeaders();
        const payload = {
            orderIds: rawIds
        };

        const response = await fetch('/api/order/store/confirm', {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.message || "Batch confirmation failed.");
        }

        this.selectedOrderIds.clear();
        return rawIds.length;
    }

    /**
     * Performs a batch shipping start for selected orders.
     */
    async batchStartShippingOrders(orderIds) {
        return await this.batchUpdateOrderStatusInDb(orderIds, 'In Transit');
    }

    /**
     * Performs batch status updates for selected orders in DB.
     */
    async batchUpdateOrderStatusInDb(orderIds, newSubStatus) {
        const rawIds = orderIds.map(id => {
            const o = this.findOrderById(id);
            return o ? o.orderIDRaw : null;
        }).filter(id => id !== null);

        if (rawIds.length === 0) return 0;

        const targetStatusEnum = this._mapSubStatusToEnum(newSubStatus);
        const headers = this._getHeaders();
        const payload = {
            orderIds: rawIds,
            status: targetStatusEnum
        };

        const response = await fetch('/api/order/store/status', {
            method: 'PUT',
            headers: headers,
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.message || "Batch status update failed.");
        }

        this.selectedOrderIds.clear();
        return rawIds.length;
    }

    /**
     * Review return/refund request (Approve/Reject) on backend.
     */
    async reviewReturnRequest(requestId, isApproved, rejectReason = "") {
        const headers = this._getHeaders();
        const payload = {
            isApproved: isApproved,
            rejectReason: isApproved ? null : rejectReason
        };

        const response = await fetch(`/api/return/staff/${requestId}/review`, {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const err = await response.json();
            throw new Error(err.message || "Failed to submit return request review.");
        }

        return true;
    }

    /**
     * Toggles the selection status of an order for batch processing.
     */
    toggleOrderSelection(orderId) {
        if (this.selectedOrderIds.has(orderId)) {
            this.selectedOrderIds.delete(orderId);
        } else {
            this.selectedOrderIds.add(orderId);
        }
    }

    /**
     * Checks if all orders of a specific tab are selected.
     */
    isAllSelectedForTab(tabOrders) {
        if (tabOrders.length === 0) return false;
        return tabOrders.every(order => this.selectedOrderIds.has(order.id));
    }

    /**
     * Selects or deselects all orders of a specific tab.
     */
    setAllSelectedForTab(tabOrders, selected) {
        tabOrders.forEach(order => {
            if (selected) {
                this.selectedOrderIds.add(order.id);
            } else {
                this.selectedOrderIds.delete(order.id);
            }
        });
    }

    /**
     * Clears all order selections.
     */
    clearOrderSelection() {
        this.selectedOrderIds.clear();
    }
}
