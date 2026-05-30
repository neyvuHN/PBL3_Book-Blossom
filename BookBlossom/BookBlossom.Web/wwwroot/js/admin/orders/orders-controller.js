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
                        alert(`🎉 Order #${orderId} confirmed successfully! Action logged within 48h to prevent KPI penalties. This order has moved to "To Ship" status.`);
                        this.renderCurrentView();
                    }
                    return;
                }

                // C. Cancel Pending Order
                if (btnCancel) {
                    const orderId = btnCancel.getAttribute('data-id');
                    const confirmation = confirm(`Are you sure you want to cancel Order #${orderId}? Funds will be automatically refunded from escrow.`);
                    if (confirmation && this.model.cancelOrder(orderId)) {
                        alert(`Order #${orderId} was cancelled. Status updated to Refunded.`);
                        this.renderCurrentView();
                    }
                    return;
                }

                // D. Start Shipping (Packing -> In Transit)
                if (btnStartShip) {
                    const orderId = btnStartShip.getAttribute('data-id');
                    if (this.model.startShippingOrder(orderId)) {
                        alert(`🚚 Logistics carrier notified! Order #${orderId} status changed to "In Transit". Live tracking commenced.`);
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
                    if (this.model.updateMockStatus(orderId, 'Delivered')) {
                        alert(`✓ Order #${orderId} marked as DELIVERED. Customer notified, review period activated, escrow funds released.`);
                        this.renderCurrentView();
                    }
                    return;
                }
            });

            // G. Change Event Delegation for In Transit dropdowns
            this.view.ordersContentArea.addEventListener('change', (e) => {
                const selectLogistic = e.target.closest('.select-logistic-mock');
                if (selectLogistic) {
                    const orderId = selectLogistic.getAttribute('data-id');
                    const newSub = selectLogistic.value;
                    if (this.model.updateMockStatus(orderId, newSub)) {
                        alert(`Logistic simulator: Order #${orderId} status set to [${newSub}].`);
                        this.renderCurrentView();
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
                    if (item && this.model.restockItem(id)) {
                        alert(`📥 Stocking complete! ${item.quantity} unit(s) of "${item.bookTitle}" returned to stock. Inventory count incremented by ${item.quantity}.`);
                        this.renderCurrentView();
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
                    alert(`📧 Opening support portal to contact buyer at [${email}]. Supporting Ticket ref: #${ticketId}`);
                }

                if (btnResolve) {
                    const id = btnResolve.getAttribute('data-id');
                    if (this.model.resolveComplaint(id)) {
                        alert(`💬 Support ticket #${id} marked as RESOLVED. Resolution notes logged and archived.`);
                        this.renderCurrentView();
                    }
                }
            });
        }

        // --- 7. Modal Control Interactions ---
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
                    alert("No active orders found in the current view to export!");
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
                alert("📥 Simulating PDF download... 'Book_Blossom_Order_Manifest.pdf' downloaded successfully to your local machine.");
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
                    const confirmedCount = this.model.batchConfirmOrders(selectedIds);
                    alert(`✅ Batch operations processed: Approved & confirmed ${confirmedCount} pending orders! Checked items transitioned to 'To Ship'.`);
                } else if (activeTab === 'toship') {
                    const shippedCount = this.model.batchStartShippingOrders(selectedIds);
                    alert(`🚚 Batch operations processed: Started shipping ${shippedCount} orders! Handover documents issued to courier.`);
                } else if (activeTab === 'intransit') {
                    const deliveredCount = this.model.batchUpdateMockStatus(selectedIds, 'Delivered');
                    alert(`✓ Batch operations processed: Marked ${deliveredCount} orders as DELIVERED! Escrow release scheduled.`);
                } else {
                    // Export select orders
                    const ordersToExport = this.model.orders.filter(o => selectedIds.includes(o.id));
                    this.view.openExportPdfModal(ordersToExport);
                    this.model.clearOrderSelection();
                }

                this.renderCurrentView();
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
                const pendingIds = this.model.orders
                    .filter(o => o.status === 'Pending Confirmation')
                    .map(o => o.id);
                
                if (pendingIds.length === 0) return;

                const confirmedCount = this.model.batchConfirmOrders(pendingIds);
                alert(`⚡ Express KPI Protection Triggered! Automatically confirmed all ${confirmedCount} pending orders before the SLA threshold.`);
                this.renderCurrentView();
            });
        }
    }
}
