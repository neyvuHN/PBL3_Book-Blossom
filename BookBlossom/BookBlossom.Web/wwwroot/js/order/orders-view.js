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

        switch(order.status) {
            case 'to-confirm':
                statusText = 'To Confirm';
                actionsHtml = `
                    <button class="btn btn-primary">Cancel Order</button>
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
                    <button class="btn btn-primary">Order Received</button>
                    <button class="btn btn-outline-secondary">Return/Refund</button>
                `;
                break;
            case 'completed':
                statusText = 'Completed';
                actionsHtml = `
                    <button class="btn btn-primary">Buy Again</button>
                    ${!order.isRated ? '<button class="btn btn-outline-secondary">Rate</button>' : ''}
                `;
                break;
            case 'cancelled-return':
                statusText = 'Cancelled/Returned';
                extraInfoHtml = `<div class="text-muted small mb-2"><i class="fas fa-info-circle"></i> Reason: ${order.cancelReason}</div>`;
                actionsHtml = `
                    <button class="btn btn-primary">Buy Again</button>
                    <button class="btn btn-outline-secondary">View Details</button>
                `;
                break;
        }

        // Always add view details to the end if not already present
        if(order.status !== 'cancelled-return') {
            actionsHtml += `<button class="btn btn-outline-secondary">View Details</button>`;
        }

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
}
