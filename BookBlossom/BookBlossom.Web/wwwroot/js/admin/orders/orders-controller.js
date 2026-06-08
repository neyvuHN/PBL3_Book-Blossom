/**
 * Frontend MVC - Controller
 * Binds DOM events, coordinates data flow between Model and View, and handles user interactions.
 * Connects Frontend Actions directly with Live Backend APIs.
 */
class OrdersController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
    }

    /**
     * Entry point: initializes data rendering and binds event handlers.
     */
    async init() {
        console.log("Orders MVC Controller initialized.");
        await this.loadAllData();
        this.bindEvents();
    }

    /**
     * Helper to load all dynamic data from database APIs.
     */
    async loadAllData() {
        try {
            await this.model.fetchAllDataFromApi();
            this.renderCurrentView();
        } catch (err) {
            this.view.showToast("Data Sync Error", err.message || "Failed to load dynamic data from APIs.", "error");
        }
    }

    /**
     * Calculates data sets, counts, and passes them to the view for rendering.
     */
    renderCurrentView() {
        const activeTab = this.model.activeTab;
        
        // 1. Get filtered lists
        const filteredOrders = this.model.getFilteredOrders();
        const filteredReturns = this.model.getFilteredReturns();
        const filteredComplaints = this.model.getFilteredComplaints();

        // 2. Render appropriate tab content
        this.view.showTab(activeTab);

        if (activeTab === 'returns') {
            this.view.renderReturns(filteredReturns);
        } else if (activeTab === 'complaints') {
            this.view.renderComplaints(filteredComplaints);
        } else {
            this.view.renderOrders(filteredOrders, this.model.selectedOrderIds);
        }

        // 3. Recalculate badge counts for all tabs
        const counts = {
            all: this.model.orders.length,
            pending: this.model.orders.filter(o => o.status === 'Pending Confirmation').length,
            toship: this.model.orders.filter(o => o.status === 'To Ship').length,
            intransit: this.model.orders.filter(o => o.status === 'In Transit').length,
            completed: this.model.orders.filter(o => o.status === 'Completed').length,
            returns: this.model.returnedItems.length,
            complaints: this.model.complaints.length
        };
        this.view.updateTabBadges(counts);

        // 4. Update batch actions bar based on selections
        const tabOrders = (activeTab === 'returns' || activeTab === 'complaints') ? [] : filteredOrders;
        const selectedCount = this.model.selectedOrderIds.size;
        const allChecked = tabOrders.length > 0 && this.model.isAllSelectedForTab(tabOrders);
        this.view.updateBatchActionBar(selectedCount, tabOrders.length, allChecked);
    }

    /**
     * Binds all DOM elements and dynamic container events using event delegation.
     */
    bindEvents() {
        // --- 1. Tab Navigation Clicks ---
        if (this.view.tabNav) {
            this.view.tabNav.addEventListener('click', (e) => {
                const tabItem = e.target.closest('.admin-tab-item');
                if (!tabItem) return;

                const selectedTab = tabItem.getAttribute('data-tab');
                this.model.activeTab = selectedTab;
                
                // Clear selected batch items when transitioning to avoid leaking states
                this.model.clearOrderSelection();
                
                this.renderCurrentView();
            });
        }

        // --- 2. Search & Dropdown Filters ---
        if (this.view.searchInput) {
            this.view.searchInput.addEventListener('input', (e) => {
                this.model.searchQuery = e.target.value.trim();
                this.renderCurrentView();
            });
        }

        if (this.view.typeFilter) {
            this.view.typeFilter.addEventListener('change', (e) => {
                this.model.filterType = e.target.value;
                this.renderCurrentView();
            });
        }

        if (this.view.kpiFilter) {
            this.view.kpiFilter.addEventListener('change', (e) => {
                this.model.filterKpi = e.target.value;
                this.renderCurrentView();
            });
        }

        // --- 3. Selection & Batch Checkboxes ---
        if (this.view.selectAllBatchCheck) {
            this.view.selectAllBatchCheck.addEventListener('change', (e) => {
                const activeTabOrders = this.model.getFilteredOrders();
                this.model.setAllSelectedForTab(activeTabOrders, e.target.checked);
                this.renderCurrentView();
            });
        }

        // --- 4. Dynamic Order List Button Event Delegation ---
        if (this.view.ordersContentArea) {
            this.view.ordersContentArea.addEventListener('click', async (e) => {
                const btnConfirm = e.target.closest('.btn-confirm-order');
                const btnCancel = e.target.closest('.btn-cancel-order');
                const btnStartShip = e.target.closest('.btn-start-shipping');
                const btnPrintLabel = e.target.closest('.btn-print-label');
                const btnMarkDelivered = e.target.closest('.btn-mark-delivered');
                const chkSelect = e.target.closest('.order-select-chk');
                const btnChatBuyer = e.target.closest('.btn-chat-buyer');

                if (btnChatBuyer) {
                    const buyerName = btnChatBuyer.getAttribute('data-name');
                    const buyerAvatarUrl = btnChatBuyer.getAttribute('data-avatar');
                    this.view.openChatModal(buyerName, buyerAvatarUrl);
                    return;
                }

                // A. Checkbox Selection Toggle
                if (chkSelect) {
                    const orderId = chkSelect.getAttribute('data-id');
                    this.model.toggleOrderSelection(orderId);
                    this.renderCurrentView();
                    return;
                }

                // Click on order card to view details
                const orderCard = e.target.closest('.order-card');
                const isActionClick = e.target.closest('.footer-buttons') || e.target.closest('.order-meta') || e.target.closest('.buyer-profile');
                if (orderCard && !isActionClick && !chkSelect && !btnChatBuyer && !btnConfirm && !btnCancel && !btnStartShip && !btnPrintLabel && !btnMarkDelivered) {
                    const orderId = orderCard.getAttribute('data-order-id');
                    this.view.showToast('Loading', 'Fetching order details...', 'info');
                    try {
                        const detail = await this.model.fetchOrderDetails(orderId);
                        if (detail) {
                            this.view.showOrderDetailsModal(detail);
                        } else {
                            this.view.showToast('Error', 'Order not found.', 'error');
                        }
                    } catch (err) {
                        this.view.showToast('Error', 'Failed to load order details.', 'error');
                    }
                    return;
                }

                // B. Confirm Pending Order
                if (btnConfirm) {
                    const orderId = btnConfirm.getAttribute('data-id');
                    this.view.showToast('Processing', `Confirming Order #${orderId}...`, 'info');
                    try {
                        await this.model.confirmOrder(orderId);
                        this.view.showToast(
                            'Order Confirmed', 
                            `Order #${orderId} has been successfully approved!`, 
                            'success'
                        );
                        await this.loadAllData();
                    } catch (err) {
                        this.view.showToast('Confirmation Failed', err.message, 'error');
                    }
                    return;
                }

                // C. Cancel Pending Order (With Custom Dialog)
                if (btnCancel) {
                    const orderId = btnCancel.getAttribute('data-id');
                    this.view.showConfirmDialog(
                        'Cancel Order?',
                        `Are you sure you want to cancel Order #${orderId}? Escrow funds will be automatically returned to the buyer.`,
                        'error',
                        async () => {
                            this.view.showToast('Cancelling', `Cancelling Order #${orderId}...`, 'info');
                            try {
                                await this.model.cancelOrder(orderId);
                                this.view.showToast('Order Cancelled', `Order #${orderId} was successfully cancelled.`, 'error');
                                await this.loadAllData();
                            } catch (err) {
                                this.view.showToast('Cancellation Failed', err.message, 'error');
                            }
                        }
                    );
                    return;
                }

                // D. Start Shipping (Packing -> In Transit)
                if (btnStartShip) {
                    const orderId = btnStartShip.getAttribute('data-id');
                    this.view.showToast('Shipping', `Starting shipping process for Order #${orderId}...`, 'info');
                    try {
                        await this.model.startShippingOrder(orderId);
                        this.view.showToast(
                            'Shipping Initiated', 
                            `Order #${orderId} handed over to logistics courier successfully!`, 
                            'info'
                        );
                        await this.loadAllData();
                    } catch (err) {
                        this.view.showToast('Shipping Failed', err.message, 'error');
                    }
                    return;
                }

                // E. Print Shipping Label (QuestPDF generation and rendering)
                if (btnPrintLabel) {
                    const orderId = btnPrintLabel.getAttribute('data-id');
                    const token = localStorage.getItem('accessToken');
                    
                    this.view.showToast("Generating PDF", "Rendering invoice with QuestPDF on server...", "info");
                    
                    const headers = {};
                    if (token) headers['Authorization'] = `Bearer ${token}`;
                    
                    fetch(`/api/order/store/${orderId}/invoice`, { headers })
                        .then(res => {
                            if (!res.ok) throw new Error("Failed to render invoice PDF.");
                            return res.blob();
                        })
                        .then(blob => {
                            const blobUrl = URL.createObjectURL(blob);
                            window.open(blobUrl, '_blank');
                            this.view.showToast("PDF Ready", "QuestPDF invoice loaded successfully.", "success");
                        })
                        .catch(err => {
                            this.view.showToast("PDF Rendering Error", err.message, "error");
                        });
                    return;
                }

                // F. Fast Mark Delivered
                if (btnMarkDelivered) {
                    const orderId = btnMarkDelivered.getAttribute('data-id');
                    this.view.showConfirmDialog(
                        'Mark as Delivered?',
                        `Mark Order #${orderId} as Delivered? The buyer will have 7 days to confirm receipt or request a return.`,
                        'success',
                        async () => {
                            this.view.showToast('Delivering', `Completing delivery for Order #${orderId}...`, 'info');
                            try {
                                await this.model.updateOrderStatusInDb(orderId, 'Delivered');
                                this.view.showToast(
                                    'Order Delivered', 
                                    `Order #${orderId} marked as Delivered. Waiting for buyer confirmation!`, 
                                    'success'
                                );
                                await this.loadAllData();
                            } catch (err) {
                                this.view.showToast('Delivery Completion Failed', err.message, 'error');
                            }
                        }
                    );
                    return;
                }
            });

            // G. Change Event Delegation for In Transit dropdowns
            this.view.ordersContentArea.addEventListener('change', async (e) => {
                const selectLogistic = e.target.closest('.select-logistic-mock');
                if (selectLogistic) {
                    const orderId = selectLogistic.getAttribute('data-id');
                    const newSub = selectLogistic.value;
                    
                    if (newSub === 'Delivered') {
                        // Forward to custom confirm
                        this.view.showConfirmDialog(
                            'Mark as Delivered?',
                            `Mark Order #${orderId} as Delivered? The buyer will have 7 days to confirm receipt.`,
                            'success',
                            async () => {
                                this.view.showToast('Delivering', 'Updating status on server...', 'info');
                                try {
                                    await this.model.updateOrderStatusInDb(orderId, 'Delivered');
                                    this.view.showToast('Order Delivered', `Order #${orderId} marked as Delivered. Waiting for buyer confirmation!`, 'success');
                                    await this.loadAllData();
                                } catch (err) {
                                    this.view.showToast('Update Failed', err.message, 'error');
                                    this.renderCurrentView();
                                }
                            },
                            () => {
                                this.renderCurrentView(); // Revert select value visually
                            }
                        );
                    } else {
                        this.view.showToast('Updating', 'Updating status on server...', 'info');
                        try {
                            await this.model.updateOrderStatusInDb(orderId, newSub);
                            this.view.showToast('Status Updated', `Logistics status for Order #${orderId} set to [${newSub}].`, 'info');
                            await this.loadAllData();
                        } catch (err) {
                            this.view.showToast('Update Failed', err.message, 'error');
                            this.renderCurrentView();
                        }
                    }
                }
            });
        }

        // --- 5. Return Logistics Tab Event Delegation ---
        if (this.view.returnsContentArea) {
            this.view.returnsContentArea.addEventListener('click', async (e) => {
                const btnApprove = e.target.closest('.btn-approve-return');
                const btnReject = e.target.closest('.btn-reject-return');
                const btnPlayVideo = e.target.closest('.btn-play-video');
                const orderLink = e.target.closest('.view-order-details-link');

                if (orderLink) {
                    const orderId = orderLink.getAttribute('data-id');
                    this.view.showToast('Loading', 'Fetching order details...', 'info');
                    try {
                        const detail = await this.model.fetchOrderDetails(orderId);
                        if (detail) {
                            this.view.showOrderDetailsModal(detail);
                        } else {
                            this.view.showToast('Error', 'Order not found.', 'error');
                        }
                    } catch (err) {
                        this.view.showToast('Error', 'Failed to load order details.', 'error');
                    }
                    return;
                }

                if (btnPlayVideo) {
                    const videoUrl = btnPlayVideo.getAttribute('data-video');
                    this.view.openVideoModal(videoUrl);
                    return;
                }

                if (btnApprove) {
                    const id = btnApprove.getAttribute('data-id');
                    const item = this.model.findReturnedItemById(id);
                    if (item) {
                        this.view.showConfirmDialog(
                            'Restock Returned Item?',
                            `Confirm warehouse restocking and refund payout for "${item.bookTitle}" (Qty: ${item.quantity})?`,
                            'success',
                            async () => {
                                this.view.showToast('Restocking', 'Processing return restocking & warehouse update...', 'info');
                                try {
                                    await this.model.restockReturn(id);
                                    this.view.showToast(
                                        'Item Restocked', 
                                        `Restock success! ${item.quantity} unit(s) of "${item.bookTitle}" returned to stock.`, 
                                        'success'
                                    );
                                    await this.loadAllData();
                                } catch (err) {
                                    this.view.showToast('Restock Failed', err.message, 'error');
                                }
                            }
                        );
                    }
                    return;
                }

                if (btnReject) {
                    const id = btnReject.getAttribute('data-id');
                    const item = this.model.findReturnedItemById(id);
                    if (item) {
                        const rejectReason = prompt("Please enter the reason for rejecting this refund request:", "Uploaded video proof is incomplete or missing.");
                        if (rejectReason === null) return; // cancelled prompt
                        
                        this.view.showToast('Rejecting', 'Processing rejection on server...', 'info');
                        try {
                            await this.model.reviewReturnRequest(id, false, rejectReason);
                            this.view.showToast('Return Rejected', `Refund request #${id} has been rejected.`, 'error');
                            await this.loadAllData();
                        } catch (err) {
                            this.view.showToast('Rejection Failed', err.message, 'error');
                        }
                    }
                    return;
                }
            });
        }

        // --- 6. Complaints & Reviews Tab Event Delegation ---
        if (this.view.complaintsContentArea) {
            this.view.complaintsContentArea.addEventListener('click', async (e) => {
                const btnContact = e.target.closest('.btn-contact-buyer');
                const btnResolve = e.target.closest('.btn-resolve-complaint');
                const btnReject = e.target.closest('.btn-reject-complaint');
                const orderLink = e.target.closest('.view-order-details-link');

                if (orderLink) {
                    const orderId = orderLink.getAttribute('data-id');
                    this.view.showToast('Loading', 'Fetching order details...', 'info');
                    try {
                        const detail = await this.model.fetchOrderDetails(orderId);
                        if (detail) {
                            this.view.showOrderDetailsModal(detail);
                        } else {
                            this.view.showToast('Error', 'Order not found.', 'error');
                        }
                    } catch (err) {
                        this.view.showToast('Error', 'Failed to load order details.', 'error');
                    }
                    return;
                }

                if (btnContact) {
                    const buyerName = btnContact.getAttribute('data-buyer');
                    window.location.href = '/Admin/Messages?buyer=' + encodeURIComponent(buyerName);
                    return;
                }

                if (btnResolve) {
                    const id = btnResolve.getAttribute('data-id');
                    this.view.showConfirmDialog(
                        'Resolve Complaint Ticket?',
                        `Resolve this support ticket #${id} and release transaction funds?`,
                        'success',
                        async () => {
                            this.view.showToast('Resolving', 'Resolving ticket on server...', 'info');
                            try {
                                await this.model.reviewReturnRequest(id, true);
                                this.view.showToast('Ticket Resolved', `Ticket #${id} resolved successfully.`, 'success');
                                await this.loadAllData();
                            } catch (err) {
                                this.view.showToast('Failed to Resolve', err.message, 'error');
                            }
                        }
                    );
                    return;
                }

                if (btnReject) {
                    const id = btnReject.getAttribute('data-id');
                    const rejectReason = prompt("Please enter the reason for rejecting this refund:", "Invalid refund claim.");
                    if (rejectReason === null) return; // cancelled prompt
                    
                    this.view.showToast('Rejecting', 'Processing rejection on server...', 'info');
                    try {
                        await this.model.reviewReturnRequest(id, false, rejectReason);
                        this.view.showToast('Refund Rejected', `Refund request #${id} has been rejected.`, 'error');
                        await this.loadAllData();
                    } catch (err) {
                        this.view.showToast('Rejection Failed', err.message, 'error');
                    }
                    return;
                }
            });
        }

        // --- 7. Modal Control Interactions ---
        if (this.view.btnCloseChatModal) {
            this.view.btnCloseChatModal.addEventListener('click', () => this.view.closeChatModal());
        }

        if (this.view.btnCloseOrderDetailsModal) {
            this.view.btnCloseOrderDetailsModal.addEventListener('click', () => this.view.closeOrderDetailsModal());
        }
        if (this.view.btnCancelOrderDetails) {
            this.view.btnCancelOrderDetails.addEventListener('click', () => this.view.closeOrderDetailsModal());
        }

        if (this.view.btnClosePrintModal) {
            this.view.btnClosePrintModal.addEventListener('click', () => this.view.closePrintLabelModal());
        }
        if (this.view.btnCancelPrint) {
            this.view.btnCancelPrint.addEventListener('click', () => this.view.closePrintLabelModal());
        }

        // B. Export List manifest modal triggers
        if (this.view.btnExportPdfList) {
            this.view.btnExportPdfList.addEventListener('click', () => {
                const activeOrders = this.model.getFilteredOrders();
                if (activeOrders.length === 0) {
                    this.view.showToast('Export Failed', 'There are no active orders in the current view to export.', 'error');
                    return;
                }
                this.view.openExportPdfModal(activeOrders);
            });
        }

        if (this.view.btnCloseExportModal) {
            this.view.btnCloseExportModal.addEventListener('click', () => this.view.closeExportPdfModal());
        }
        if (this.view.btnCancelExport) {
            this.view.btnCancelExport.addEventListener('click', () => this.view.closeExportPdfModal());
        }

        // Trigger merging QuestPDF manifest for export
        if (this.view.btnTriggerPdfDownload) {
            this.view.btnTriggerPdfDownload.addEventListener('click', () => {
                const selectedIds = Array.from(this.model.selectedOrderIds);
                if (selectedIds.length === 0) {
                    // Export all active orders
                    const activeOrders = this.model.getFilteredOrders();
                    selectedIds.push(...activeOrders.map(o => o.id));
                }
                if (selectedIds.length === 0) return;

                const token = localStorage.getItem('accessToken');
                const headers = { 'Content-Type': 'application/json' };
                if (token) headers['Authorization'] = `Bearer ${token}`;

                const rawIds = selectedIds.map(id => {
                    const o = this.model.findOrderById(id);
                    return o ? o.orderIDRaw : null;
                }).filter(id => id !== null);

                this.view.showToast("Generating PDF Manifest", "Merging invoices with QuestPDF...", "info");
                
                fetch('/api/order/store/invoices', {
                    method: 'POST',
                    headers: headers,
                    body: JSON.stringify(rawIds)
                })
                .then(res => {
                    if (!res.ok) throw new Error("Failed to merge invoices.");
                    return res.blob();
                })
                .then(blob => {
                    const blobUrl = URL.createObjectURL(blob);
                    window.open(blobUrl, '_blank');
                    this.view.showToast("Manifest Ready", "QuestPDF manifest opened in a new window.", "success");
                    this.view.closeExportPdfModal();
                    this.model.clearOrderSelection();
                    this.renderCurrentView();
                })
                .catch(err => {
                    this.view.showToast("Export Failed", err.message, "error");
                });
            });
        }

        // --- 8. Batch Action Panel Clicks ---
        if (this.view.btnBatchActionSubmit) {
            this.view.btnBatchActionSubmit.addEventListener('click', () => {
                const selectedIds = Array.from(this.model.selectedOrderIds);
                const activeTab = this.model.activeTab;
                
                if (selectedIds.length === 0) return;

                if (activeTab === 'pending') {
                    this.view.showConfirmDialog(
                        'Batch Confirm Orders?',
                        `Approve and confirm all ${selectedIds.length} selected orders in bulk? Checked items will transition to 'To Ship'.`,
                        'success',
                        async () => {
                            this.view.showToast('Confirming', `Confirming ${selectedIds.length} orders...`, 'info');
                            try {
                                const confirmedCount = await this.model.batchConfirmOrders(selectedIds);
                                this.view.showToast('Batch Confirmed', `Successfully approved and confirmed ${confirmedCount} orders!`, 'success');
                                await this.loadAllData();
                            } catch (err) {
                                this.view.showToast('Batch Failed', err.message, 'error');
                            }
                        }
                    );
                } else if (activeTab === 'toship') {
                    this.view.showConfirmDialog(
                        'Batch Start Shipping?',
                        `Issue shipping labels and start carrier delivery for all ${selectedIds.length} selected orders?`,
                        'info',
                        async () => {
                            this.view.showToast('Shipping', `Starting shipping for ${selectedIds.length} orders...`, 'info');
                            try {
                                const shippedCount = await this.model.batchStartShippingOrders(selectedIds);
                                this.view.showToast('Batch Shipped', `Successfully handed over ${shippedCount} orders to courier!`, 'info');
                                await this.loadAllData();
                            } catch (err) {
                                this.view.showToast('Batch Failed', err.message, 'error');
                            }
                        }
                    );
                } else if (activeTab === 'intransit') {
                    this.view.showConfirmDialog(
                        'Batch Mark Delivered?',
                        `Mark all ${selectedIds.length} selected transit orders as Delivered? Buyers will need to confirm receipt to complete the orders.`,
                        'success',
                        async () => {
                            this.view.showToast('Delivering', `Completing delivery for ${selectedIds.length} orders...`, 'info');
                            try {
                                const deliveredCount = await this.model.batchUpdateOrderStatusInDb(selectedIds, 'Delivered');
                                this.view.showToast('Batch Delivered', `Successfully completed delivery for ${deliveredCount} orders! Waiting for buyers confirmation.`, 'success');
                                await this.loadAllData();
                            } catch (err) {
                                this.view.showToast('Batch Failed', err.message, 'error');
                            }
                        }
                    );
                } else {
                    // Export selected orders
                    const ordersToExport = this.model.orders.filter(o => selectedIds.includes(o.id));
                    this.view.openExportPdfModal(ordersToExport);
                    this.model.clearOrderSelection();
                    this.renderCurrentView();
                }
            });
        }

        if (this.view.btnBatchActionCancel) {
            this.view.btnBatchActionCancel.addEventListener('click', () => {
                this.model.clearOrderSelection();
                this.renderCurrentView();
            });
        }

        // Header Auto-Confirm Click Shortcut
        if (this.view.btnBatchConfirmHeader) {
            this.view.btnBatchConfirmHeader.addEventListener('click', () => {
                const pendingOrders = this.model.orders.filter(o => o.status === 'Pending Confirmation');
                if (pendingOrders.length === 0) return;

                this.view.showConfirmDialog(
                    'Auto-Confirm All Pending?',
                    `Instantly approve all ${pendingOrders.length} pending orders?`,
                    'success',
                    async () => {
                        const ids = pendingOrders.map(o => o.id);
                        this.view.showToast('Confirming', `Confirming all ${ids.length} pending orders...`, 'info');
                        try {
                            const confirmedCount = await this.model.batchConfirmOrders(ids);
                            this.view.showToast('Auto-Confirm Complete', `Successfully approved ${confirmedCount} orders!`, 'success');
                            await this.loadAllData();
                        } catch (err) {
                            this.view.showToast('Confirmation Failed', err.message, 'error');
                        }
                    }
                );
            });
        }

        // Gallery Modal Closes
        if (this.view.btnCloseGallery) {
            this.view.btnCloseGallery.addEventListener('click', () => {
                this.view.galleryModal.style.display = 'none';
            });
        }
    }
}
