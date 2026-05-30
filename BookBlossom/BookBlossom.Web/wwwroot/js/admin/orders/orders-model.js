/**
 * Frontend MVC - Model
 * Manages the state, filtering, and data modifications of the admin order management console.
 */
class OrdersModel {
    constructor(initialData) {
        this.orders = initialData.orders || [];
        this.returnedItems = initialData.returnedItems || [];
        this.complaints = initialData.complaints || [];
        
        // Active Filter States
        this.activeTab = 'all'; // 'all', 'pending', 'toship', 'intransit', 'completed', 'returns', 'complaints'
        this.searchQuery = '';
        this.filterType = ''; // '', 'standard', 'blind-date'
        this.filterKpi = ''; // '', 'high' (<24h), 'warning' (<48h)
        
        // Batch Selection State
        this.selectedOrderIds = new Set();
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
     * Confirm a single pending order (KPI critical action within 48h).
     * Transitions status: "Pending Confirmation" -> "To Ship" (SubStatus: "Packing")
     */
    confirmOrder(orderId) {
        const order = this.findOrderById(orderId);
        if (!order || order.status !== 'Pending Confirmation') return false;

        order.status = 'To Ship';
        order.subStatus = 'Packing';
        order.remainingTimeText = '';
        order.remainingHours = 0;
        
        // Remove from batch selection if present
        this.selectedOrderIds.delete(orderId);
        return true;
    }

    /**
     * Cancel an order. Sets status to Refund/Dispute.
     */
    cancelOrder(orderId) {
        const order = this.findOrderById(orderId);
        if (!order || order.status !== 'Pending Confirmation') return false;

        order.status = 'Refund/Dispute';
        order.subStatus = 'Cancelled';
        order.remainingTimeText = '';
        order.remainingHours = 0;
        this.selectedOrderIds.delete(orderId);
        return true;
    }

    /**
     * Start shipping an order.
     * Transitions status: "To Ship" -> "In Transit" (SubStatus: "In Transit")
     */
    startShippingOrder(orderId) {
        const order = this.findOrderById(orderId);
        if (!order || order.status !== 'To Ship') return false;

        order.status = 'In Transit';
        order.subStatus = 'In Transit';
        this.selectedOrderIds.delete(orderId);
        return true;
    }

    /**
     * Simulates changing the mock logistic sub-status of an order in transit.
     * Allowed statuses: Packing, Handed to carrier, In transit, Delivering, Delivered.
     */
    updateMockStatus(orderId, newSubStatus) {
        const order = this.findOrderById(orderId);
        if (!order) return false;

        order.subStatus = newSubStatus;

        if (newSubStatus === 'Delivered') {
            order.status = 'Completed';
        } else if (newSubStatus === 'Packing' || newSubStatus === 'Handed to carrier') {
            order.status = 'To Ship';
        } else {
            order.status = 'In Transit';
        }
        return true;
    }

    /**
     * Performs a batch confirmation for selected orders.
     */
    batchConfirmOrders(orderIds) {
        let count = 0;
        orderIds.forEach(id => {
            if (this.confirmOrder(id)) count++;
        });
        return count;
    }

    /**
     * Performs a batch shipping start for selected orders.
     */
    batchStartShippingOrders(orderIds) {
        let count = 0;
        orderIds.forEach(id => {
            if (this.startShippingOrder(id)) count++;
        });
        return count;
    }

    /**
     * Simulates batch status updates for selected orders.
     */
    batchUpdateMockStatus(orderIds, newSubStatus) {
        let count = 0;
        orderIds.forEach(id => {
            if (this.updateMockStatus(id, newSubStatus)) count++;
        });
        return count;
    }

    /**
     * Restock returned items to warehouse inventory (Logistics handling).
     */
    restockItem(itemId) {
        const item = this.findReturnedItemById(itemId);
        if (!item || item.restockStatus !== 'Pending Restock') return false;

        item.restockStatus = 'Restocked';
        return true;
    }

    /**
     * Marks an escalated complaint or bad review as resolved after contact.
     */
    resolveComplaint(complaintId) {
        const complaint = this.findComplaintById(complaintId);
        if (!complaint || complaint.status !== 'Pending Support') return false;

        complaint.status = 'Resolved';
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
