class OrdersView {
    constructor() {
        this.ordersListContainer = document.getElementById('orders-list-container');
        this.emptyState = document.getElementById('orders-empty-state');
        this.tabLinks = document.querySelectorAll('.orders-tabs .nav-link');
        this.searchInput = document.getElementById('orders-search-input');
        this.toReceiveBadge = document.getElementById('to-receive-badge');
    }

    bindTabChange(handler) {
        this.tabLinks.forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                // Remove active class from all
                this.tabLinks.forEach(t => t.classList.remove('active'));
                // Add active class to clicked
                e.target.classList.add('active');

                const tab = e.target.getAttribute('data-tab');
                handler(tab);
            });
        });
    }

    bindSearch(handler) {
        this.searchInput.addEventListener('input', (e) => {
            handler(e.target.value);
        });
    }

    updateToReceiveBadge(count) {
        if (count > 0) {
            this.toReceiveBadge.textContent = count;
            this.toReceiveBadge.style.display = 'inline-block';
        } else {
            this.toReceiveBadge.style.display = 'none';
        }
    }

    renderOrders(orders) {
        this.ordersListContainer.innerHTML = '';

        if (orders.length === 0) {
            this.ordersListContainer.style.display = 'none';
            this.emptyState.style.display = 'block';
            return;
        }

        this.ordersListContainer.style.display = 'block';
        this.emptyState.style.display = 'none';

        orders.forEach(order => {
            const orderHtml = this.generateOrderHtml(order);
            this.ordersListContainer.insertAdjacentHTML('beforeend', orderHtml);
        });
    }

    generateOrderHtml(order) {
        const itemsHtml = order.items.map(item => `
            <div class="order-item">
                <img src="${item.image}" alt="${item.title}" class="order-item-img" onerror="this.src='/images/placeholder.jpg'">
                <div class="order-item-details">
                    <div class="order-item-title">${item.title}</div>
                    <div class="order-item-meta">Author: ${item.author}</div>
                    <div class="order-item-meta">Qty: ${item.quantity}</div>
                </div>
                <div class="order-item-price">${item.price.toLocaleString('vi-VN')}đ</div>
            </div>
        `).join('');

        let actionsHtml = '';
        let statusText = '';
        let extraInfoHtml = '';

        switch (order.status) {
            case 'to-confirm':
                statusText = 'To Confirm';
                actionsHtml = `
                    <button class="btn btn-primary" data-action="cancel-order" data-id="${order.id}">Cancel Order</button>
                `;
                break;
            case 'to-ship':
                statusText = 'To Ship';
                extraInfoHtml = `<div class="text-muted small mb-2"><i class="fas fa-truck"></i> ${order.tracking}</div>`;
                actionsHtml = `
                    <button class="btn btn-primary">Track Order</button>
                `;
                break;
            case 'to-receive':
                statusText = 'To Receive';
                extraInfoHtml = `<div class="text-muted small mb-2"><i class="fas fa-truck"></i> ${order.tracking}</div>`;
                actionsHtml = `
                    <button class="btn btn-primary" data-action="order-received" data-id="${order.id}">Order Received</button>
                    <button class="btn btn-outline-secondary">Return/Refund</button>
                `;
                break;
            case 'completed':
                statusText = 'Completed';
                // [UPDATED] Buy Again opens the Secure Checkout popup
                actionsHtml = `
                    <button class="btn btn-primary" data-action="buy-again" data-id="${order.id}">Buy Again</button>
                    ${!order.isRated ? '<button class="btn btn-outline-secondary">Rate</button>' : ''}
                `;
                break;
            case 'cancelled':
                statusText = 'Cancelled';
                extraInfoHtml = `<div class="text-muted small mb-2"><i class="fas fa-times-circle text-danger"></i> Reason: ${order.cancelReason}</div>`;
                // [UPDATED] Buy Again opens the Secure Checkout popup
                actionsHtml = `
                    <button class="btn btn-primary" data-action="buy-again" data-id="${order.id}">Buy Again</button>
                `;
                break;
            case 'returned':
                statusText = 'Returned';
                extraInfoHtml = `<div class="text-muted small mb-2"><i class="fas fa-undo-alt text-warning"></i> Reason: ${order.cancelReason}</div>`;
                // [UPDATED] Buy Again opens the Secure Checkout popup
                actionsHtml = `
                    <button class="btn btn-primary" data-action="buy-again" data-id="${order.id}">Buy Again</button>
                `;
                break;
        }

        // Always add view details to the end
        actionsHtml += `<button class="btn btn-outline-secondary" data-action="view-details" data-id="${order.id}">View Details</button>`;

        return `
            <div class="order-card">
                <div class="order-header">
                    <div class="order-shop-info">
                        <i class="fas fa-store"></i> ${order.shopName}
                    </div>
                    <div class="order-status">
                        ${statusText}
                    </div>
                </div>
                
                ${extraInfoHtml}
                
                <div class="order-items-container">
                    ${itemsHtml}
                </div>
                
                <div class="order-footer">
                    <div class="order-total-price">
                        Total: <strong>${order.totalPrice.toLocaleString('vi-VN')}đ</strong>
                    </div>
                    <div class="order-actions">
                        ${actionsHtml}
                    </div>
                </div>
            </div>
        `;
    }

    bindViewDetails(handler) {
        this.ordersListContainer.addEventListener('click', (e) => {
            if (e.target.closest('[data-action="view-details"]')) {
                const btn = e.target.closest('[data-action="view-details"]');
                const orderId = btn.getAttribute('data-id');
                handler(orderId);
            }
        });
    }

    bindCancelOrder(handler) {
        this.ordersListContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-action="cancel-order"]');
            if (btn) {
                const orderId = btn.getAttribute('data-id');
                handler(orderId);
            }
        });
    }

    bindOrderReceived(handler) {
        this.ordersListContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-action="order-received"]');
            if (btn) {
                const orderId = btn.getAttribute('data-id');
                handler(orderId);
            }
        });
    }

    // [UPDATED] Delegate Buy Again clicks to the controller handler
    bindBuyAgain(handler) {
        this.ordersListContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-action="buy-again"]');
            if (btn) {
                const orderId = btn.getAttribute('data-id');
                handler(orderId);
            }
        });
    }

    showConfirmModal({ icon, iconColor, accentColor, title, message, confirmText, confirmBtnClass, showReasonInput = false, onConfirm }) {
        // Set accent bar color
        document.getElementById('confirm-modal-accent').style.background = accentColor || 'linear-gradient(90deg, #d8456b, #f76b8a)';

        // Set icon
        const iconEl = document.getElementById('confirm-modal-icon');
        iconEl.innerHTML = `<i class="${icon}" style="color:${iconColor || '#d8456b'};"></i>`;

        // Set texts
        document.getElementById('confirmActionModalLabel').textContent = title || 'Confirm Action';
        document.getElementById('confirm-modal-message').textContent = message || 'Are you sure?';

        // Show / hide reason textarea and reset state
        const reasonWrapper = document.getElementById('confirm-reason-wrapper');
        const reasonInput = document.getElementById('confirm-reason-input');
        const reasonError = document.getElementById('confirm-reason-error');
        reasonWrapper.style.display = showReasonInput ? 'block' : 'none';
        reasonInput.value = '';
        reasonError.style.display = 'none';
        reasonInput.style.borderColor = '#e2e8f0';

        // Set confirm button
        const actionBtn = document.getElementById('confirm-modal-action-btn');
        actionBtn.textContent = confirmText || 'Confirm';
        actionBtn.className = `btn rounded-pill px-4 ${confirmBtnClass || 'btn-danger'}`;
        actionBtn.style.minWidth = '140px';

        // Attach the callback (clone to remove old listeners)
        const freshBtn = actionBtn.cloneNode(true);
        actionBtn.parentNode.replaceChild(freshBtn, actionBtn);
        freshBtn.addEventListener('click', () => {
            if (showReasonInput) {
                const reason = reasonInput.value.trim();
                if (!reason) {
                    reasonError.style.display = 'block';
                    reasonInput.style.borderColor = '#e53e3e';
                    reasonInput.focus();
                    return;
                }
                const modalEl = document.getElementById('confirmActionModal');
                bootstrap.Modal.getInstance(modalEl)?.hide();
                onConfirm(reason);
            } else {
                const modalEl = document.getElementById('confirmActionModal');
                bootstrap.Modal.getInstance(modalEl)?.hide();
                onConfirm();
            }
        });

        // Live-clear validation error as user types
        reasonInput.oninput = () => {
            if (reasonInput.value.trim()) {
                reasonError.style.display = 'none';
                reasonInput.style.borderColor = '#68d391';
            }
        };

        // Show modal via Bootstrap 5
        const modalEl = document.getElementById('confirmActionModal');
        const modal = new bootstrap.Modal(modalEl);
        modal.show();

        // Focus textarea after modal opens (if shown)
        if (showReasonInput) {
            modalEl.addEventListener('shown.bs.modal', () => reasonInput.focus(), { once: true });
        }
    }

    showOrderDetailsModal(order) {
        // Populate modal data
        document.getElementById('detail-order-id').textContent = `#${order.id}`;
        document.getElementById('detail-receiver').textContent = order.shipReceiverName || 'N/A';
        document.getElementById('detail-phone').textContent = order.shipPhoneNumber || 'N/A';
        document.getElementById('detail-address').textContent = order.shipDetailAddress || 'N/A';
        document.getElementById('detail-payment-method').textContent = order.paymentMethod || 'N/A';
        document.getElementById('detail-note').textContent = order.note || 'None';

        // Populate items
        const itemsList = document.getElementById('detail-items-list');
        itemsList.innerHTML = order.items.map(item => `
            <div class="d-flex align-items-center mb-3">
                <img src="${item.image}" alt="${item.title}" style="width: 50px; height: 70px; object-fit: cover; border-radius: 4px;" class="mr-3" onerror="this.src='/images/placeholder.jpg'">
                <div class="flex-grow-1">
                    <div class="font-weight-medium text-dark">${item.title}</div>
                    <div class="text-muted small">Qty: ${item.quantity}</div>
                </div>
                <div class="font-weight-medium">${item.price.toLocaleString('vi-VN')}đ</div>
            </div>
        `).join('');

        // Summary amounts
        document.getElementById('detail-subtotal').textContent = (order.subTotal || order.totalPrice).toLocaleString('vi-VN') + 'đ';
        document.getElementById('detail-shipping-fee').textContent = (order.shippingFee || 0).toLocaleString('vi-VN') + 'đ';
        document.getElementById('detail-discount-val').textContent = (order.discountAmount || 0).toLocaleString('vi-VN') + 'đ';
        document.getElementById('detail-total').textContent = order.totalPrice.toLocaleString('vi-VN') + 'đ';

        // Timeline
        const timelineEl = document.getElementById('detail-timeline');
        let timelineHtml = '';
        if (order.orderDate) timelineHtml += `<li><small class="text-muted">${order.orderDate}</small> - Order placed</li>`;
        if (order.shippedDate) timelineHtml += `<li><small class="text-muted">${order.shippedDate}</small> - Order shipped</li>`;
        if (order.deliveredDate) timelineHtml += `<li><small class="text-muted">${order.deliveredDate}</small> - Order delivered</li>`;
        if (order.completedDate) timelineHtml += `<li><small class="text-muted">${order.completedDate}</small> - Order completed</li>`;
        if (order.cancelReason) {
            const statusLabel = order.status === 'returned' ? 'Returned' : 'Cancelled';
            timelineHtml += `<li><small class="text-danger">${statusLabel}: ${order.cancelReason}</small></li>`;
        }

        timelineEl.innerHTML = timelineHtml;

        // Show modal using jQuery
        $('#orderDetailsModal').modal('show');
    }
}
