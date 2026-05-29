class OrdersController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
    }

    init() {
        // Initial setup
        this.updateView();
        this.updateBadge();

        // Bind events
        this.view.bindTabChange(this.handleTabChange.bind(this));
        this.view.bindSearch(this.handleSearch.bind(this));
        this.view.bindViewDetails(this.handleViewDetails.bind(this));
        this.view.bindCancelOrder(this.handleCancelOrder.bind(this));
        this.view.bindOrderReceived(this.handleOrderReceived.bind(this));
    }

    handleViewDetails(orderId) {
        const order = this.model.orders.find(o => o.id === orderId);
        if (order) {
            this.view.showOrderDetailsModal(order);
        }
    }

    handleCancelOrder(orderId) {
        const order = this.model.orders.find(o => o.id === orderId);
        if (!order) return;

        const itemNames = order.items.map(i => i.title).join(', ');
        this.view.showConfirmModal({
            icon: 'fas fa-times-circle',
            iconColor: '#e53e3e',
            accentColor: 'linear-gradient(90deg, #e53e3e, #fc8181)',
            title: 'Cancel Order',
            message: `Are you sure you want to cancel order #${order.id}? (${itemNames})`,
            showReasonInput: true,
            confirmText: 'Yes, Cancel Order',
            confirmBtnClass: 'btn-danger',
            onConfirm: (reason) => {
                order.status = 'cancelled';
                order.cancelReason = reason;
                this.updateView();
                this.updateBadge();
            }
        });
    }

    handleOrderReceived(orderId) {
        const order = this.model.orders.find(o => o.id === orderId);
        if (!order) return;

        this.view.showConfirmModal({
            icon: 'fas fa-check-circle',
            iconColor: '#38a169',
            accentColor: 'linear-gradient(90deg, #38a169, #68d391)',
            title: 'Confirm Order Received',
            message: `Have you received your order #${order.id}? Please confirm only after the package is in your hands.`,
            confirmText: 'Yes, I\'ve Received It',
            confirmBtnClass: 'btn-success',
            onConfirm: () => {
                order.status = 'completed';
                order.completedDate = new Date().toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', year: 'numeric', month: '2-digit', day: '2-digit' });
                this.updateView();
                this.updateBadge();
            }
        });
    }

    handleTabChange(tab) {
        this.model.setTab(tab);
        this.updateView();
    }

    handleSearch(query) {
        this.model.setSearchQuery(query);
        this.updateView();
    }

    updateView() {
        const filteredOrders = this.model.getFilteredOrders();
        this.view.renderOrders(filteredOrders);
    }

    updateBadge() {
        const toReceiveCount = this.model.getToReceiveCount();
        this.view.updateToReceiveBadge(toReceiveCount);
    }
}
