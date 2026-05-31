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
        // [NEW] Bind Rate and View Review
        this.view.bindRateOrder(this.handleRateOrder.bind(this));
        this.view.bindViewReview(this.handleViewReview.bind(this));
        // [NEW] Bind Reveal Real Book
        this.view.bindRevealRealBook(this.handleRevealRealBook.bind(this));
        // [UPDATED] Bind clicking on book items to navigate to book details / blind book details
        this.view.bindViewBook(this.handleViewBook.bind(this));
        // [NEW] Bind Return/Refund click and submit handlers
        this.view.bindReturnRefundClick(this.handleReturnRefundClick.bind(this));
        this.view.bindReturnRefundSubmit(this.handleReturnRefundSubmit.bind(this));
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

    // [NEW] Handle clicking Rate
    handleRateOrder(orderId) {
        const order = this.model.orders.find(o => o.id === orderId);
        if (order && !order.isRated) {
            this.view.showRateOrderModal(order, (id, rating, reviewText) => {
                order.isRated = true;
                order.userRating = rating;
                order.userReviewText = reviewText;
                
                this.updateView();

                // [NEW] Award +2 Reputation Score points in LocalStorage
                let reputationAwarded = false;
                try {
                    const userKey = 'BookBlossomUser';
                    let userData = localStorage.getItem(userKey);
                    if (userData) {
                        const user = JSON.parse(userData);
                        const oldScore = Number(user.reputationScore) || 110;
                        const maxScore = Number(user.maxReputationScore) || 150;
                        user.reputationScore = Math.min(maxScore, oldScore + 2);
                        localStorage.setItem(userKey, JSON.stringify(user));
                        reputationAwarded = true;
                        console.log(`[Reputation Update] Score increased from ${oldScore} to ${user.reputationScore} (+2 points)`);
                    } else {
                        // Fallback default state if user has not loaded profile yet
                        const defaultUser = {
                            fullName: "Jane Doe",
                            username: "janedoe_bookworm",
                            reputationScore: 112,
                            maxReputationScore: 150
                        };
                        localStorage.setItem(userKey, JSON.stringify(defaultUser));
                        reputationAwarded = true;
                    }
                } catch (e) {
                    console.error("Error updating reputation score in localStorage:", e);
                }
                
                const pointsMessage = reputationAwarded 
                    ? `Thank you! Your review has been submitted successfully. <strong>You have earned +2 Reputation Score points!</strong>` 
                    : `Thank you! Your review has been submitted successfully.`;

                this.view.showConfirmModal({
                    icon: 'fas fa-check-circle',
                    iconColor: '#38a169',
                    accentColor: 'linear-gradient(90deg, #38a169, #68d391)',
                    title: 'Review Submitted',
                    message: pointsMessage,
                    confirmText: 'Great',
                    confirmBtnClass: 'btn-success',
                    onConfirm: () => {}
                });
            });
        }
    }

    // [NEW] Handle clicking View Review
    handleViewReview(orderId, title, isBlind) {
        const order = this.model.orders.find(o => o.id === orderId);
        
        // Set a flag in sessionStorage so the book detail page knows to show user's review first
        sessionStorage.setItem('show-my-review-first', 'true');
        sessionStorage.setItem('my-review-text', order?.userReviewText || 'Great book, very satisfied with my purchase!');
        sessionStorage.setItem('my-review-rating', order?.userRating || '5');
        
        if (isBlind) {
            window.location.href = `/BlindDate#blind-details-${encodeURIComponent(title)}?tab=reviews`;
        } else {
            window.location.href = `/Explore#book-details-${encodeURIComponent(title)}?tab=reviews`;
        }
    }

    // [NEW] Handle clicking Reveal Real Book
    handleRevealRealBook(orderId) {
        const order = this.model.orders.find(o => o.id === orderId);
        if (order) {
            this.view.showRevealRealBookModal(order);
        }
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

    // [NEW] Handle clicking on Return/Refund button on an order
    handleReturnRefundClick(orderId) {
        const order = this.model.orders.find(o => o.id === orderId);
        if (order) {
            this.view.showReturnRefundModal(order);
        }
    }

    // [NEW] Handle submitting Return/Refund request data
    handleReturnRefundSubmit(orderId, requestData) {
        const order = this.model.orders.find(o => o.id === orderId);
        if (!order) return;

        // Perform model update
        this.model.submitReturnRefund(orderId, requestData);

        // Hide return/refund modal using Bootstrap 5
        const modalEl = document.getElementById('returnRefundModal');
        const modalInstance = bootstrap.Modal.getInstance(modalEl);
        if (modalInstance) {
            modalInstance.hide();
        }

        // Show successful completion feedback overlay popup modal using confirmation styling
        this.view.showConfirmModal({
            icon: 'fas fa-check-circle',
            iconColor: '#38a169',
            accentColor: 'linear-gradient(90deg, #38a169, #68d391)',
            title: 'Request Submitted',
            message: `Your return/refund request for Order #${orderId} was submitted successfully! The seller has 48 hours to respond.`,
            confirmText: 'Great, Thank You',
            confirmBtnClass: 'btn-success',
            onConfirm: () => {
                // Refresh views
                this.updateView();
                this.updateBadge();
            }
        });
    }
}
