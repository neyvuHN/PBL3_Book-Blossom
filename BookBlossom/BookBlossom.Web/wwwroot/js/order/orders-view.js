class OrdersView {
    constructor() {
        this.ordersListContainer = document.getElementById('orders-list-container');
        this.emptyState = document.getElementById('orders-empty-state');
        this.tabLinks = document.querySelectorAll('.orders-tabs .nav-link');
        this.searchInput = document.getElementById('orders-search-input');
        this.toReceiveBadge = document.getElementById('to-receive-badge');
        this.toReceiveBannerContainer = document.getElementById('to-receive-banner-container');

        // [NEW] Return/Refund state management
        this.uploadedImages = [];
        this.uploadedVideo = null;
        this.activeProposal = 'return'; // Default proposal
        this.currentOrderTotal = 0;
        this.isVideoUploading = false;

        this.initReturnRefundEvents();
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
        const activeTabEl = document.querySelector('.orders-tabs .nav-link.active');
        const currentTab = activeTabEl ? activeTabEl.getAttribute('data-tab') : '';
        this.updateToReceiveBanner(currentTab);

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

    updateToReceiveBanner(currentTab) {
        if (!this.toReceiveBannerContainer) return;

        if (currentTab === 'to-receive') {
            this.toReceiveBannerContainer.innerHTML = `
                <div class="alert alert-info alert-dismissible fade show mb-4 border-0 shadow-sm d-flex align-items-center" role="alert" style="background-color: #ebf8ff; border-left: 4px solid #3182ce !important; border-radius: 8px; color: #2b6cb0; padding: 16px 20px; position: relative;">
                    <div style="font-size: 1.25rem; display: flex; align-items: center; margin-right: 18px;">
                        <i class="fas fa-info-circle"></i>
                    </div>
                    <div style="font-size: 0.92rem; line-height: 1.5; padding-right: 24px; margin-left: 6px;">
                        <strong>Auto-Complete Policy:</strong> If you do not respond (click "Order Received" or submit a "Return/Refund" request) within <strong>7 days</strong> of successful delivery, the order will be automatically marked as <strong>Completed</strong>.
                    </div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close" style="position: absolute; right: 15px; top: 50%; transform: translateY(-50%); border: none; background: transparent; color: #2b6cb0; font-size: 0.8rem;"></button>
                </div>
            `;
            this.toReceiveBannerContainer.style.display = 'block';
        } else {
            this.toReceiveBannerContainer.innerHTML = '';
            this.toReceiveBannerContainer.style.display = 'none';
        }
    }

    generateOrderHtml(order) {
        const itemsHtml = order.items.map(item => `
            <div class="order-item">
                <!-- [UPDATED] Added data attributes and CSS link class to the book image -->
                <img src="${item.image}" alt="${item.title}" class="order-item-img order-item-img-link" data-action="view-book" data-title="${item.title}" data-blind="${item.isBlind || false}" onerror="this.src='/images/placeholder.jpg'">
                <div class="order-item-details">
                    <!-- [UPDATED] Added data attributes and CSS link class to the book title -->
                    <div class="order-item-title order-item-title-link" data-action="view-book" data-title="${item.title}" data-blind="${item.isBlind || false}">${item.title}</div>
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
                    <button class="btn btn-primary" data-action="track-order" data-id="${order.id}">Track Order</button>
                `;
                break;
            case 'to-receive':
                statusText = 'To Receive';
                extraInfoHtml = `<div class="text-muted small mb-2"><i class="fas fa-truck"></i> ${order.tracking}</div>`;
                actionsHtml = `
                    <button class="btn btn-primary" data-action="order-received" data-id="${order.id}">Order Received</button>
                    <button class="btn btn-outline-secondary" data-action="return-refund" data-id="${order.id}">Return/Refund</button>
                `;
                break;
            case 'completed':
                statusText = 'Completed';
                const hasBlindBook = order.items.some(item => item.isBlind && item.realBook);
                // [UPDATED] Buy Again opens the Secure Checkout popup
                actionsHtml = `
                    <button class="btn btn-primary" data-action="buy-again" data-id="${order.id}">Buy Again</button>
                    ${hasBlindBook ? `<button class="btn btn-outline-info" data-action="reveal-real-book" data-id="${order.id}">Reveal Real Book <i class="fas fa-magic"></i></button>` : ''}
                    ${!order.isRated ? `<button class="btn btn-outline-secondary" data-action="rate-order" data-id="${order.id}">Rate</button>` : `<button class="btn btn-outline-secondary" data-action="view-review" data-id="${order.id}" data-book-title="${order.items[0].title}" data-blind="${order.items[0].isBlind || false}">View Review</button>`}
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
                statusText = order.resolutionType === 0 ? 'Refund Only' : 'Returned';
                let returnStatusBadge = '';
                if (order.returnStatus === 0) {
                    returnStatusBadge = '<span class="badge bg-warning text-dark ms-2">Pending</span>';
                } else if (order.returnStatus === 1) {
                    returnStatusBadge = '<span class="badge bg-success ms-2">Approved</span>';
                } else if (order.returnStatus === 2) {
                    returnStatusBadge = '<span class="badge bg-danger ms-2">Rejected</span>';
                }
                statusText += returnStatusBadge;
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

    bindTrackOrder(handler) {
        this.ordersListContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-action="track-order"]');
            if (btn) {
                const orderId = btn.getAttribute('data-id');
                handler(orderId);
            }
        });
    }

    // [NEW] Delegate Rate Order clicks to the controller handler
    bindRateOrder(handler) {
        this.ordersListContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-action="rate-order"]');
            if (btn) {
                const orderId = btn.getAttribute('data-id');
                handler(orderId);
            }
        });
    }

    // [NEW] Delegate View Review clicks to the controller handler
    bindViewReview(handler) {
        this.ordersListContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-action="view-review"]');
            if (btn) {
                const orderId = btn.getAttribute('data-id');
                const title = btn.getAttribute('data-book-title');
                const isBlind = btn.getAttribute('data-blind') === 'true';
                handler(orderId, title, isBlind);
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

    // [UPDATED] Delegate book/blind-date item clicks to the controller handler
    bindViewBook(handler) {
        // Bind click on order list items
        this.ordersListContainer.addEventListener('click', (e) => {
            const target = e.target.closest('[data-action="view-book"]');
            if (target) {
                const title = target.getAttribute('data-title');
                const isBlind = target.getAttribute('data-blind') === 'true';
                handler(title, isBlind);
            }
        });

        // Bind click on order details modal items
        const modalItemsList = document.getElementById('detail-items-list');
        if (modalItemsList) {
            modalItemsList.addEventListener('click', (e) => {
                const target = e.target.closest('[data-action="view-book"]');
                if (target) {
                    // Hide the details modal first before navigating
                    $('#orderDetailsModal').modal('hide');
                    const title = target.getAttribute('data-title');
                    const isBlind = target.getAttribute('data-blind') === 'true';
                    handler(title, isBlind);
                }
            });
        }
    }

    // [NEW] Delegate Reveal Real Book clicks to the controller handler
    bindRevealRealBook(handler) {
        this.ordersListContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-action="reveal-real-book"]');
            if (btn) {
                const orderId = btn.getAttribute('data-id');
                handler(orderId);
            }
        });
    }

    showRevealRealBookModal(order) {
        // Find the blind book item that has a realBook mapping
        const blindItem = order.items.find(item => item.isBlind && item.realBook);
        if (!blindItem) return;

        const realBook = blindItem.realBook;

        document.getElementById('reveal-real-book-title').textContent = realBook.title;
        document.getElementById('reveal-real-book-author').textContent = realBook.author;
        document.getElementById('reveal-real-book-image').src = realBook.image;
        document.getElementById('reveal-real-book-desc').textContent = realBook.description;

        // Attach click event on the entire card to redirect to detail page
        const cardEl = document.getElementById('reveal-book-card-container');
        if (cardEl) {
            cardEl.onclick = () => {
                $('#revealRealBookModal').modal('hide');
                window.location.href = `/Explore#book-details-${encodeURIComponent(realBook.title)}`;
            };
        }

        $('#revealRealBookModal').modal('show');
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
                <!-- [UPDATED] Added data attributes and CSS link class to modal book image -->
                <img src="${item.image}" alt="${item.title}" style="width: 50px; height: 70px; object-fit: cover; border-radius: 4px;" class="mr-3 order-item-img-link" data-action="view-book" data-title="${item.title}" data-blind="${item.isBlind || false}" onerror="this.src='/images/placeholder.jpg'">
                <div class="flex-grow-1">
                    <!-- [UPDATED] Added data attributes and CSS link class to modal book title -->
                    <div class="font-weight-medium text-dark order-item-title-link" data-action="view-book" data-title="${item.title}" data-blind="${item.isBlind || false}">${item.title}</div>
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

    showOrderTrackingModal(order) {
        document.getElementById('tracking-order-id').textContent = order.id;

        const statusBadge = document.getElementById('tracking-order-status');
        let statusLabel = '';
        let statusBg = '#ebf8ff';
        let statusColor = '#2b6cb0';

        switch (order.status) {
            case 'to-confirm':
                statusLabel = 'To Confirm';
                statusBg = '#edf2f7';
                statusColor = '#4a5568';
                break;
            case 'to-ship':
                statusLabel = 'To Ship';
                statusBg = '#feebc8';
                statusColor = '#dd6b20';
                break;
            case 'to-receive':
                statusLabel = 'To Receive';
                statusBg = '#ebf8ff';
                statusColor = '#2b6cb0';
                break;
            case 'completed':
                statusLabel = 'Completed';
                statusBg = '#c6f6d5';
                statusColor = '#22543d';
                break;
            case 'cancelled':
                statusLabel = 'Cancelled';
                statusBg = '#fed7d7';
                statusColor = '#9b2c2c';
                break;
            case 'returned':
                statusLabel = 'Returned';
                statusBg = '#e2e8f0';
                statusColor = '#4a5568';
                break;
            default:
                statusLabel = order.status;
        }

        statusBadge.textContent = statusLabel;
        statusBadge.style.backgroundColor = statusBg;
        statusBadge.style.color = statusColor;

        const container = document.getElementById('tracking-timeline-container');
        const milestones = order.trackingMilestones || this.generateDefaultMilestones(order);

        container.innerHTML = milestones.map((ms, idx) => {
            const isCompleted = ms.status === 'completed';
            const isCurrent = ms.status === 'current';

            let stepClass = 'pending';
            let iconHtml = '';

            if (isCompleted) {
                stepClass = 'completed';
                iconHtml = '<i class="fas fa-check"></i>';
            } else if (isCurrent) {
                stepClass = 'current';
                iconHtml = '<i class="fas fa-truck"></i><span class="node-subtext">In Transit</span>';
            }

            let connectorHtml = '';
            if (idx < milestones.length - 1) {
                const nextMs = milestones[idx + 1];
                const isNextActive = nextMs.status === 'completed' || nextMs.status === 'current';
                const lineClass = (isCompleted && isNextActive) ? 'solid-green' : 'dashed-gray';
                connectorHtml = `<div class="tracking-connector ${lineClass}"></div>`;
            }

            const timeHtml = ms.time ? `<div class="tracking-time">${ms.time}</div>` : '';
            const descHtml = ms.description ? `<div class="tracking-desc">${ms.description}</div>` : '';

            return `
                <div class="tracking-step ${stepClass}">
                    <div class="tracking-node">
                        ${iconHtml}
                    </div>
                    <div class="tracking-info">
                        <div class="tracking-title">${ms.title}</div>
                        ${timeHtml}
                        ${descHtml}
                    </div>
                    ${connectorHtml}
                </div>
            `;
        }).join('');

        // Show modal using Bootstrap 5
        const modalEl = document.getElementById('orderTrackingModal');
        const modal = new bootstrap.Modal(modalEl);
        modal.show();
    }

    generateDefaultMilestones(order) {
        // Fallback milestone generator if specific milestones are not supplied
        const milestones = [
            { title: "Order Placed", time: order.orderDate || "Today, 10:00", description: "", status: "completed" },
            { title: "Seller Shipped", time: order.shippedDate || "", description: "", status: "pending" },
            { title: "Arrived at Sort Facility", time: "", description: "", status: "pending" },
            { title: "Out for Delivery", time: "", description: "", status: "pending" },
            { title: "Delivered", time: "", description: "", status: "pending" }
        ];

        if (order.status === 'to-ship') {
            milestones[1].status = 'current';
            milestones[1].time = 'Today, 14:30';
            milestones[1].description = 'Seller is preparing your package.';
        } else if (order.status === 'to-receive') {
            milestones[1].status = 'completed';
            milestones[2].status = 'completed';
            milestones[2].time = order.shippedDate || 'Yesterday, 14:00';
            milestones[3].status = 'current';
            milestones[3].time = 'Today, 08:30';
            milestones[3].description = 'Parcel is out for delivery with the local courier.';
        } else if (order.status === 'completed') {
            milestones.forEach(m => m.status = 'completed');
            milestones[1].time = order.shippedDate || '2 days ago';
            milestones[2].time = 'Yesterday, 10:00';
            milestones[3].time = 'Yesterday, 14:00';
            milestones[4].time = order.completedDate || 'Today, 11:30';
            milestones[4].description = 'Package has been successfully handed over.';
        }

        return milestones;
    }

    // [NEW] Bind click on Return/Refund button
    bindReturnRefundClick(handler) {
        this.ordersListContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-action="return-refund"]');
            if (btn) {
                const orderId = btn.getAttribute('data-id');
                handler(orderId);
            }
        });
    }

    // [NEW] Bind Submit Request click
    bindReturnRefundSubmit(handler) {
        const submitBtn = document.getElementById('submit-refund-request-btn');
        if (!submitBtn) return;

        submitBtn.addEventListener('click', () => {
            if (submitBtn.hasAttribute('disabled')) return;

            const orderId = submitBtn.getAttribute('data-order-id');
            const reasonSelect = document.getElementById('refund-reason-select');

            let reasonText = "";
            if (reasonSelect.value === 'other') {
                const customInput = document.getElementById('custom-reason-input');
                reasonText = customInput ? customInput.value.trim() : "Other reason";
            } else {
                reasonText = reasonSelect.options[reasonSelect.selectedIndex].text;
            }

            let refundAmount = this.currentOrderTotal;
            if (this.activeProposal === 'keep') {
                const cleanAmount = document.getElementById('refund-amount-input').value.replace(/,/g, '');
                refundAmount = parseFloat(cleanAmount) || 0;
            }

            handler(orderId, {
                reason: reasonText,
                proposal: this.activeProposal,
                refundAmount: refundAmount,
                imagesCount: this.uploadedImages.length,
                hasVideo: !!this.uploadedVideo,
                videoFile: this.uploadedVideo
            });
        });
    }

    // [NEW] Open Return/Refund full-screen modal and initialize order-related fields
    showReturnRefundModal(order) {
        this.currentOrderTotal = order.totalPrice;

        // Set dynamic display fields
        document.getElementById('refund-modal-order-id').textContent = `#${order.id}`;
        document.getElementById('refund-modal-order-total').textContent = `${order.totalPrice.toLocaleString('vi-VN')}đ`;
        document.getElementById('return-full-amount-text').textContent = `${order.totalPrice.toLocaleString('vi-VN')} VND`;
        document.getElementById('refund-max-text').textContent = `${order.totalPrice.toLocaleString('vi-VN')} VND`;

        // Prefill maximum refund amount
        const amountInput = document.getElementById('refund-amount-input');
        amountInput.value = order.totalPrice.toLocaleString('vi-VN');

        // Configure requirement text dynamically (video mandatory for orders > 500k as a tooltip tip)
        const requirementText = document.getElementById('video-upload-requirement-text');
        if (order.totalPrice > 500000) {
            requirementText.textContent = "Upload Unboxing Video (REQUIRED - Order exceeds 500k)";
        } else {
            requirementText.textContent = "Upload Unboxing Video (Required for all missing/damaged claims)";
        }

        // Attach order ID to the submit button
        document.getElementById('submit-refund-request-btn').setAttribute('data-order-id', order.id);

        // Reset uploads & form fields
        this.uploadedImages = [];
        this.uploadedVideo = null;
        this.isVideoUploading = false;
        document.getElementById('refund-reason-select').value = "";

        const customBlock = document.getElementById('custom-reason-block');
        const customInput = document.getElementById('custom-reason-input');
        if (customBlock) customBlock.style.display = 'none';
        if (customInput) customInput.value = "";

        // Reset view states
        this.renderPhotoSlots();

        // Reset video panel view states
        document.getElementById('video-upload-initial-state').style.display = 'block';
        document.getElementById('video-upload-progress-state').style.display = 'none';
        document.getElementById('video-upload-success-state').style.display = 'none';

        // Reset proposal state to default (Return Item)
        this.selectProposal('return');

        // Check initial state
        this.updateSubmitButtonState();

        // Show Return/Refund Modal
        const modalEl = document.getElementById('returnRefundModal');
        const modal = new bootstrap.Modal(modalEl);
        modal.show();
    }

    // [NEW] Initialize interactive forms, upload actions, and overlay popups
    initReturnRefundEvents() {
        const photoTrigger = document.getElementById('trigger-photo-upload');
        const photoInput = document.getElementById('refund-images-input');
        const videoTrigger = document.getElementById('trigger-video-upload');
        const videoInput = document.getElementById('refund-video-input');
        const reasonSelect = document.getElementById('refund-reason-select');
        const refundAmountInput = document.getElementById('refund-amount-input');
        const customReasonInput = document.getElementById('custom-reason-input');

        // Photo trigger click
        if (photoTrigger && photoInput) {
            photoTrigger.addEventListener('click', () => photoInput.click());
            photoInput.addEventListener('change', (e) => this.handlePhotoUpload(e.target.files));
        }

        // Video trigger click
        if (videoTrigger && videoInput) {
            videoTrigger.addEventListener('click', () => {
                if (!this.uploadedVideo && !this.isVideoUploading) {
                    videoInput.click();
                }
            });
            videoInput.addEventListener('change', (e) => {
                if (e.target.files.length > 0) {
                    this.handleVideoUpload(e.target.files[0]);
                }
            });
        }

        // Remove video action
        const removeVideoBtn = document.getElementById('remove-video-btn');
        if (removeVideoBtn) {
            removeVideoBtn.addEventListener('click', (e) => {
                e.stopPropagation(); // Avoid triggering videoInput click
                this.uploadedVideo = null;
                document.getElementById('video-upload-initial-state').style.display = 'block';
                document.getElementById('video-upload-success-state').style.display = 'none';
                if (videoInput) videoInput.value = "";
                this.updateSubmitButtonState();
            });
        }

        // Proposal Tab Toggles
        const returnTab = document.getElementById('proposal-return-tab');
        const keepTab = document.getElementById('proposal-keep-tab');
        if (returnTab && keepTab) {
            returnTab.addEventListener('click', () => this.selectProposal('return'));
            keepTab.addEventListener('click', () => this.selectProposal('keep'));
        }

        // Reason change validation
        if (reasonSelect) {
            reasonSelect.addEventListener('change', () => {
                const customBlock = document.getElementById('custom-reason-block');
                const customInput = document.getElementById('custom-reason-input');
                if (reasonSelect.value === 'other') {
                    if (customBlock) customBlock.style.display = 'block';
                    if (customInput) customInput.focus();
                } else {
                    if (customBlock) customBlock.style.display = 'none';
                    if (customInput) customInput.value = "";
                }
                this.updateSubmitButtonState();
            });
        }

        // Custom Reason typing validation
        if (customReasonInput) {
            customReasonInput.addEventListener('input', () => {
                this.updateSubmitButtonState();
            });
        }

        // Refund Amount inputs formatting and validation
        if (refundAmountInput) {
            refundAmountInput.addEventListener('input', (e) => {
                // Extract numeric values only
                let valueStr = e.target.value.replace(/[^0-9]/g, '');
                let numericVal = parseFloat(valueStr) || 0;

                // Enforce max validation
                const validationError = document.getElementById('refund-validation-error');
                if (numericVal > this.currentOrderTotal) {
                    validationError.style.display = 'inline';
                    refundAmountInput.style.borderColor = '#e53e3e';
                } else {
                    validationError.style.display = 'none';
                    refundAmountInput.style.borderColor = '#cbd5e1';
                }

                // Prefill back formatted text
                e.target.value = numericVal.toLocaleString('vi-VN');
                this.updateSubmitButtonState();
            });
        }

        // Policy overlay triggers
        const openPolicyBtn = document.getElementById('open-dispute-policy-btn');
        const dismissPolicyBtn = document.getElementById('dismiss-policy-btn');
        const closePolicyBtn = document.getElementById('close-policy-btn');

        if (openPolicyBtn) {
            openPolicyBtn.addEventListener('click', (e) => {
                e.preventDefault();
                const policyModalEl = document.getElementById('fairDisputePolicyModal');
                const policyModal = new bootstrap.Modal(policyModalEl);
                policyModal.show();
            });
        }

        const hidePolicy = () => {
            const policyModalEl = document.getElementById('fairDisputePolicyModal');
            const modalInstance = bootstrap.Modal.getInstance(policyModalEl);
            if (modalInstance) modalInstance.hide();
        };

        if (dismissPolicyBtn) dismissPolicyBtn.addEventListener('click', hidePolicy);
        if (closePolicyBtn) closePolicyBtn.addEventListener('click', hidePolicy);
    }

    // [NEW] Handle simulated photo file uploads
    handlePhotoUpload(files) {
        if (this.uploadedImages.length >= 5) return;

        // Mock upload images simulation
        for (let i = 0; i < files.length; i++) {
            if (this.uploadedImages.length >= 5) break;

            const file = files[i];
            const objectUrl = URL.createObjectURL(file);
            this.uploadedImages.push({
                name: file.name,
                url: objectUrl
            });
        }

        this.renderPhotoSlots();
        this.updateSubmitButtonState();

        // Reset file input value to allow uploading same photo again
        const photoInput = document.getElementById('refund-images-input');
        if (photoInput) photoInput.value = "";
    }

    // [NEW] Renders photo grids including custom uploaded image containers and triggers
    renderPhotoSlots() {
        const photoGrid = document.querySelector('.proof-photo-grid');
        if (!photoGrid) return;

        // Clear all except the first item (which is the trigger)
        const photoSlots = photoGrid.querySelectorAll('.photo-slot');
        photoSlots.forEach(slot => slot.remove());

        // Append active uploaded photo containers
        this.uploadedImages.forEach((imgData, index) => {
            const slotHtml = `
                <div class="upload-box-square photo-slot has-image" style="width: 85px; height: 85px;">
                    <img src="${imgData.url}" alt="Proof Image ${index + 1}">
                    <button type="button" class="delete-photo-btn" data-index="${index}">&times;</button>
                </div>
            `;
            photoGrid.insertAdjacentHTML('beforeend', slotHtml);
        });

        // Add back placeholders to pad up to 5 empty boxes
        const emptySlotsCount = 5 - this.uploadedImages.length;
        for (let i = 0; i < emptySlotsCount; i++) {
            const slotHtml = `
                <div class="upload-box-square photo-slot empty-slot" style="width: 85px; height: 85px; border: 2px dashed #cbd5e1; border-radius: 12px; background-color: #edf2f7; opacity: 0.5;"></div>
            `;
            photoGrid.insertAdjacentHTML('beforeend', slotHtml);
        }

        // Attach click actions to delete photo buttons
        const deleteBtns = photoGrid.querySelectorAll('.delete-photo-btn');
        deleteBtns.forEach(btn => {
            btn.addEventListener('click', (e) => {
                const index = parseInt(btn.getAttribute('data-index'));

                // Revoke URL to prevent memory leaks
                URL.revokeObjectURL(this.uploadedImages[index].url);
                this.uploadedImages.splice(index, 1);

                this.renderPhotoSlots();
                this.updateSubmitButtonState();
            });
        });
    }

    // [NEW] Handle simulated unboxing video upload progress animation
    handleVideoUpload(file) {
        this.isVideoUploading = true;

        // Switch view states
        document.getElementById('video-upload-initial-state').style.display = 'none';
        document.getElementById('video-upload-progress-state').style.display = 'block';
        document.getElementById('video-upload-success-state').style.display = 'none';

        const progressbar = document.getElementById('video-upload-progressbar');
        const statusText = document.getElementById('video-upload-status');
        progressbar.style.width = '0%';

        let progress = 0;
        const uploadSpeed = 100; // ms intervals

        const interval = setInterval(() => {
            progress += Math.floor(Math.random() * 12) + 6;
            if (progress >= 100) {
                progress = 100;
                progressbar.style.width = '100%';
                statusText.textContent = "Uploading video ... 100%";
                clearInterval(interval);

                // Wait briefly, then display success state
                setTimeout(() => {
                    this.isVideoUploading = false;
                    this.uploadedVideo = file;

                    document.getElementById('video-upload-progress-state').style.display = 'none';
                    document.getElementById('video-upload-success-state').style.display = 'block';
                    document.getElementById('uploaded-video-filename').textContent = file.name;

                    this.updateSubmitButtonState();
                }, 400);
            } else {
                progressbar.style.width = `${progress}%`;
                statusText.textContent = `Uploading video ... ${progress}%`;
            }
        }, uploadSpeed);
    }

    // [NEW] Toggle active proposal option: Return Item vs Keep Item
    selectProposal(type) {
        this.activeProposal = type;

        const returnTab = document.getElementById('proposal-return-tab');
        const keepTab = document.getElementById('proposal-keep-tab');
        const refundAmountBlock = document.getElementById('refund-amount-block');
        const returnNoteBlock = document.getElementById('return-note-block');

        if (type === 'return') {
            returnTab.classList.add('active');
            keepTab.classList.remove('active');
            if (refundAmountBlock) refundAmountBlock.style.display = 'none';
            if (returnNoteBlock) returnNoteBlock.style.display = 'block';
        } else {
            returnTab.classList.remove('active');
            keepTab.classList.add('active');
            if (refundAmountBlock) refundAmountBlock.style.display = 'block';
            if (returnNoteBlock) returnNoteBlock.style.display = 'none';
        }

        this.updateSubmitButtonState();
    }

    // [NEW] Validate requirements: unboxing video mandatory, at least 2 images, reason selected, valid refund amount
    updateSubmitButtonState() {
        const submitBtn = document.getElementById('submit-refund-request-btn');
        if (!submitBtn) return;

        const reasonSelect = document.getElementById('refund-reason-select');
        let reasonSelected = reasonSelect && reasonSelect.value !== "";

        // If 'other' is selected, require custom reason text
        if (reasonSelect && reasonSelect.value === 'other') {
            const customInput = document.getElementById('custom-reason-input');
            reasonSelected = customInput && customInput.value.trim() !== "";
        }

        const hasVideo = this.uploadedVideo !== null;
        const hasMinPhotos = this.uploadedImages.length >= 2;

        let amountIsValid = true;
        if (this.activeProposal === 'keep') {
            const amountInput = document.getElementById('refund-amount-input');
            const cleanAmount = amountInput ? amountInput.value.replace(/[^0-9]/g, '') : "0";
            const numericVal = parseFloat(cleanAmount) || 0;

            amountIsValid = numericVal > 0 && numericVal <= this.currentOrderTotal;
        }

        // Must upload unboxing video AND at least 2 images AND choose refund reason AND valid amount
        const canSubmit = reasonSelected && hasVideo && hasMinPhotos && amountIsValid && !this.isVideoUploading;

        if (canSubmit) {
            submitBtn.removeAttribute('disabled');
            submitBtn.style.cursor = 'pointer';
            submitBtn.style.opacity = '1';
        } else {
            submitBtn.setAttribute('disabled', 'true');
            submitBtn.style.cursor = 'not-allowed';
            submitBtn.style.opacity = '0.6';
        }
    }

    // [NEW] Show modal for rating an order
    showRateOrderModal(order, onSubmit) {
        document.getElementById('rate-modal-book-title').textContent = order.items[0].title;
        const reviewInput = document.getElementById('rate-modal-review-text');
        if (reviewInput) reviewInput.value = '';

        let selectedRating = 0;
        let selectedMediaFiles = []; // Store selected files
        const stars = document.querySelectorAll('#rate-modal-stars i');
        const ratingText = document.getElementById('rate-modal-rating-text');
        const wordCounter = document.getElementById('rate-modal-word-count');
        const validationWarning = document.getElementById('rate-modal-validation-warning');
        const phoneWarning = document.getElementById('rate-modal-phone-warning');
        const mediaInput = document.getElementById('rate-modal-media-input');
        const mediaPreviewContainer = document.getElementById('rate-modal-media-preview');
        let submitBtn = document.getElementById('btn-submit-rate-order');

        // Reset UI
        if (stars.length > 0) {
            stars.forEach(s => {
                s.style.color = '#e2e8f0';
            });
        }
        if (ratingText) ratingText.textContent = '';
        if (wordCounter) {
            wordCounter.textContent = '0 / 60 words';
            wordCounter.className = 'font-weight-bold small text-danger';
        }
        if (validationWarning) {
            validationWarning.style.display = 'block';
            validationWarning.textContent = '* Minimum 60 words and a star rating are required to submit.';
        }
        if (phoneWarning) {
            phoneWarning.style.display = 'none';
        }

        const renderMediaPreview = () => {
            if (!mediaPreviewContainer) return;
            // Clear existing previews
            const existingPreviews = mediaPreviewContainer.querySelectorAll('.media-preview-item');
            existingPreviews.forEach(el => el.remove());

            selectedMediaFiles.forEach((file, index) => {
                const url = URL.createObjectURL(file);
                const isVideo = file.type.startsWith('video/');
                const html = `
                    <div class="media-preview-item" style="position: relative; width: 70px; height: 70px; border-radius: 8px; overflow: hidden; border: 1px solid #cbd5e1;">
                        ${isVideo ? `<video src="${url}" style="width:100%;height:100%;object-fit:cover;"></video><div style="position:absolute;top:50%;left:50%;transform:translate(-50%,-50%);color:white;text-shadow:0 0 4px black;"><i class="fas fa-play"></i></div>` : `<img src="${url}" style="width:100%;height:100%;object-fit:cover;">`}
                        <button type="button" class="btn-remove-media" data-index="${index}" style="position: absolute; top: 2px; right: 2px; background: rgba(0,0,0,0.5); color: white; border: none; border-radius: 50%; width: 20px; height: 20px; display: flex; align-items: center; justify-content: center; font-size: 0.7rem; cursor: pointer;">&times;</button>
                    </div>
                `;
                mediaPreviewContainer.insertAdjacentHTML('afterbegin', html);
            });

            // Bind remove events
            mediaPreviewContainer.querySelectorAll('.btn-remove-media').forEach(btn => {
                btn.onclick = function(e) {
                    e.stopPropagation();
                    const index = parseInt(this.getAttribute('data-index'));
                    selectedMediaFiles.splice(index, 1);
                    renderMediaPreview();
                };
            });
        };

        if (mediaInput) {
            mediaInput.value = '';
            // Clone and replace to remove old listeners
            const freshMediaInput = mediaInput.cloneNode(true);
            mediaInput.parentNode.replaceChild(freshMediaInput, mediaInput);
            
            freshMediaInput.addEventListener('change', function() {
                Array.from(this.files).forEach(file => {
                    selectedMediaFiles.push(file);
                });
                renderMediaPreview();
                this.value = ''; // Reset
            });
            renderMediaPreview(); // Clear UI initially
        }

        // Helper to count words
        const getWordCount = (text) => {
            const cleanText = text.trim();
            if (!cleanText) return 0;
            return cleanText.split(/\s+/).filter(word => word.length > 0).length;
        };

        // Helper to check for spam/unhelpful duplicate text and phone numbers
        const checkSpamText = (text) => {
            const cleanText = text.toLowerCase().trim();
            if (!cleanText) return { isSpam: false };

            // Phone number regex check (Vietnamese formats)
            const phoneRegex = /(03|05|07|08|09|01[2|6|8|9])+([0-9]{8})\b/;
            if (phoneRegex.test(cleanText.replace(/\s|\./g, ''))) {
                return { isSpam: true, isPhone: true, reason: "Contains a phone number." };
            }

            const words = cleanText.split(/\s+/).filter(word => word.length > 0);
            if (words.length === 0) return { isSpam: false };

            // 1. Check for consecutive word repetition (e.g. 3 times in a row like "và và và")
            let consecutiveCount = 1;
            for (let i = 1; i < words.length; i++) {
                if (words[i] === words[i - 1]) {
                    consecutiveCount++;
                    if (consecutiveCount >= 3) {
                        return { isSpam: true, reason: "Too many consecutive repeated words (e.g., repeating '" + words[i] + "' consecutively)." };
                    }
                } else {
                    consecutiveCount = 1;
                }
            }

            // 2. Check for unique word ratio (diversity of words)
            const uniqueWords = new Set(words);
            const uniqueRatio = uniqueWords.size / words.length;

            // If they write a long text but keep repeating a tiny set of words (e.g., under 35% unique words)
            if (words.length >= 10 && uniqueRatio < 0.35) {
                return { isSpam: true, reason: "Highly repetitive text. Please provide an organic, descriptive review." };
            }

            // 3. Check if any single word takes up more than 25% of the entire review
            const frequencies = {};
            for (const w of words) {
                frequencies[w] = (frequencies[w] || 0) + 1;
            }
            for (const w in frequencies) {
                const ratio = frequencies[w] / words.length;
                if (words.length >= 15 && ratio > 0.25) {
                    return { isSpam: true, reason: "The word '" + w + "' is repeated excessively (" + Math.round(ratio * 100) + "% of the text)." };
                }
            }

            return { isSpam: false };
        };

        // Helper to update button state & live validation style
        const updateValidationState = () => {
            const text = reviewInput ? reviewInput.value : '';
            const wordCount = getWordCount(text);
            const spamCheck = checkSpamText(text);

            // Update word counter element
            if (wordCounter) {
                wordCounter.textContent = `${wordCount} / 60 words`;
                if (wordCount >= 60 && !spamCheck.isSpam) {
                    wordCounter.className = 'font-weight-bold small text-success';
                } else {
                    wordCounter.className = 'font-weight-bold small text-danger';
                }
            }

            const isRatingValid = selectedRating > 0;
            const isTextValid = wordCount >= 60 && !spamCheck.isSpam;
            const isValid = isRatingValid && isTextValid;

            if (phoneWarning) {
                phoneWarning.style.display = spamCheck.isPhone ? 'block' : 'none';
            }

            if (submitBtn) {
                if (isValid) {
                    submitBtn.removeAttribute('disabled');
                    submitBtn.style.background = '#d8456b';
                    submitBtn.style.color = 'white';
                    submitBtn.style.cursor = 'pointer';
                    submitBtn.style.opacity = '1';
                    if (validationWarning) {
                        validationWarning.style.display = 'none';
                    }
                } else {
                    submitBtn.setAttribute('disabled', 'true');
                    submitBtn.style.background = '#cbd5e1';
                    submitBtn.style.color = '#94a3b8';
                    submitBtn.style.cursor = 'not-allowed';
                    submitBtn.style.opacity = '1';

                    if (validationWarning) {
                        validationWarning.style.display = 'block';
                        if (spamCheck.isSpam) {
                            validationWarning.textContent = spamCheck.isPhone ? '* Phone numbers are not allowed.' : `* Spam detected: ${spamCheck.reason}`;
                        } else if (!isRatingValid && wordCount < 60) {
                            validationWarning.textContent = '* Minimum 60 words and a star rating are required to submit.';
                        } else if (!isRatingValid) {
                            validationWarning.textContent = '* Please select a star rating.';
                        } else {
                            validationWarning.textContent = `* Review is too short. You need ${60 - wordCount} more word(s).`;
                        }
                    }
                }
            }
        };

        // Bind star hover & click
        const ratingLabels = { 1: "Poor", 2: "Fair", 3: "Good", 4: "Very Good", 5: "Excellent" };

        stars.forEach(star => {
            star.onmouseover = function () {
                const val = parseInt(this.getAttribute('data-value'));
                stars.forEach(s => {
                    if (parseInt(s.getAttribute('data-value')) <= val) {
                        s.style.color = '#fbbf24';
                    } else {
                        s.style.color = '#e2e8f0';
                    }
                });
                if (ratingText) ratingText.textContent = ratingLabels[val];
            };

            star.onmouseout = function () {
                stars.forEach(s => {
                    if (parseInt(s.getAttribute('data-value')) <= selectedRating) {
                        s.style.color = '#fbbf24';
                    } else {
                        s.style.color = '#e2e8f0';
                    }
                });
                if (ratingText) ratingText.textContent = selectedRating > 0 ? ratingLabels[selectedRating] : '';
            };

            star.onclick = function () {
                selectedRating = parseInt(this.getAttribute('data-value'));
                updateValidationState();
            };
        });

        // Bind input event to textarea
        if (reviewInput) {
            reviewInput.addEventListener('input', updateValidationState);
        }

        // Clone button to remove old listeners and refer to active DOM element
        if (submitBtn) {
            const freshBtn = submitBtn.cloneNode(true);
            submitBtn.parentNode.replaceChild(freshBtn, submitBtn);
            submitBtn = freshBtn;

            submitBtn.addEventListener('click', () => {
                const reviewText = reviewInput ? reviewInput.value.trim() : '';
                const modalEl = document.getElementById('rateOrderModal');
                const modalInstance = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
                if (modalInstance) modalInstance.hide();
                onSubmit(order.id, selectedRating, reviewText, selectedMediaFiles);
            });
        }

        // Set initial validation state
        updateValidationState();

        // Show Modal
        const modalEl = document.getElementById('rateOrderModal');
        if (modalEl) {
            const modal = bootstrap.Modal.getInstance(modalEl) || new bootstrap.Modal(modalEl);
            modal.show();
        }
    }
}
