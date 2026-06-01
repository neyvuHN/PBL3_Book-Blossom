document.addEventListener('DOMContentLoaded', function () {
    // Checkout Logic
    const btnChangeAddr = document.getElementById('btn-change-shipping-addr');
    const checkoutModal = document.getElementById('checkout-modal-overlay');
    const checkoutContent = document.getElementById('checkout-modal-content');
    const btnCloseCheckout = document.getElementById('btn-close-checkout');
    const paymentMethods = document.querySelectorAll('.payment-method-card');
    const btnConfirmCheckout = document.getElementById('btn-confirm-checkout');

    if (!checkoutModal || !checkoutContent || !btnConfirmCheckout) {
        console.warn('Checkout modal DOM is missing. Please include _CheckoutModal.cshtml before checkout.js.');
        return;
    }

    const pmCod = document.getElementById('pm-cod');
    const codDisabledOverlay = document.getElementById('cod-disabled-overlay');

    if (pmCod && codDisabledOverlay) {
        if (window.apiClient) {
            window.apiClient.apiGet('/api/Reputation/my-reputation').then(rep => {
                let userReputationScore = 100; // Default safe
                if (rep && rep.points !== undefined) userReputationScore = rep.points;
                else if (rep && rep.Points !== undefined) userReputationScore = rep.Points;

                if (userReputationScore < 60) {
                    pmCod.style.pointerEvents = 'none';
                    pmCod.style.opacity = '0.7';
                    codDisabledOverlay.style.display = 'flex';
                    // Fallback to VNPay if COD is selected by default
                    if (document.querySelector('.payment-method-card.active')?.getAttribute('data-method') === 'cod') {
                        document.querySelector('[data-method="vnpay"]')?.click();
                    }
                } else {
                    codDisabledOverlay.style.display = 'none';
                }
            }).catch(e => {
                console.warn("Could not fetch reputation, COD allowed by default.");
                codDisabledOverlay.style.display = 'none';
            });
        }
    }

    // [UPDATED] openCheckout now resets the Order Note field on every fresh open
    function openCheckout() {
        checkoutModal.style.display = 'flex';
        // Trigger reflow for transition
        void checkoutModal.offsetWidth;
        checkoutContent.style.opacity = '1';
        checkoutContent.style.transform = 'scale(1)';

        // Reset the order note textarea so previous sessions don't bleed over
        const $note = document.getElementById('checkout-order-note');
        if ($note) $note.value = '';
    }

    function closeCheckout() {
        checkoutContent.style.opacity = '0';
        checkoutContent.style.transform = 'scale(0.95)';
        setTimeout(() => {
            checkoutModal.style.display = 'none';
        }, 300);
    }

    window.checkoutState = { subtotal: 297000, shippingFee: 30000, discount: 0 };

    function updateCheckoutTotals() {
        const total = Math.max(0, window.checkoutState.subtotal + window.checkoutState.shippingFee - window.checkoutState.discount);
        const fmt = (num) => new Intl.NumberFormat('vi-VN').format(num) + ' VND';

        document.getElementById('checkout-subtotal').innerText = fmt(window.checkoutState.subtotal);
        document.getElementById('checkout-shipping-fee').innerText = fmt(window.checkoutState.shippingFee);
        document.getElementById('checkout-voucher-discount').innerText = '-' + fmt(window.checkoutState.discount);
        document.getElementById('checkout-summary-total').innerText = fmt(total);

        if (document.getElementById('checkout-total-price')) {
            document.getElementById('checkout-total-price').innerText = fmt(total);
        }
    }

    // [UPDATED] Expose helpers including getOrderNote for page-level controllers
    window.openCheckout = openCheckout;
    window.closeCheckout = closeCheckout;
    window.updateCheckoutTotals = updateCheckoutTotals;

    // Returns the current value of the Order Note field (trimmed)
    window.getOrderNote = function () {
        const $note = document.getElementById('checkout-order-note');
        return $note ? $note.value.trim() : '';
    };

    // [UPDATED] Populator to dynamically render book information (single item details, or multiple items from cart)
    window.populateCheckoutBookInfo = function (items) {
        const $container = $('#checkout-book-info-container');
        $container.empty();

        if (items.length === 1) {
            const item = items[0];
            const itemPriceVnd = item.priceVnd ? item.priceVnd : (item.price * 20000);
            $container.css({
                'display': 'flex',
                'gap': '20px',
                'align-items': 'center',
                'flex-direction': 'row',
                'background': '#faf8f5',
                'padding': '20px',
                'border-radius': '12px',
                'border': '1px solid #f2ede4'
            });
            $container.append(`
    <img id="checkout-book-img" src="${item.img || '/images/Book/book4.jpg'}" style="width: 80px; height: 120px; object-fit: cover; border-radius: 6px; box-shadow: 0 4px 10px rgba(0,0,0,0.1);" alt="Book Cover">
        <div style="flex-grow: 1;">
            <h4 id="checkout-book-title" style="margin: 0 0 5px; font-size: 1.2rem; font-weight: 700; color: #333;">${item.title}</h4>
            <p id="checkout-book-author" style="margin: 0 0 10px; font-size: 0.9rem; color: #666;">${item.author || 'BookBlossom Edition'}</p>
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <span style="font-size: 0.95rem; color: #555;">Quantity: <strong id="checkout-book-qty">${item.qty}</strong></span>
                <span id="checkout-book-price" style="font-size: 1.25rem; font-weight: 700; color: #C2185B;">${new Intl.NumberFormat('vi-VN').format(itemPriceVnd * item.qty)} VND</span>
            </div>
        </div>
        `);
        } else {
            $container.css({
                'display': 'flex',
                'flex-direction': 'column',
                'gap': '15px',
                'align-items': 'stretch',
                'background': '#faf8f5',
                'padding': '20px',
                'border-radius': '12px',
                'border': '1px solid #f2ede4'
            });

            let itemsHtml = '';
            items.forEach(item => {
                const itemPriceVnd = item.priceVnd ? item.priceVnd : (item.price * 20000);
                itemsHtml += `
        <div style="display: flex; gap: 15px; align-items: center; border-bottom: 1px dashed #e2e8f0; padding-bottom: 12px; margin-bottom: 3px;">
            <img src="${item.img || '/images/Book/book4.jpg'}" style="width: 50px; height: 75px; object-fit: cover; border-radius: 4px; box-shadow: 0 2px 6px rgba(0,0,0,0.08);" alt="Book Cover">
                <div style="flex-grow: 1;">
                    <h5 style="margin: 0 0 3px; font-size: 0.95rem; font-weight: 700; color: #333; line-height: 1.3;">${item.title}</h5>
                    <p style="margin: 0 0 5px; font-size: 0.8rem; color: #666;">${item.author || 'BookBlossom Edition'}</p>
                    <div style="display: flex; justify-content: space-between; align-items: center;">
                        <span style="font-size: 0.82rem; color: #555;">Qty: <strong>${item.qty}</strong></span>
                        <span style="font-size: 0.95rem; font-weight: 700; color: #C2185B;">${new Intl.NumberFormat('vi-VN').format(itemPriceVnd * item.qty)} VND</span>
                    </div>
                </div>
        </div>
        `;
            });

            $container.html(`
        <div style="max-height: 220px; overflow-y: auto; padding-right: 5px;">
            ${itemsHtml}
        </div>
        `);
            $container.find('> div > div:last-child').css({
                'border-bottom': 'none',
                'padding-bottom': '0',
                'margin-bottom': '0'
            });
        }
    };

    // Selection logic for product detail vouchers
    const couponCards = document.querySelectorAll('.coupon-slider .coupon-card');
    couponCards.forEach(card => {
        card.addEventListener('click', function () {
            const checkmark = this.querySelector('.checkmark-badge');
            this.classList.toggle('selected');
            const isSelected = this.classList.contains('selected');

            if (isSelected) {
                checkmark.style.display = 'flex';
                this.style.border = '2px solid #C2185B';
                this.style.background = '#fffafb';
            } else {
                checkmark.style.display = 'none';
                this.style.border = this.dataset.code === 'ZALOPAY50' ? '1.5px dashed #2b6cb0' :
                    (this.dataset.code === 'MOMO12' ? '1.5px dashed #d53f8c' : '1.5px dashed #f07c7c');
                this.style.background = this.dataset.code === 'ZALOPAY50' ? '#fdfcff' :
                    (this.dataset.code === 'MOMO12' ? '#fffafc' : '#fffdfb');
            }
        });
    });

    // Reorder blind vouchers to put selected ones at the top/beginning of the list
    function sortBlindVouchersOnPage() {
        const container = document.querySelector('.blind-coupons-container');
        if (!container) return;

        const cards = Array.from(container.children);
        cards.sort((a, b) => {
            const aSel = a.classList.contains('selected') ? 1 : 0;
            const bSel = b.classList.contains('selected') ? 1 : 0;
            return bSel - aSel;
        });

        cards.forEach(card => container.appendChild(card));
    }

    // Selection logic for blind book vouchers
    $(document).on('click', '.blind-coupons-container .blind-coupon-card', function () {
        const checkmark = this.querySelector('.checkmark-badge-blind');
        this.classList.toggle('selected');
        const isSelected = this.classList.contains('selected');

        if (isSelected) {
            checkmark.style.display = 'flex';
            this.style.border = '2px solid #6a4f8c';
            this.style.background = '#f7f4fa';
        } else {
            checkmark.style.display = 'none';
            this.style.border = '1.5px dashed #a291b5';
            this.style.background = '#faf8fc';
        }

        // Immediately sort cards so selected ones jump to the beginning of the list
        sortBlindVouchersOnPage();
    });

    function getSelectedVouchers() {
        const selected = [];
        const isBlind = window.checkoutState && window.checkoutState.isBlind;

        if (isBlind) {
            // Only apply vouchers from the Blind Date Details page
            document.querySelectorAll('.blind-coupons-container .blind-coupon-card.selected').forEach(card => {
                selected.push({
                    code: card.dataset.code,
                    discount: parseInt(card.dataset.discount) || 0
                });
            });
        } else {
            // Only apply vouchers from the Regular Book Details page
            document.querySelectorAll('.coupon-slider .coupon-card.selected').forEach(card => {
                selected.push({
                    code: card.dataset.code,
                    discount: parseInt(card.dataset.discount) || 0
                });
            });
        }
        return selected;
    }

    function populateAppliedVouchers() {
        const selected = getSelectedVouchers();
        const container = document.getElementById('checkout-applied-vouchers-container');
        const list = document.getElementById('checkout-vouchers-list');

        list.innerHTML = '';
        if (selected.length > 0) {
            container.style.display = 'block';
            let totalDiscount = 0;
            selected.forEach(v => {
                totalDiscount += v.discount;
                const badge = document.createElement('div');
                const isMystery = v.code.startsWith('MYSTERY');
                const bg = isMystery ? '#faf8fc' : '#ffebee';
                const border = isMystery ? '1px solid #a291b5' : '1px solid #f07c7c';
                const textCol = isMystery ? '#6a4f8c' : '#C2185B';
                badge.style.cssText = `background: ${bg}; border: ${border}; color: ${textCol}; font-weight: 700; font-size: 0.8rem; padding: 4px 10px; border-radius: 6px; display: flex; align-items: center; gap: 5px;`;
                badge.innerHTML = `<i class="fas fa-ticket"></i> ${v.code} (-${new Intl.NumberFormat('vi-VN').format(v.discount)} VND)`;
                list.appendChild(badge);
            });
            window.checkoutState.discount = totalDiscount;
        } else {
            container.style.display = 'none';
            window.checkoutState.discount = 0;
        }
    }

    const addressModal = document.getElementById('address-modal-overlay');
    const addressContent = document.getElementById('address-modal-content');
    const btnCloseAddress = document.getElementById('btn-close-address');
    const btnCancelAddress = document.getElementById('btn-cancel-address');
    const btnSaveAddress = document.getElementById('btn-save-address');
    const btnChangeBlindAddr = document.getElementById('btn-change-blind-shipping');

    let activeAddressTargetId = 'detail-shipping-addr-text';

    function openAddressModal(targetId = 'detail-shipping-addr-text') {
        activeAddressTargetId = targetId;
        const currentAddrText = document.getElementById(targetId).innerText;
        document.getElementById('address-input-text').value = currentAddrText;

        const currentName = document.getElementById('checkout-name') ? document.getElementById('checkout-name').value : "John Doe";
        const currentPhone = document.getElementById('checkout-phone') ? document.getElementById('checkout-phone').value : "0987654321";
        document.getElementById('address-input-name').value = currentName;
        document.getElementById('address-input-phone').value = currentPhone;

        addressModal.style.display = 'flex';
        void addressModal.offsetWidth;
        addressContent.style.opacity = '1';
        addressContent.style.transform = 'scale(1)';
    }

    function closeAddressModal() {
        addressContent.style.opacity = '0';
        addressContent.style.transform = 'scale(0.95)';
        setTimeout(() => {
            addressModal.style.display = 'none';
        }, 300);
    }

    if (btnChangeAddr) {
        btnChangeAddr.addEventListener('click', function (e) {
            e.preventDefault();
            openAddressModal('detail-shipping-addr-text');
        });
    }

    if (btnChangeBlindAddr) {
        btnChangeBlindAddr.addEventListener('click', function (e) {
            e.preventDefault();
            openAddressModal('blind-detail-shipping-addr-text');
        });
    }

    if (btnCloseAddress) btnCloseAddress.addEventListener('click', closeAddressModal);
    if (btnCancelAddress) btnCancelAddress.addEventListener('click', closeAddressModal);

    if (addressModal) {
        addressModal.addEventListener('click', function (e) {
            if (e.target === addressModal) closeAddressModal();
        });
    }

    if (btnSaveAddress) {
        btnSaveAddress.addEventListener('click', function () {
            const newAddr = document.getElementById('address-input-text').value.trim();
            const newName = document.getElementById('address-input-name').value.trim();
            const newPhone = document.getElementById('address-input-phone').value.trim();

            if (!newAddr) {
                showToast("Please enter a valid shipping address!");
                return;
            }

            document.getElementById(activeAddressTargetId).innerText = newAddr;

            if (document.getElementById('checkout-address')) {
                document.getElementById('checkout-address').value = newAddr;
            }
            if (document.getElementById('checkout-name')) {
                document.getElementById('checkout-name').value = newName;
            }
            if (document.getElementById('checkout-phone')) {
                document.getElementById('checkout-phone').value = newPhone;
            }

            closeAddressModal();
            showToast("Shipping address updated successfully!");
        });
    }

    if (btnCloseCheckout) btnCloseCheckout.addEventListener('click', closeCheckout);

    // Close on overlay click
    checkoutModal.addEventListener('click', function (e) {
        if (e.target === checkoutModal) closeCheckout();
    });

    // Payment method selection
    let selectedMethod = 'vnpay';
    paymentMethods.forEach(card => {
        card.addEventListener('click', function () {
            if (this.style.pointerEvents === 'none') return; // disabled
            paymentMethods.forEach(c => {
                c.classList.remove('active');
                c.style.border = '1px solid #ddd';
                c.style.background = '#fff';
            });
            this.classList.add('active');
            this.style.border = '2px solid #C2185B';
            this.style.background = '#fffafb';
            selectedMethod = this.getAttribute('data-method');
        });
    });

    // [UPDATED] Confirm & Pay Flow – reads Order Note and attaches to checkoutState before processing
    btnConfirmCheckout.addEventListener('click', async function () {
        // Read the Order Note value and persist it on the shared checkout state
        const orderNote = window.getOrderNote ? window.getOrderNote() : '';
        window.checkoutState.orderNote = orderNote;

        // Collect address info
        const addressPayload = {
            receiverName: document.getElementById('checkout-name').value.trim() || 'Guest',
            phoneNumber: document.getElementById('checkout-phone').value.trim() || '0123456789',
            detailAddress: document.getElementById('checkout-address').value.trim() || 'No address',
            isDefault: false
        };

        // Determine CartItems
        let cartItemsDto = [];
        if (window.checkoutState.isCart) {
            const items = window.BookBlossomCart.getCartItems().filter(i => i.selected);
            cartItemsDto = items.map(i => ({
                bookID: i.isBlind ? null : (i.bookID || parseInt(i.id)),
                blindBookID: i.isBlind ? (i.blindBookID || parseInt(i.id)) : null,
                quantity: i.qty
            }));
        } else if (window.checkoutState.buyNowItem) { // Needs to be set in buy now
            cartItemsDto = [{
                bookID: window.checkoutState.buyNowItem.isBlind ? null : window.checkoutState.buyNowItem.bookID,
                blindBookID: window.checkoutState.buyNowItem.isBlind ? window.checkoutState.buyNowItem.blindBookID : null,
                quantity: window.checkoutState.buyNowItem.qty
            }];
        } else {
             // Fallback if not specified, try to extract from UI but we need ID.
             cartItemsDto = [];
        }

        const voucherCode = (window.checkoutState.appliedVouchers && window.checkoutState.appliedVouchers.length > 0) ? window.checkoutState.appliedVouchers[0].code : null;

        if (selectedMethod === 'vnpay') {
            closeCheckout();
            const loadingOverlay = document.getElementById('vnpay-loading-overlay');
            loadingOverlay.style.display = 'flex';

            try {
                // 1. Create Address
                const addressResp = await window.apiClient.apiPost('/api/Order/address', addressPayload);
                const addressId = addressResp.addressID || addressResp.AddressID || addressResp.addressId;

                // 2. Checkout
                const checkoutPayload = {
                    addressID: addressId,
                    paymentMethod: 1, // VNPay
                    voucherCode: voucherCode,
                    cartItems: cartItemsDto
                };

                const orderResp = await window.apiClient.apiPost('/api/Order/checkout', checkoutPayload);
                if (orderResp && orderResp.paymentUrl) {
                    window.location.href = orderResp.paymentUrl;
                } else {
                    loadingOverlay.style.display = 'none';
                    showToast('Payment URL not returned from server.', 'error');
                }
            } catch (error) {
                loadingOverlay.style.display = 'none';
                console.error("Checkout failed:", error);
                showToast(error.message || 'Checkout failed.', 'error');
            }

        } else {
            // COD Success Flow
            closeCheckout();
            const loadingOverlay = document.getElementById('vnpay-loading-overlay');
            loadingOverlay.style.display = 'flex';
            loadingOverlay.querySelector('h2').innerText = 'Processing Order...';

            try {
                // 1. Create Address
                const addressResp = await window.apiClient.apiPost('/api/Order/address', addressPayload);
                const addressId = addressResp.addressID || addressResp.AddressID || addressResp.addressId;

                // 2. Checkout
                const checkoutPayload = {
                    addressID: addressId,
                    paymentMethod: 0, // COD
                    voucherCode: voucherCode,
                    cartItems: cartItemsDto
                };

                const orderResp = await window.apiClient.apiPost('/api/Order/checkout', checkoutPayload);

                loadingOverlay.style.display = 'none';
                
                // [UPDATED] Sync success paid amount and execute cart clear callback if applicable
                const finalTotal = Math.max(0, window.checkoutState.subtotal + window.checkoutState.shippingFee - window.checkoutState.discount);
                document.getElementById('payment-success-amount').innerText = new Intl.NumberFormat('vi-VN').format(finalTotal) + ' VND';
                if (window.checkoutState.isCart && typeof window.onCartCheckoutSuccess === 'function') {
                    window.onCartCheckoutSuccess();
                }

                document.getElementById('payment-success-overlay').style.display = 'flex';
            } catch(error) {
                loadingOverlay.style.display = 'none';
                console.error("Checkout failed:", error);
                document.getElementById('payment-failed-overlay').style.display = 'flex';
            }
        }
    });

    // Handle VNPay Return
    async function handleVNPayReturn() {
        const urlParams = new URLSearchParams(window.location.search);
        // Ensure this logic only runs when VNPAY params are present
        if (urlParams.has('vnp_ResponseCode') && urlParams.has('vnp_TxnRef')) {
            const verifyOverlay = document.getElementById('vnpay-return-overlay');
            verifyOverlay.style.display = 'flex';

            // Assume Backend has an endpoint that handles return logic or we extract order ID from txnRef if needed.
            // Wait, we can extract order ID from vnp_TxnRef which might have format "BB_orderId_xxx"
            // Let's call GET /api/Order/customer/my-orders or use an endpoint to mark payment success
            
            const responseCode = urlParams.get('vnp_ResponseCode');
            const txnRef = urlParams.get('vnp_TxnRef');
            
            try {
                if (responseCode === '00') {
                    // It's a success
                    // Find the orderID. The backend OrderService generates vnp_TxnRef. Let's assume it ends with orderId or we just display success.
                    // Actually, OrderController API 9: POST /api/Order/{orderId}/payment-success
                    // We need orderId. Usually VNPay txnRef is "OrderId_Time". Let's extract orderId.
                    const parts = txnRef.split('_');
                    const orderIdStr = parts.length > 0 ? parts[0] : null;
                    if (orderIdStr && !isNaN(parseInt(orderIdStr))) {
                         await window.apiClient.apiPost(`/api/Order/${orderIdStr}/payment-success`);
                    }

                    verifyOverlay.style.display = 'none';
                    // Final total is not known here if refreshed, just show a message.
                    document.getElementById('payment-success-amount').innerText = "Paid via VNPay";
                    if (window.onCartCheckoutSuccess && typeof window.onCartCheckoutSuccess === 'function') {
                        window.onCartCheckoutSuccess();
                    }
                    document.getElementById('payment-success-overlay').style.display = 'flex';
                } else {
                    verifyOverlay.style.display = 'none';
                    document.getElementById('payment-failed-overlay').style.display = 'flex';
                }
            } catch (error) {
                console.error("Error verifying VNPay payment:", error);
                verifyOverlay.style.display = 'none';
                document.getElementById('payment-failed-overlay').style.display = 'flex';
            }
            
            // Clean up URL to prevent refreshing causing duplicate triggers
            const url = new URL(window.location);
            url.search = '';
            window.history.replaceState({}, document.title, url.toString());
        }
    }

    // Check URL on load in case we landed on the return page
    handleVNPayReturn();

    // [UPDATED] Cart SPA logic (showCart, renderCart, calculateTotals, appliedVoucher,
    // all cart event handlers, syncCartBadge) has been moved to the first
    // $(document).ready() block (before the closing }); at the end of Script Block 1)
    // so that those functions can access the SPA section variables
    // ($mainContent, $exploreSection, $dedicatedCommunity, $tindbookFeature,
    // $mainNavbar, $navLinks, cartItems, syncCartBadge, VOUCHERS_DATA)
    // that are locally scoped to that block.

    // Success/Fail actions
    document.querySelector('.btn-continue-shopping').addEventListener('click', () => {
        document.getElementById('payment-success-overlay').style.display = 'none';
        history.pushState(null, '', '/');
    });
    document.querySelector('.btn-view-order').addEventListener('click', () => {
        document.getElementById('payment-success-overlay').style.display = 'none';
        history.pushState(null, '', '/');
        alert("Redirecting to Order Details...");
    });
    document.querySelector('.btn-retry-payment').addEventListener('click', () => {
        document.getElementById('payment-failed-overlay').style.display = 'none';
        history.pushState(null, '', '/');
        openCheckout();
    });
    document.querySelector('.btn-change-method').addEventListener('click', () => {
        document.getElementById('payment-failed-overlay').style.display = 'none';
        history.pushState(null, '', '/');
        openCheckout();
    });
    document.querySelector('.btn-cancel-order').addEventListener('click', () => {
        document.getElementById('payment-failed-overlay').style.display = 'none';
        history.pushState(null, '', '/');
    });
});
