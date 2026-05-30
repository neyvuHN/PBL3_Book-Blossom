/**
 * Frontend MVC - Controller
 * Binds DOM events, coordinates data flow between Model and View, and handles user interactions.
 */
class OrdersController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
    }

    /**
     * Entry point: initializes data rendering and binds event handlers.
     */
    init() {
        console.log("Orders MVC Controller initialized.");
        this.renderCurrentView();
        this.bindEvents();
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
            this.view.ordersContentArea.addEventListener('click', (e) => {
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

                // B. Confirm Pending Order (Within 48h deadline)
                if (btnConfirm) {
                    const orderId = btnConfirm.getAttribute('data-id');
                    if (this.model.confirmOrder(orderId)) {
                        this.view.showToast(
                            'Order Confirmed', 
                            `Order #${orderId} has been successfully approved within the 48h SLA window to preserve shop KPI. Moved to 'To Ship'.`, 
                            'success'
                        );
                        this.renderCurrentView();
                    }
                    return;
                }

                // C. Cancel Pending Order (With Custom Dialog)
                if (btnCancel) {
                    const orderId = btnCancel.getAttribute('data-id');
                    this.view.showConfirmDialog(
                        'Cancel Order?',
                        `Are you sure you want to cancel Order #${orderId}? Escrow funds will be automatically returned to the buyer's balance.`,
                        'error',
                        () => {
                            if (this.model.cancelOrder(orderId)) {
                                this.view.showToast('Order Cancelled', `Order #${orderId} was successfully cancelled and buyer refunded.`, 'error');
                                this.renderCurrentView();
                            }
                        }
                    );
                    return;
                }

                // D. Start Shipping (Packing -> In Transit)
                if (btnStartShip) {
                    const orderId = btnStartShip.getAttribute('data-id');
                    if (this.model.startShippingOrder(orderId)) {
                        this.view.showToast(
                            'Shipping Initiated', 
                            `Order #${orderId} handed over to logistics. Courier notified and tracking links activated!`, 
                            'info'
                        );
                        this.renderCurrentView();
                    }
                    return;
                }

                // E. Print Shipping Label
                if (btnPrintLabel) {
                    const orderId = btnPrintLabel.getAttribute('data-id');
                    const order = this.model.findOrderById(orderId);
                    if (order) {
                        this.view.openPrintLabelModal(order);
                    }
                    return;
                }

                // F. Fast Mark Delivered
                if (btnMarkDelivered) {
                    const orderId = btnMarkDelivered.getAttribute('data-id');
                    this.view.showConfirmDialog(
                        'Mark as Delivered?',
                        `Mark Order #${orderId} as Delivered? This will clear transit states, trigger customer notification, and release escrow funds.`,
                        'success',
                        () => {
                            if (this.model.updateMockStatus(orderId, 'Delivered')) {
                                this.view.showToast(
                                    'Order Delivered', 
                                    `Order #${orderId} marked as Delivered. Escrow payout finalized!`, 
                                    'success'
                                );
                                this.renderCurrentView();
                            }
                        }
                    );
                    return;
                }
            });

            // G. Change Event Delegation for In Transit dropdowns
            this.view.ordersContentArea.addEventListener('change', (e) => {
                const selectLogistic = e.target.closest('.select-logistic-mock');
                if (selectLogistic) {
                    const orderId = selectLogistic.getAttribute('data-id');
                    const newSub = selectLogistic.value;
                    
                    if (newSub === 'Delivered') {
                        // Forward to custom confirm
                        this.view.showConfirmDialog(
                            'Mark as Delivered?',
                            `Mark Order #${orderId} as Delivered? This will release escrow funds.`,
                            'success',
                            () => {
                                if (this.model.updateMockStatus(orderId, 'Delivered')) {
                                    this.view.showToast('Order Delivered', `Order #${orderId} marked as Delivered. Escrow payout finalized!`, 'success');
                                    this.renderCurrentView();
                                }
                            },
                            () => {
                                this.renderCurrentView(); // Revert select value visually
                            }
                        );
                    } else {
                        if (this.model.updateMockStatus(orderId, newSub)) {
                            this.view.showToast('Status Updated', `Logistics status for Order #${orderId} set to [${newSub}].`, 'info');
                            this.renderCurrentView();
                        }
                    }
                }
            });
        }

        // --- 5. Return Logistics Tab Event Delegation ---
        if (this.view.returnsContentArea) {
            this.view.returnsContentArea.addEventListener('click', (e) => {
                const btnRestock = e.target.closest('.btn-restock');
                if (btnRestock) {
                    const id = btnRestock.getAttribute('data-id');
                    const item = this.model.findReturnedItemById(id);
                    if (item) {
                        this.view.showConfirmDialog(
                            'Process Return & Restock?',
                            `Confirm warehouse restocking for returned item "${item.bookTitle}" (Qty: ${item.quantity})? Stock levels will adjust automatically.`,
                            'info',
                            () => {
                                if (this.model.restockItem(id)) {
                                    this.view.showToast(
                                        'Inventory Restocked', 
                                        `Stocking complete! Inflowed ${item.quantity} unit(s) of "${item.bookTitle}" back to warehouse stock.`, 
                                        'success'
                                    );
                                    this.renderCurrentView();
                                }
                            }
                        );
                    }
                }
            });
        }

        // --- 6. Complaints & Reviews Tab Event Delegation ---
        if (this.view.complaintsContentArea) {
            this.view.complaintsContentArea.addEventListener('click', (e) => {
                const btnContact = e.target.closest('.btn-contact-buyer');
                const btnResolve = e.target.closest('.btn-resolve-complaint');

                if (btnContact) {
                    const email = btnContact.getAttribute('data-email');
                    const ticketId = btnContact.getAttribute('data-id');
                    
                    const row = btnContact.closest('tr');
                    const buyerNameElement = row.querySelector('div[style*="font-weight:600; color:#2C2630;"]');
                    const buyerName = buyerNameElement ? buyerNameElement.textContent : 'Customer';
                    
                    this.view.openChatModal(buyerName, null);
                }

                if (btnResolve) {
                    const id = btnResolve.getAttribute('data-id');
                    this.view.showConfirmDialog(
                        'Resolve Complaint?',
                        `Mark support ticket #${id} as resolved and closed? Escalation records will be archived.`,
                        'success',
                        () => {
                            if (this.model.resolveComplaint(id)) {
                                this.view.showToast('Ticket Resolved', `Support Ticket #${id} marked as Resolved and closed.`, 'success');
                                this.renderCurrentView();
                            }
                        }
                    );
                }
            });
        }

        // --- 7. Modal Control Interactions ---
        if (this.view.btnCloseChatModal) {
            this.view.btnCloseChatModal.addEventListener('click', () => this.view.closeChatModal());
        }

        // A. Print invoice modal closes
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

        if (this.view.btnTriggerPdfDownload) {
            this.view.btnTriggerPdfDownload.addEventListener('click', () => {
                this.view.showToast(
                    'Download Started', 
                    "Generating PDF manifest... 'Book_Blossom_Order_Manifest.pdf' downloaded successfully.", 
                    'success'
                );
                this.view.closeExportPdfModal();
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
                        () => {
                            const confirmedCount = this.model.batchConfirmOrders(selectedIds);
                            this.view.showToast('Batch Confirmed', `Successfully approved and confirmed ${confirmedCount} orders!`, 'success');
                            this.renderCurrentView();
                        }
                    );
                } else if (activeTab === 'toship') {
                    this.view.showConfirmDialog(
                        'Batch Start Shipping?',
                        `Issue shipping labels and start carrier delivery for all ${selectedIds.length} selected orders?`,
                        'info',
                        () => {
                            const shippedCount = this.model.batchStartShippingOrders(selectedIds);
                            this.view.showToast('Batch Shipped', `Successfully handed over ${shippedCount} orders to logistics courier!`, 'info');
                            this.renderCurrentView();
                        }
                    );
                } else if (activeTab === 'intransit') {
                    this.view.showConfirmDialog(
                        'Batch Mark Delivered?',
                        `Mark all ${selectedIds.length} selected transit orders as Delivered? This releases escrow funds.`,
                        'success',
                        () => {
                            const deliveredCount = this.model.batchUpdateMockStatus(selectedIds, 'Delivered');
                            this.view.showToast('Batch Delivered', `Successfully completed delivery and released funds for ${deliveredCount} orders!`, 'success');
                            this.renderCurrentView();
                        }
                    );
                } else {
                    // Export select orders
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
                    () => {
                        const ids = pendingOrders.map(o => o.id);
                        const confirmedCount = this.model.batchConfirmOrders(ids);
                        this.view.showToast('Auto-Confirm Complete', `Successfully approved ${confirmedCount} orders!`, 'success');
                        this.renderCurrentView();
                    }
                );
            });
        }
    }
}
