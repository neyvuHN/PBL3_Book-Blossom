class OrdersController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
    }

    async init() {
        // Fetch real data
        await this.model.fetchOrders();

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
        const order = this.model.orders.find(o => o.id.toString() === orderId.toString());
        if (order) {
            this.view.showOrderDetailsModal(order);
        }
    }

    handleTrackOrder(orderId) {
        const order = this.model.orders.find(o => o.id.toString() === orderId.toString());
        if (order) {
            this.view.showOrderTrackingModal(order);
        }
    }

    handleCancelOrder(orderId) {
        const order = this.model.orders.find(o => o.id.toString() === orderId.toString());
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
            onConfirm: async (reason) => {
                const success = await this.model.cancelOrderApi(order.id, reason);
                if (success) {
                    await this.init(); // Refresh data
                } else {
                    alert("Failed to cancel order.");
                }
            }
        });
    }

    handleOrderReceived(orderId) {
        const order = this.model.orders.find(o => o.id.toString() === orderId.toString());
        if (!order) return;

        this.view.showConfirmModal({
            icon: 'fas fa-check-circle',
            iconColor: '#38a169',
            accentColor: 'linear-gradient(90deg, #38a169, #68d391)',
            title: 'Confirm Order Received',
            message: `Have you received your order #${order.id}? Please confirm only after the package is in your hands.`,
            confirmText: 'Yes, I\'ve Received It',
            confirmBtnClass: 'btn-success',
            onConfirm: async () => {
                const success = await this.model.confirmReceivedApi(order.id);
                if (success) {
                    await this.init(); // Refresh data
                } else {
                    alert("Failed to confirm received.");
                }
            }
        });
    }

    async handleBuyAgain(orderId) {
        const order = this.model.orders.find(o => o.id.toString() === orderId.toString());
        if (!order) return;

        try {
            const token = this.model.getToken();
            for (const item of order.items) {
                // Ignore blind books for rebuy if we cannot easily fetch them, or map properly
                await fetch('/api/Cart', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${token}`
                    },
                    body: JSON.stringify({
                        bookID: item.isBlind ? null : item.id,
                        blindBookID: item.isBlind ? item.id : null,
                        quantity: item.quantity
                    })
                });
            }
            // Navigate to Cart or show a toast
            window.location.href = '/Cart';
        } catch (error) {
            console.error("Failed to add items back to cart", error);
            alert("Failed to add items to cart.");
        }
    }

    // [NEW] Handle clicking Rate
    handleRateOrder(orderId) {
        const order = this.model.orders.find(o => o.id.toString() === orderId.toString());
        if (order && !order.isRated) {
            this.view.showRateOrderModal(order, async (id, rating, reviewText, mediaFiles) => {
                const token = this.model.getToken();
                if (!token) {
                    alert('Please log in to submit a review.');
                    return;
                }

                const item = order.items[0]; // Assuming rating the first item, or we rate the order and item
                const formData = new FormData();
                formData.append('OrderID', order.id);
                if (item) {
                    if (item.isBlind) {
                        formData.append('BlindBookID', item.id);
                    } else {
                        formData.append('BookID', item.id);
                    }
                }
                formData.append('Rating', rating);
                formData.append('Content', reviewText);
                
                if (mediaFiles && mediaFiles.length > 0) {
                    mediaFiles.forEach(f => {
                        formData.append('mediaFiles', f);
                    });
                }

                // Show loading state on button (done implicitly if fast, or can be improved)
                try {
                    const response = await fetch('/api/Review', {
                        method: 'POST',
                        headers: {
                            'Authorization': `Bearer ${token}`
                        },
                        body: formData
                    });

                    if (!response.ok) {
                        const errData = await response.json().catch(() => ({}));
                        throw new Error(errData.message || 'Failed to submit review');
                    }

                    const result = await response.json();

                    order.isRated = true;
                    order.userRating = rating;
                    order.userReviewText = reviewText;
                    
                    this.updateView();

                    // Show success modal with reputation info if any
                    let reputationPoints = result.awardedReputation || 2; // Default to 2 if not provided by backend yet
                    const pointsMessage = reputationPoints > 0 
                        ? `Thank you! Your review has been submitted successfully. <strong>You have earned +${reputationPoints} Reputation Score points!</strong>` 
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

                } catch (error) {
                    console.error("Error submitting review:", error);
                    alert(error.message || 'An error occurred while submitting your review. Please try again.');
                }
            });
        }
    }

    // [NEW] Handle clicking View Review
    handleViewReview(orderId, title, isBlind) {
        const order = this.model.orders.find(o => o.id.toString() === orderId.toString());
        
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
        const order = this.model.orders.find(o => o.id.toString() === orderId.toString());
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
        const order = this.model.orders.find(o => o.id.toString() === orderId.toString());
        if (order) {
            this.view.showReturnRefundModal(order);
        }
    }

    // [NEW] Handle submitting Return/Refund request data
    async handleReturnRefundSubmit(orderId, requestData) {
        const order = this.model.orders.find(o => o.id.toString() === orderId.toString());
        if (!order) return;

        const submitBtn = document.getElementById('submit-refund-request-btn');
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Submitting...';
        submitBtn.setAttribute('disabled', 'true');

        // Perform model update
        const result = await this.model.submitReturnRefund(orderId, requestData);

        submitBtn.innerHTML = originalText;
        submitBtn.removeAttribute('disabled');

        if (result && result.success) {
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
                onConfirm: async () => {
                    // Refresh views
                    await this.init();
                }
            });
        } else {
            alert(result && result.message ? result.message : "Failed to submit return/refund request. Please check your data and try again.");
        }
    }
}
