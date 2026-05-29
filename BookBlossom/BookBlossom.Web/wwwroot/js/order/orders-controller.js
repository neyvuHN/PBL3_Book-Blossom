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
        this.view.bindTrackOrder(this.handleTrackOrder.bind(this));
        // [UPDATED] Bind Buy Again to open Secure Checkout popup
        this.view.bindBuyAgain(this.handleBuyAgain.bind(this));
        // [UPDATED] Bind clicking on book items to navigate to book details / blind book details
        this.view.bindViewBook(this.handleViewBook.bind(this));
    }

    handleViewDetails(orderId) {
        const order = this.model.orders.find(o => o.id === orderId);
        if (order) {
            this.view.showOrderDetailsModal(order);
        }
    }

    handleTrackOrder(orderId) {
        const order = this.model.orders.find(o => o.id === orderId);
        if (order) {
            this.view.showOrderTrackingModal(order);
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

    // [UPDATED] handleBuyAgain – builds a checkout payload from the existing order
    // and opens the shared Secure Checkout modal (same flow as Buy Now on product pages)
    handleBuyAgain(orderId) {
        const order = this.model.orders.find(o => o.id === orderId);
        if (!order) return;

        if (typeof window.openCheckout !== 'function') {
            console.warn('[OrdersController] Secure Checkout modal is not loaded on this page.');
            return;
        }

        // Map order items to the shape expected by populateCheckoutBookInfo()
        const checkoutItems = order.items.map(item => ({
            id:       'buy-again-' + orderId + '-' + Date.now(),
            title:    item.title,
            author:   item.author || 'BookBlossom',
            img:      item.image  || '/images/Book/book1.jpg',
            qty:      item.quantity,
            priceVnd: item.price,
            price:    item.price / 20000,
            isBlind:  item.isBlind || false
        }));

        // Build a fresh checkoutState from the original order totals
        window.checkoutState = {
            isCart:      false,
            isBuyNow:    true,
            isBlind:     false,
            subtotal:    order.subTotal  || order.totalPrice,
            shippingFee: order.shippingFee || 0,
            discount:    order.discountAmount || 0,
            orderNote:   ''
        };

        // Populate book info block inside the modal
        if (typeof window.populateCheckoutBookInfo === 'function') {
            window.populateCheckoutBookInfo(checkoutItems);
        }

        // Recalculate and render the order summary totals
        if (typeof window.updateCheckoutTotals === 'function') {
            window.updateCheckoutTotals();
        }

        // Open the Secure Checkout popup
        window.openCheckout();
    }

    // [UPDATED] handleViewBook – redirects to either Explore (normal book) or BlindDate page with hash key
    handleViewBook(title, isBlind) {
        if (isBlind) {
            window.location.href = `/BlindDate#blind-details-${encodeURIComponent(title)}`;
        } else {
            window.location.href = `/Explore#book-details-${encodeURIComponent(title)}`;
        }
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
