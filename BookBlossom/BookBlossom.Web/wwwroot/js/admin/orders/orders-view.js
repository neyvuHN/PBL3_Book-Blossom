/**
 * Frontend MVC - View
 * Manages the DOM, dynamic rendering, active status toggles, track bars, and modals.
 * Configured for real backend data binding.
 */
class OrdersView {
    constructor() {
        // Core Tab Navigation Elements
        this.tabNav = document.getElementById('orderTabNav');
        this.tabItems = document.querySelectorAll('.admin-tab-item');
        
        // Content Area Containers
        this.ordersContentArea = document.getElementById('ordersContentArea');
        this.returnsContentArea = document.getElementById('returnsContentArea');
        this.complaintsContentArea = document.getElementById('complaintsContentArea');

        // Filter and Search Inputs
        this.searchInput = document.getElementById('orderSearchInput');
        this.typeFilter = document.getElementById('orderTypeFilter');
        this.kpiFilter = document.getElementById('kpiFilter');
        this.filtersArea = document.getElementById('ordersFiltersArea');

        // Batch Action Panel Elements
        this.batchActionBar = document.getElementById('batchActionBar');
        this.selectAllBatchCheck = document.getElementById('selectAllBatchCheck');
        this.batchSelectedCountText = document.getElementById('batchSelectedCountText');
        this.btnBatchActionSubmit = document.getElementById('btnBatchActionSubmit');
        this.btnBatchActionCancel = document.getElementById('btnBatchActionCancel');

        // Tab Badges & Indicators
        this.badges = {
            all: document.getElementById('badge-all'),
            pending: document.getElementById('badge-pending'),
            toship: document.getElementById('badge-toship'),
            intransit: document.getElementById('badge-intransit'),
            completed: document.getElementById('badge-completed'),
            returns: document.getElementById('badge-returns'),
            complaints: document.getElementById('badge-complaints')
        };
        this.pendingRedDot = document.getElementById('pending-red-dot');

        // Modal Selectors (Print Label)
        this.printInvoiceModal = document.getElementById('printInvoiceModal');
        this.btnClosePrintModal = document.getElementById('btnClosePrintModal');
        this.btnCancelPrint = document.getElementById('btnCancelPrint');
        this.btnConfirmNativePrint = document.getElementById('btnConfirmNativePrint');
        this.invoicePrintArea = document.getElementById('invoicePrintArea');

        // Modal Selectors (Export Manifest)
        this.exportListModal = document.getElementById('exportListModal');
        this.btnCloseExportModal = document.getElementById('btnCloseExportModal');
        this.btnCancelExport = document.getElementById('btnCancelExport');
        this.btnTriggerPdfDownload = document.getElementById('btnTriggerPdfDownload');
        this.exportPdfPreviewArea = document.getElementById('exportPdfPreviewArea');

        // Top Actions
        this.btnExportPdfList = document.getElementById('btnExportPdfList');
        this.btnBatchConfirmHeader = document.getElementById('btnBatchConfirm');

        // Premium Custom Notifications & Confirmation Selectors
        this.toastContainer = document.getElementById('toastContainer');
        this.confirmOverlay = document.getElementById('confirmDialogOverlay');
        this.confirmAlertIcon = document.getElementById('confirmAlertIcon');
        this.confirmTitle = document.getElementById('confirmTitle');
        this.confirmMessage = document.getElementById('confirmMessage');
        this.btnConfirmNo = document.getElementById('btnConfirmNo');
        this.btnConfirmYes = document.getElementById('btnConfirmYes');

        // Modal Selectors (Chat Modal)
        this.chatModal = document.getElementById('chatModal');
        this.btnCloseChatModal = document.getElementById('btnCloseChatModal');
        this.chatHeaderAvatar = document.getElementById('chatHeaderAvatar');
        this.chatHeaderName = document.getElementById('chatHeaderName');
        this.chatSidebarAvatar = document.getElementById('chatSidebarAvatar');
        this.chatSidebarName = document.getElementById('chatSidebarName');
        this.chatSidebarOrders = document.getElementById('chatSidebarOrders');
        this.chatMessagesArea = document.getElementById('chatMessagesArea');
        
        // Gallery Modal
        this.galleryModal = document.getElementById('galleryModal');
        this.btnCloseGallery = document.getElementById('btnCloseGallery');
        this.galleryPreviewImage = document.getElementById('galleryPreviewImage');
        this.galleryPreviewVideo = document.getElementById('galleryPreviewVideo');
        this.galleryCaption = document.getElementById('galleryCaption');

        // Order Details Modal
        this.orderDetailsModal = document.getElementById('orderDetailsModal');
        this.btnCloseOrderDetailsModal = document.getElementById('btnCloseOrderDetailsModal');
        this.btnCancelOrderDetails = document.getElementById('btnCancelOrderDetails');
    }

    /**
     * Swaps the visible screen panel based on active navigation tab.
     */
    showTab(tabName) {
        this.tabItems.forEach(tab => {
            if (tab.getAttribute('data-tab') === tabName) {
                tab.classList.add('active');
            } else {
                tab.classList.remove('active');
            }
        });

        // Toggle layout views
        if (tabName === 'returns') {
            this.ordersContentArea.style.display = 'none';
            this.returnsContentArea.style.display = 'block';
            this.complaintsContentArea.style.display = 'none';
            this.filtersArea.style.display = 'none';
            this.batchActionBar.style.display = 'none';
        } else if (tabName === 'complaints') {
            this.ordersContentArea.style.display = 'none';
            this.returnsContentArea.style.display = 'none';
            this.complaintsContentArea.style.display = 'block';
            this.filtersArea.style.display = 'none';
            this.batchActionBar.style.display = 'none';
        } else {
            this.ordersContentArea.style.display = 'block';
            this.returnsContentArea.style.display = 'none';
            this.complaintsContentArea.style.display = 'none';
            this.filtersArea.style.display = 'flex';
            // Show or hide KPI risk filter depending if Pending is selected
            if (tabName === 'pending') {
                this.kpiFilter.style.display = 'inline-block';
            } else {
                this.kpiFilter.style.display = 'none';
            }
        }
    }

    /**
     * Helper to render connecting progress bars and step dots based on order state.
     */
    _getProgressTrackerHtml(order) {
        let fillWidth = '0%';
        let step1Class = ''; // Confirmed
        let step2Class = ''; // Preparing
        let step3Class = ''; // In Transit
        let step4Class = ''; // Delivered

        if (order.status === 'Pending Confirmation') {
            fillWidth = '0%';
            step1Class = '';
        } else if (order.status === 'To Ship') {
            step1Class = 'completed';
            if (order.subStatus === 'Packing') {
                fillWidth = '16%';
                step2Class = 'active';
            } else {
                fillWidth = '33%';
                step2Class = 'completed';
                step3Class = 'active';
            }
        } else if (order.status === 'In Transit') {
            step1Class = 'completed';
            step2Class = 'completed';
            
            if (order.subStatus === 'In Transit') {
                fillWidth = '66%';
                step3Class = 'active';
            } else if (order.subStatus === 'Delivering') {
                fillWidth = '83%';
                step3Class = 'completed';
                step4Class = 'active';
            } else if (order.subStatus === 'Delivered') {
                fillWidth = '100%';
                step3Class = 'completed';
                step4Class = 'completed'; // Trỏ tới Delivered
            }
        } else if (order.status === 'Completed') {
            fillWidth = '100%';
            step1Class = 'completed';
            step2Class = 'completed';
            step3Class = 'completed';
            step4Class = 'completed';
        }

        return `
            <div class="progress-track-wrapper">
                <div class="progress-line-bg"></div>
                <div class="progress-line-fill" style="width: ${fillWidth};"></div>
                
                <div class="progress-step ${step1Class}">
                    <div class="step-dot"></div>
                    <span class="step-label">Confirmed</span>
                </div>
                <div class="progress-step ${step2Class}">
                    <div class="step-dot"></div>
                    <span class="step-label">Preparing</span>
                </div>
                <div class="progress-step ${step3Class}">
                    <div class="step-dot"></div>
                    <span class="step-label">In Transit</span>
                </div>
                <div class="progress-step ${step4Class}">
                    <div class="step-dot"></div>
                    <span class="step-label">Delivered</span>
                </div>
            </div>
        `;
    }

    /**
     * Helper to render order card footer actions dynamically.
     */
    _getFooterActionsHtml(order) {
        if (order.status === 'Pending Confirmation') {
            // Confirm warning deadline text
            const kpiWarningHtml = order.remainingHours > 0 
                ? `<div class="kpi-warning-tag" title="Confirm within 48h limit to avoid KPI penalty">
                     <i class="ph ph-alarm animate-pulse"></i> Confirm within 48h (Left: ${order.remainingTimeText})
                   </div>`
                : `<div class="kpi-warning-tag" style="background-color: rgba(235, 87, 87, 0.15);" title="KPI SLA breached!">
                     <i class="ph ph-warning-circle"></i> SLA Expired (KPI Penalized)
                   </div>`;

            return `
                ${kpiWarningHtml}
                <div class="footer-buttons">
                    <button class="btn-outline-action btn-cancel-order" data-id="${order.id}">Cancel</button>
                    <button class="btn-primary-action btn-confirm-order" data-id="${order.id}">Confirm Order</button>
                </div>
            `;
        } else if (order.status === 'To Ship') {
            return `
                <div style="font-size: 0.8rem; color: #2F80ED; font-weight:600; display:flex; align-items:center; gap:4px;">
                    <i class="ph ph-info"></i> Ready to package and release label
                </div>
                <div class="footer-buttons">
                    <button class="btn-outline-action btn-print-label" data-id="${order.id}">
                        <i class="ph ph-printer"></i> Print Invoice PDF
                    </button>
                    <button class="btn-primary-action toship btn-start-shipping" data-id="${order.id}">
                        Start Shipping
                    </button>
                </div>
            `;
        } else if (order.status === 'In Transit') {
            const isDeliveredWaiting = order.subStatus === 'Delivered';

            return `
                <div style="display:flex; align-items:center; gap:8px;">
                    <span style="font-size: 0.78rem; font-weight:600; color: #82758D;">Logistic Status Simulator:</span>
                    <select class="filter-select select-logistic-mock" data-id="${order.id}" style="padding: 4px 12px; font-size: 0.8rem;" ${isDeliveredWaiting ? 'disabled' : ''}>
                        <option value="In Transit" ${order.subStatus === 'In Transit' ? 'selected' : ''}>In Transit</option>
                        <option value="Delivering" ${order.subStatus === 'Delivering' ? 'selected' : ''}>Delivering</option>
                        <option value="Delivered" ${isDeliveredWaiting ? 'selected' : ''} disabled>Delivered (Waiting Buyer)</option>
                    </select>
                </div>
                <div class="footer-buttons">
                    <button class="btn-outline-action btn-print-label" data-id="${order.id}">
                        <i class="ph ph-printer"></i> Reprint Label
                    </button>
                    ${isDeliveredWaiting ? `
                    <button class="btn-primary-action" disabled style="background-color: #BDC3C7; cursor: not-allowed; color: #fff;">
                        Waiting for Buyer
                    </button>
                    ` : `
                    <button class="btn-primary-action btn-mark-delivered" data-id="${order.id}" style="background-color: #27AE60;">
                        Mark Delivered
                    </button>
                    `}
                </div>
            `;
        } else if (order.status === 'Completed') {
            return `
                <div style="color:#27AE60; font-weight: 700; font-size: 0.88rem; display:flex; align-items:center; gap:6px;">
                    <i class="ph ph-check-circle" style="font-size: 1.2rem;"></i> Order Completed & Funds Released
                </div>
                <div class="footer-buttons">
                    <button class="btn-outline-action btn-print-label" data-id="${order.id}">
                        <i class="ph ph-file-text"></i> View Invoice
                    </button>
                </div>
            `;
        } else {
            return `
                <div style="color:#EB5757; font-weight: 600; font-size: 0.88rem; display:flex; align-items:center; gap:6px;">
                    <i class="ph ph-warning-octagon"></i> Dispute / Refund Processing (${order.subStatus || 'Active'})
                </div>
                <div class="footer-buttons">
                    <button class="btn-outline-action btn-contact-dispute" data-id="${order.id}">Contact Buyer</button>
                </div>
            `;
        }
    }

    /**
     * Helper to render order card items.
     */
    _createOrderCardHtml(order, isSelected) {
        const checkedAttribute = isSelected ? 'checked' : '';
        const cardClass = order.status === 'Pending Confirmation' ? 'pending' :
                          order.status === 'To Ship' ? 'toship' :
                          order.status === 'In Transit' ? 'intransit' :
                          order.status === 'Completed' ? 'completed' : 'dispute';
        
        const badgeClass = order.status === 'Pending Confirmation' ? 'pending' :
                           order.status === 'To Ship' ? 'toship' :
                           order.status === 'In Transit' ? 'intransit' :
                           order.status === 'Completed' ? 'completed' : 'dispute';

        // Check for client-buyer special notes
        const noteBoxHtml = order.customerNote 
            ? `<div class="note-box">
                <i class="ph ph-notepad"></i> "${order.customerNote}"
               </div>`
            : `<div style="font-size: 0.8rem; color:#AFA5B8; font-style:italic;">No customer note attached</div>`;

        // Order Items details
        const items = order.fullDetail && order.fullDetail.orderItems ? order.fullDetail.orderItems : [];
        const itemsCount = items.length;

        let bookInfoHtml = '';
        if (itemsCount > 0) {
            const firstItem = items[0];
            const isBlind = firstItem.blindBookID != null;
            const coverImg = firstItem.sampleFilePath || '/images/Book/book1.jpg';
            const realTitle = isBlind ? (firstItem.realBookTitle || firstItem.title) : firstItem.title;
            const isbnText = firstItem.isbn || "N/A";

            bookInfoHtml = `
                <div class="book-details-col" style="display:flex; flex-direction:column; gap:8px;">
                    <div style="display:flex; gap:15px; align-items:flex-start;">
                        <div class="book-thumbnail-wrapper" style="position: relative;">
                            <img src="${coverImg}" alt="Book Cover" class="book-thumbnail" onerror="this.src='/images/Book/book1.jpg'" />
                        </div>
                        <div class="book-info-text">
                            ${isBlind ? `<div class="genre-tags" style="margin-bottom:4px;"><span class="genre-tag" style="background:#FFEbee; color:#C2185B; padding:2px 6px; border-radius:4px; font-size:0.65rem;">#BlindDate</span></div>` : ''}
                            <span class="book-title" style="font-weight:600; color:#2C2630; display:block;">${realTitle}</span>
                            <span class="book-qty" style="font-size:0.8rem; color:#82758D; display:block;">Quantity: x${firstItem.quantity}</span>
                            ${isBlind ? `
                                <div class="blind-date-warning" style="margin-top:4px; font-size:0.75rem; color:#E3597D;">
                                    <i class="ph ph-eye-slash"></i> <strong>Blind Date:</strong> DO NOT write title on package!
                                </div>
                            ` : `<span class="book-isbn" style="font-size:0.75rem; color:#AFA5B8; display:block;">ISBN: ${isbnText}</span>`}
                        </div>
                    </div>
                    ${itemsCount > 1 ? `
                        <div style="font-size:0.75rem; font-weight:600; color:#2F80ED; background:#E8F0FE; padding:4px 10px; border-radius:20px; display:inline-block; align-self:flex-start; margin-top:4px;">
                            + ${itemsCount - 1} other item(s) in this order
                        </div>
                    ` : ''}
                </div>
            `;
        } else {
            bookInfoHtml = `<div class="book-details-col"><span style="color:#82758D; font-style:italic;">No items found</span></div>`;
        }

        const formattedTotal = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(order.totalAmount);

        return `
            <div class="order-card ${cardClass}" data-order-id="${order.id}">
                <!-- Card Header -->
                <div class="order-header">
                    <div class="order-meta">
                        <input type="checkbox" class="order-select-chk" data-id="${order.id}" ${checkedAttribute} style="scale: 1.15; margin-right: 8px; cursor:pointer;" />
                        <span class="order-id">#${order.id}</span>
                        <span class="bullet">•</span>
                        <span class="order-time">${order.createdAt}</span>
                    </div>
                    <span class="status-badge ${badgeClass}">${order.status}</span>
                </div>
                
                <!-- Card Body -->
                <div class="order-body">
                    <!-- Book Details -->
                    ${bookInfoHtml}

                    <!-- Buyer Info -->
                    <div class="buyer-details-col">
                        <div class="buyer-profile">
                            <img src="${order.buyerAvatarUrl}" alt="${order.buyerName}" class="buyer-avatar" onerror="this.src='/images/Avatar/avatar1.jpg'" />
                            <span class="buyer-name">${order.buyerName}</span>
                            <button class="btn-icon-soft btn-chat-buyer" data-id="${order.id}" data-name="${order.buyerName}" data-avatar="${order.buyerAvatarUrl}" title="Chat with Buyer" style="margin-left: auto; width: 28px; height: 28px; font-size: 1rem;"><i class="ph ph-chat-circle-dots"></i></button>
                        </div>
                        ${noteBoxHtml}
                    </div>

                    <!-- Progress Tracker Graph -->
                    <div class="progress-tracker-col">
                        ${this._getProgressTrackerHtml(order)}
                    </div>
                </div>

                <!-- Card Footer -->
                <div class="order-footer">
                    <div class="order-total-block">
                        <span class="total-label">Total Amount</span>
                        <span class="total-amount">${formattedTotal}</span>
                        <span class="funds-escrow">${order.fundsStatus}</span>
                    </div>
                    <div class="footer-buttons">
                        ${this._getFooterActionsHtml(order)}
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Renders main list of order cards into content area.
     */
    renderOrders(orders, selectedOrderIds) {
        if (orders.length === 0) {
            this.ordersContentArea.innerHTML = `
                <div class="orders-empty-state">
                    <span>📦</span>
                    <h4>No Orders Found</h4>
                    <p>There are no active orders matching your filter selection or search query.</p>
                </div>
            `;
            return;
        }

        this.ordersContentArea.innerHTML = orders.map(order => 
            this._createOrderCardHtml(order, selectedOrderIds.has(order.id))
        ).join('');
    }

    /**
     * Open Video Player Modal for unbox video proof verification.
     */
    openVideoModal(videoUrl) {
        if (!this.galleryModal) return;
        this.galleryPreviewImage.style.display = 'none';
        this.galleryPreviewVideo.style.display = 'block';
        
        this.galleryPreviewVideo.innerHTML = `
            <video src="${videoUrl}" controls autoplay style="width:100%; border-radius:8px; outline:none; max-height:60vh;"></video>
        `;
        
        this.galleryCaption.textContent = "Unboxing Proof Evidence Video";
        this.galleryModal.style.display = 'flex';
    }

    /**
     * Display Order Details Modal
     */
    showOrderDetailsModal(orderDetail) {
        if (!this.orderDetailsModal) return;

        // Header info
        document.getElementById('detail-order-id').textContent = `#${orderDetail.orderID}`;

        // Shipping Info
        document.getElementById('detail-receiver').textContent = orderDetail.shipReceiverName || orderDetail.customerName || 'N/A';
        document.getElementById('detail-phone').textContent = orderDetail.shipPhoneNumber || orderDetail.customerPhoneNumber || 'N/A';
        document.getElementById('detail-address').textContent = orderDetail.shipDetailAddress || 'N/A';

        // Payment Info
        document.getElementById('detail-payment-method').textContent = orderDetail.paymentMethod === 0 ? "COD (Cash on Delivery)" : "Online Payment (VNPay)";
        document.getElementById('detail-note').textContent = orderDetail.note || 'No notes provided';

        // Order Items
        const itemsListContainer = document.getElementById('detail-items-list');
        const formattedItems = (orderDetail.orderItems || []).map(item => {
            const unitPrice = item.unitPrice || 0;
            const price = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(unitPrice);
            let imageHtml = `<img src="${item.sampleFilePath || '/images/Book/book1.jpg'}" alt="Book Cover" style="width: 50px; height: 65px; object-fit: cover; border-radius: 4px;" onerror="this.src='/images/Book/book1.jpg'" />`;
            let isBlind = item.blindBookID != null;
            // Removed the override to blind cover so the admin can see the real book image!

            // check vouchers
            let voucherHtml = '';
            if (item.voucherBreakdown) {
                try {
                    let parsedVouchers = JSON.parse(item.voucherBreakdown);
                    if (Array.isArray(parsedVouchers)) {
                        voucherHtml = parsedVouchers.map(v => 
                            `<div style="font-size: 0.7rem; color: #E3597D; margin-top: 2px;"><i class="fas fa-tag"></i> ${v.voucherCode || v.VoucherCode} (-${new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(v.discountValue || v.DiscountValue)})</div>`
                        ).join('');
                    }
                } catch (e) {
                    let splits = item.voucherBreakdown.split('|');
                    let fallbackHtml = '';
                    splits.forEach(s => {
                        let parts = s.split(':');
                        if (parts.length === 2) {
                            fallbackHtml += `<div style="font-size: 0.7rem; color: #E3597D; margin-top: 2px;"><i class="fas fa-tag"></i> ${parts[0]} (-${new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(parseFloat(parts[1]))})</div>`;
                        } else {
                            fallbackHtml += `<div style="font-size: 0.7rem; color: #E3597D; margin-top: 2px;"><i class="fas fa-tag"></i> ${s}</div>`;
                        }
                    });
                    voucherHtml = fallbackHtml;
                }
            }

            return `
                <div style="display: flex; gap: 15px; margin-bottom: 15px; padding-bottom: 15px; border-bottom: 1px dashed #E6E1EA;">
                    ${imageHtml}
                    <div style="flex: 1;">
                        <div style="font-weight: 600; font-size: 0.95rem; color: #2C2630;">${isBlind ? "Mystery Book" : item.title} ${isBlind ? `<span style="font-size:0.75rem; color:#82758D;">(Real: ${item.realBookTitle || item.title})</span>` : ''}</div>
                        <div style="font-size: 0.85rem; color: #82758D;">Qty: x${item.quantity}</div>
                        ${voucherHtml}
                    </div>
                    <div style="font-weight: 700; color: #2C2630;">${price}</div>
                </div>
            `;
        }).join('');
        itemsListContainer.innerHTML = formattedItems;

        // Calculate subtotal from items if possible
        let subtotalVal = 0;
        if (orderDetail.orderItems && orderDetail.orderItems.length > 0) {
            subtotalVal = orderDetail.orderItems.reduce((acc, item) => acc + ((item.unitPrice || 0) * item.quantity), 0);
        } else {
            subtotalVal = orderDetail.totalAmount; // Fallback
        }

        // Summary amounts
        const shipping = (orderDetail.shippingFee !== undefined && orderDetail.shippingFee !== null) ? orderDetail.shippingFee : 30000;
        const discountVal = (orderDetail.discountAmount !== undefined && orderDetail.discountAmount !== null) ? orderDetail.discountAmount : (orderDetail.discountValue || 0);

        document.getElementById('detail-subtotal').textContent = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(subtotalVal);
        document.getElementById('detail-shipping-fee').textContent = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(shipping);
        document.getElementById('detail-discount-val').textContent = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(discountVal);
        document.getElementById('detail-total').textContent = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(orderDetail.totalAmount);

        // Global Vouchers List
        const globalVouchersContainer = document.getElementById('detail-global-vouchers');
        if (orderDetail.appliedVouchers && orderDetail.appliedVouchers.length > 0) {
            globalVouchersContainer.innerHTML = orderDetail.appliedVouchers.map(v => 
                `<div style="display:inline-block; background:#ffebee; color:#C2185B; border:1px solid #f07c7c; padding:2px 6px; border-radius:4px; font-size:0.7rem; font-weight:bold; margin-right:4px; margin-top:2px;"><i class="fas fa-tag"></i> ${v}</div>`
            ).join('');
        } else {
            globalVouchersContainer.innerHTML = '';
        }

        this.orderDetailsModal.style.display = 'flex';
    }

    closeOrderDetailsModal() {
        if (this.orderDetailsModal) {
            this.orderDetailsModal.style.display = 'none';
        }
    }

    /**
     * Renders logistics returned/refunded items inside returned tab table body.
     */
    renderReturns(returnedItems) {
        if (returnedItems.length === 0) {
            this.returnsContentArea.innerHTML = `
                <div class="logistics-card">
                    <div class="logistics-title-desc">
                        <h4>Logistics & Returned Shipments</h4>
                        <p>Restock items returned by buyers after Moderator authorization</p>
                    </div>
                    <div class="orders-empty-state">
                        <span>🔄</span>
                        <h4>No Returned items</h4>
                        <p>There are currently no returned orders awaiting restocking operations.</p>
                    </div>
                </div>
            `;
            return;
        }

        const tableRowsHtml = returnedItems.map(item => {
            const isPending = item.restockStatus === 'Pending Restock';
            const actionHtml = isPending 
                ? `<div style="display:flex; justify-content:flex-end;">
                     <button class="btn-primary-action btn-approve-return" data-id="${item.id}" style="padding: 10px 24px; font-size:0.85rem; background-color:#E3597D; border-radius:25px !important; box-shadow:none; font-weight:600; display:inline-flex; align-items:center; gap:8px;">
                         <i class="ph ph-storefront" style="font-size:1.15rem;"></i> Restock Stock
                     </button>
                   </div>`
                : `<span style="color:#27AE60; font-weight:700; font-size:0.85rem; display:flex; align-items:center; gap:4px; justify-content:flex-end;">
                     <i class="ph ph-check-circle"></i> ${item.restockStatus}
                   </span>`;
            
            const badgeRestockClass = isPending ? 'pending' : (item.restockStatus === 'Restocked' ? 'completed' : 'dispute');
            const formattedRefund = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(item.refundAmount);

            return `
                <tr data-id="${item.id}">
                    <td style="font-weight:700; color:#E3597D;">#${item.id}</td>
                    <td style="font-weight:600;"><a href="javascript:void(0);" class="view-order-details-link" data-id="${item.orderId}" style="color: #2F80ED; text-decoration: underline;">#${item.orderId}</a></td>
                    <td>
                        <div style="font-weight:600; color:#2C2630;">${item.bookTitle}</div>
                        <div style="font-size:0.75rem; color:#82758D;">Qty: x${item.quantity}</div>
                    </td>
                    <td style="font-weight:700; color:#EB5757;">${formattedRefund}</td>
                    <td>
                        <span class="badge-decision">${item.moderatorDecision}</span>
                        <div style="font-size:0.75rem; color:#82758D; margin-top:2px;">Reason: "${item.returnReason}"</div>
                    </td>
                    <td>${item.transferredDate}</td>
                    <td>
                        <span class="badge-restock ${badgeRestockClass}">${item.restockStatus}</span>
                    </td>
                    <td style="text-align:right;">
                        ${actionHtml}
                    </td>
                </tr>
            `;
        }).join('');

        this.returnsContentArea.innerHTML = `
            <div class="logistics-card">
                <div class="logistics-title-desc">
                    <h4>Logistics & Returned Shipments</h4>
                    <p>Process stocking inflows for returned orders once Moderator decisions are finalized. Stock levels will adjust automatically.</p>
                </div>
                <div class="table-responsive" style="margin-top: 15px;">
                    <table class="premium-table">
                        <thead>
                            <tr>
                                <th>Ticket ID</th>
                                <th>Order ID</th>
                                <th>Item Details</th>
                                <th>Refund Total</th>
                                <th>Moderator Ruling</th>
                                <th>Date Escalated</th>
                                <th>Stock Status</th>
                                <th style="text-align:right;">Logistics Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${tableRowsHtml}
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    }

    /**
     * Renders escalated complaints transferred down by Moderator.
     */
    renderComplaints(complaints) {
        if (complaints.length === 0) {
            this.complaintsContentArea.innerHTML = `
                <div class="complaints-card">
                    <div class="complaints-title-desc">
                        <h4>Escalated Complaints & Reviews</h4>
                        <p>Respond to customer negative feedback or claims assigned by Moderator system</p>
                    </div>
                    <div class="orders-empty-state">
                        <span>💬</span>
                        <h4>No Escalated tickets</h4>
                        <p>No complaints or negative reviews currently require support mediation.</p>
                    </div>
                </div>
            `;
            return;
        }

        const tableRowsHtml = complaints.map(c => {
            const isPending = c.status === 'Pending';
            const actionHtml = isPending 
                ? `<div style="display:flex; gap:6px; justify-content:flex-end;">
                     <button class="btn-outline-action btn-contact-buyer" data-buyer="${c.buyerName}" style="padding: 6px 12px; font-size:0.75rem;">
                         Contact
                     </button>
                     <button class="btn-primary-action btn-resolve-complaint" data-id="${c.id}" style="padding: 6px 12px; font-size:0.75rem; background-color:#27AE60; box-shadow:none;">
                         Resolve
                     </button>
                   </div>`
                : `<span style="color:#27AE60; font-weight:700; font-size:0.85rem; display:flex; align-items:center; gap:4px; justify-content:flex-end;">
                     <i class="ph ph-check-circle"></i> Resolved & Closed
                   </span>`;
            
            const badgeTypeClass = c.type.toLowerCase().replace(' ', '-');
            const badgeStatusClass = isPending ? 'pending' : 'completed';

            // Star rating display helper
            let ratingHtml = 'N/A';
            if (c.type === 'Review') {
                const stars = Array.from({ length: 5 }, (_, i) => 
                    `<i class="ph-fill ph-star" style="font-size:0.8rem; color:${i < c.rating ? '#F2C94C' : '#E0DCE4'}"></i>`
                ).join('');
                ratingHtml = `<div class="rating-stars">${stars}</div>`;
            }

            return `
                <tr data-id="${c.id}">
                    <td style="font-weight:700; color:#EB5757;">#${c.id}</td>
                    <td style="font-weight:600;"><a href="javascript:void(0);" class="view-order-details-link" data-id="${c.orderId}" style="color: #2F80ED; text-decoration: underline;">#${c.orderId}</a></td>
                    <td>
                        <div style="font-weight:600; color:#2C2630;">${c.buyerName}</div>
                        <div style="font-size:0.75rem; color:#82758D;">${c.contactEmail}</div>
                    </td>
                    <td>
                        <span class="badge-complaint-type ${badgeTypeClass}">${c.type}</span>
                    </td>
                    <td>
                        ${ratingHtml}
                        <div style="font-size:0.8rem; font-style:italic; color:#2C2630; margin-top:4px;">"${c.content}"</div>
                    </td>
                    <td>
                        <div style="font-size:0.8rem; color:#2C2630; background:rgba(0,0,0,0.02); border-left:3px solid #B4A9C0; padding:6px 10px; border-radius:4px;">
                            <strong>Mod Note:</strong> ${c.moderatorNote}
                        </div>
                    </td>
                    <td>
                        <span class="badge-restock ${badgeStatusClass}">${c.status}</span>
                    </td>
                    <td>
                        ${actionHtml}
                    </td>
                </tr>
            `;
        }).join('');

        this.complaintsContentArea.innerHTML = `
            <div class="complaints-card">
                <div class="complaints-title-desc">
                    <h4>Escalated Complaints & Reviews</h4>
                    <p>Review customer claims or negative ratings transferred from Moderator system. Initiate direct outreach or adjust scores to resolve.</p>
                </div>
                <div class="table-responsive" style="margin-top: 15px;">
                    <table class="premium-table">
                        <thead>
                            <tr>
                                <th>Ticket ID</th>
                                <th>Order ID</th>
                                <th>Buyer Info</th>
                                <th>Type</th>
                                <th>Rating & Feedback</th>
                                <th>Moderator Context</th>
                                <th>Support Status</th>
                                <th style="text-align:right;">Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${tableRowsHtml}
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    }

    /**
     * Updates counting badges inside tabs and sets red alert notification dots.
     */
    updateTabBadges(counts) {
        if (this.badges.all) this.badges.all.textContent = counts.all;
        if (this.badges.pending) this.badges.pending.textContent = counts.pending;
        if (this.badges.toship) this.badges.toship.textContent = counts.toship;
        if (this.badges.intransit) this.badges.intransit.textContent = counts.intransit;
        if (this.badges.completed) this.badges.completed.textContent = counts.completed;
        if (this.badges.returns) this.badges.returns.textContent = counts.returns;
        if (this.badges.complaints) this.badges.complaints.textContent = counts.complaints;

        // Pending critical warning dot
        if (counts.pending > 0) {
            if (this.pendingRedDot) this.pendingRedDot.style.display = 'inline-block';
            if (this.btnBatchConfirmHeader) {
                this.btnBatchConfirmHeader.style.display = 'inline-flex';
                this.btnBatchConfirmHeader.innerHTML = `<i class="ph ph-lightning"></i> Auto-Confirm Pending (${counts.pending})`;
            }
        } else {
            if (this.pendingRedDot) this.pendingRedDot.style.display = 'none';
            if (this.btnBatchConfirmHeader) this.btnBatchConfirmHeader.style.display = 'none';
        }
    }

    /**
     * Reveals or hides the bottom batch processing control panel.
     */
    updateBatchActionBar(selectedCount, totalInTab, allChecked) {
        if (selectedCount > 0) {
            this.batchActionBar.style.display = 'flex';
            this.batchSelectedCountText.textContent = `${selectedCount} order(s) selected for bulk processing`;
            
            // Adjust batch action label based on active tab
            const activeTab = document.querySelector('.admin-tab-item.active').getAttribute('data-tab');
            if (activeTab === 'pending') {
                this.btnBatchActionSubmit.textContent = 'Batch Confirm Orders';
                this.btnBatchActionSubmit.className = 'btn-batch-action primary';
                this.btnBatchActionSubmit.style.backgroundColor = '#27AE60';
            } else if (activeTab === 'toship') {
                this.btnBatchActionSubmit.textContent = 'Batch Start Shipping';
                this.btnBatchActionSubmit.className = 'btn-batch-action primary';
                this.btnBatchActionSubmit.style.backgroundColor = '#2F80ED';
            } else if (activeTab === 'intransit') {
                this.btnBatchActionSubmit.textContent = 'Batch Mark Delivered';
                this.btnBatchActionSubmit.className = 'btn-batch-action primary';
                this.btnBatchActionSubmit.style.backgroundColor = '#27AE60';
            } else {
                this.btnBatchActionSubmit.textContent = 'Export Invoices';
                this.btnBatchActionSubmit.className = 'btn-batch-action primary';
                this.btnBatchActionSubmit.style.backgroundColor = '#E3597D';
            }

            this.selectAllBatchCheck.checked = allChecked;
        } else {
            this.batchActionBar.style.display = 'none';
            this.selectAllBatchCheck.checked = false;
        }
    }

    /**
     * Reveals a downloadable PDF invoice preview.
     */
    openPrintLabelModal(order) {
        // PDF handles on a new tab via QuestPDF. Fallback if needed.
        window.open(`/api/order/store/${order.id}/invoice`, '_blank');
    }

    /**
     * Closes printable invoice modal.
     */
    closePrintLabelModal() {
        this.printInvoiceModal.style.display = 'none';
        this.invoicePrintArea.innerHTML = '';
    }

    /**
     * Opens modal panel showing simulated printable list PDF report.
     */
    openExportPdfModal(ordersList) {
        const rowsHtml = ordersList.map(order => {
            const formattedAmount = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(order.totalAmount);
            return `
                <tr>
                    <td style="font-family:monospace; font-weight:700; padding:10px; border-bottom:1px solid #E6E1EA;">#${order.id}</td>
                    <td style="padding:10px; border-bottom:1px solid #E6E1EA;">
                        <strong>${order.isBlindDate ? 'Mystery Blind Date' : order.bookTitle}</strong>
                        <div style="font-size:0.72rem; color:#82758D;">Buyer: ${order.buyerName}</div>
                    </td>
                    <td style="padding:10px; border-bottom:1px solid #E6E1EA; text-align:center;">${order.status}</td>
                    <td style="font-family:monospace; text-align:right; font-weight:700; padding:10px; border-bottom:1px solid #E6E1EA;">${formattedAmount}</td>
                </tr>
            `;
        }).join('');

        const sumTotal = ordersList.reduce((acc, curr) => acc + curr.totalAmount, 0);
        const formattedSum = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(sumTotal);

        this.exportPdfPreviewArea.innerHTML = `
            <div style="background-color: white; border:1px solid #D0C9D6; border-radius:10px; padding:25px; font-family: 'Inter', sans-serif;">
                <div style="display:flex; justify-content:space-between; align-items:center; border-bottom:2px solid #E3597D; padding-bottom:12px; margin-bottom:15px;">
                    <div>
                        <h4 style="font-family:'Lora', serif; font-weight:700; margin:0; color:#2C2630;">Book Blossom Manifest</h4>
                        <div style="font-size:0.72rem; color:#82758D;">Generated on ${new Date().toLocaleString()} | User: Admin Stella</div>
                    </div>
                    <span style="font-size:2rem;">🌸</span>
                </div>
                
                <table style="width:100%; border-collapse:collapse; font-size:0.85rem;">
                    <thead>
                        <tr style="background-color:#FAF9FB; text-align:left; font-weight:700;">
                            <th style="padding:10px; border-bottom:2px solid #E6E1EA;">Order ID</th>
                            <th style="padding:10px; border-bottom:2px solid #E6E1EA;">Item Details</th>
                            <th style="padding:10px; border-bottom:2px solid #E6E1EA; text-align:center;">Status</th>
                            <th style="padding:10px; border-bottom:2px solid #E6E1EA; text-align:right;">Subtotal</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${rowsHtml}
                        <tr style="font-weight:800; background-color:#FAF9FB;">
                            <td colspan="3" style="padding:15px 10px; text-align:right; font-size:0.9rem;">SUM TOTAL:</td>
                            <td style="padding:15px 10px; text-align:right; font-size:0.95rem; font-family:monospace; color:#E3597D;">${formattedSum}</td>
                        </tr>
                    </tbody>
                </table>
                <div style="text-align:center; font-size:0.72rem; color:#92869E; margin-top:20px; font-style:italic;">
                    Internal Book Blossom Logistics Document. Confidential.
                </div>
            </div>
        `;

        this.exportListModal.style.display = 'flex';
    }

    /**
     * Closes the export PDF modal.
     */
    closeExportPdfModal() {
        this.exportListModal.style.display = 'none';
        this.exportPdfPreviewArea.innerHTML = '';
    }

    /**
     * Triggers a highly aesthetic, premium toast notification that auto-decays in 4 seconds.
     */
    showToast(title, desc, type = 'success') {
        if (!this.toastContainer) return;

        // Select correct Phosphor Icon based on category
        let iconHtml = '<i class="ph-fill ph-check-circle"></i>';
        if (type === 'info') iconHtml = '<i class="ph-fill ph-info"></i>';
        if (type === 'warning') iconHtml = '<i class="ph-fill ph-warning"></i>';
        if (type === 'error') iconHtml = '<i class="ph-fill ph-x-circle"></i>';

        // Build premium toast DOM structure
        const toast = document.createElement('div');
        toast.className = `premium-toast ${type}`;
        toast.innerHTML = `
            <div class="toast-wrapper">
                <div class="toast-icon">${iconHtml}</div>
                <div class="toast-message-block">
                    <span class="toast-title">${title}</span>
                    <span class="toast-desc">${desc}</span>
                </div>
                <button class="btn-close-toast">&times;</button>
            </div>
            <div class="toast-progress">
                <div class="toast-progress-bar">
                    <div class="toast-progress-bar-fill"></div>
                </div>
            </div>
        `;

        this.toastContainer.appendChild(toast);

        // Bind close button click for immediate removal
        const btnClose = toast.querySelector('.btn-close-toast');
        const dismissToast = () => {
            toast.classList.add('fade-out');
            setTimeout(() => {
                if (toast.parentNode === this.toastContainer) {
                    this.toastContainer.removeChild(toast);
                }
            }, 300);
        };

        if (btnClose) {
            btnClose.addEventListener('click', dismissToast);
        }

        // Automatic dismissal after 4s (matching decay progress line animation)
        setTimeout(dismissToast, 4000);
    }

    /**
     * Opens a gorgeous, glassmorphic modal for visual confirmations.
     */
    showConfirmDialog(title, message, type = 'warning', onYes = null, onNo = null) {
        if (!this.confirmOverlay) return;

        // Customize overlay details dynamically
        this.confirmTitle.textContent = title;
        this.confirmMessage.textContent = message;

        // Customise icon based on warning severity
        let iconHtml = '<i class="ph-fill ph-warning"></i>';
        if (type === 'info') iconHtml = '<i class="ph-fill ph-info"></i>';
        if (type === 'success') iconHtml = '<i class="ph-fill ph-check-circle"></i>';
        if (type === 'error') iconHtml = '<i class="ph-fill ph-x-circle"></i>';

        this.confirmAlertIcon.className = `confirm-alert-icon ${type}`;
        this.confirmAlertIcon.innerHTML = iconHtml;

        // Clean any old event listeners by cloning button targets
        const newBtnNo = this.btnConfirmNo.cloneNode(true);
        const newBtnYes = this.btnConfirmYes.cloneNode(true);
        this.btnConfirmNo.parentNode.replaceChild(newBtnNo, this.btnConfirmNo);
        this.btnConfirmYes.parentNode.replaceChild(newBtnYes, this.btnConfirmYes);
        this.btnConfirmNo = newBtnNo;
        this.btnConfirmYes = newBtnYes;

        // Adjust text and styling for the confirmation button
        if (type === 'warning' || type === 'error') {
            this.btnConfirmYes.style.backgroundColor = '#EB5757';
        } else if (type === 'success') {
            this.btnConfirmYes.style.backgroundColor = '#27AE60';
        } else {
            this.btnConfirmYes.style.backgroundColor = '#2F80ED';
        }

        // Show Dialog
        this.confirmOverlay.style.display = 'flex';

        // Close functions
        const closeDialog = () => {
            this.confirmOverlay.style.display = 'none';
        };

        this.btnConfirmNo.addEventListener('click', () => {
            closeDialog();
            if (onNo) onNo();
        });

        this.btnConfirmYes.addEventListener('click', () => {
            closeDialog();
            if (onYes) onYes();
        });
    }

    /**
     * Opens the Chat Modal and populates buyer info.
     */
    openChatModal(buyerName, buyerAvatarUrl) {
        if (!this.chatModal) return;

        // Populate header and sidebar details
        this.chatHeaderName.textContent = buyerName || 'Customer';
        this.chatSidebarName.textContent = buyerName || 'Customer';
        
        const avatar = buyerAvatarUrl || 'https://i.pravatar.cc/150?img=9';
        this.chatHeaderAvatar.src = avatar;
        this.chatSidebarAvatar.src = avatar;
        
        // Randomize order stats for visual flavor
        this.chatSidebarOrders.textContent = Math.floor(Math.random() * 10) + 1;

        // Open Modal
        this.chatModal.style.display = 'flex';
        
        // Ensure scroll to bottom
        setTimeout(() => {
            if (this.chatMessagesArea) {
                this.chatMessagesArea.scrollTop = this.chatMessagesArea.scrollHeight;
            }
        }, 50);
    }

    /**
     * Closes the Chat Modal.
     */
    closeChatModal() {
        if (this.chatModal) {
            this.chatModal.style.display = 'none';
        }
    }
}
