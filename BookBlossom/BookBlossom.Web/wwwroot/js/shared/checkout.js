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

    window.userAddresses = [];
    window.selectedAddress = null;
    let temporarySelectedAddressId = null;

    function showToast(message, type = 'success', title = '') {
        if (window.apiClient && typeof window.apiClient.showToast === 'function') {
            window.apiClient.showToast(message, type, title);
        } else {
            console.log(`[Toast ${type}]: ${message}`);
            alert(message);
        }
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
        let vouchersHtml = '';
        if (item.appliedVouchers && item.appliedVouchers.length > 0) {
            item.appliedVouchers.forEach(v => {
                vouchersHtml += `<div style="display:inline-flex; align-items:center; background:#ffebee; color:#C2185B; border:1px solid #f07c7c; padding:2px 8px; border-radius:4px; font-size:0.75rem; font-weight:bold; margin-right:5px; margin-top:5px;"><i class="fas fa-tag" style="margin-right:4px;"></i> ${v.code} (-${new Intl.NumberFormat('vi-VN').format(v.discount)}đ)</div>`;
            });
        }
        
        $container.append(`
    <img id="checkout-book-img" src="${item.img || '/images/Book/book4.jpg'}" style="width: 80px; height: 120px; object-fit: cover; border-radius: 6px; box-shadow: 0 4px 10px rgba(0,0,0,0.1);" alt="Book Cover">
        <div style="flex-grow: 1;">
            <h4 id="checkout-book-title" style="margin: 0 0 5px; font-size: 1.2rem; font-weight: 700; color: #333;">${item.title}</h4>
            <p id="checkout-book-author" style="margin: 0 0 10px; font-size: 0.9rem; color: #666;">${item.author || 'BookBlossom Edition'}</p>
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <span style="font-size: 0.95rem; color: #555;">Quantity: <strong id="checkout-book-qty">${item.qty}</strong></span>
                <span id="checkout-book-price" style="font-size: 1.25rem; font-weight: 700; color: #C2185B;">${new Intl.NumberFormat('vi-VN').format(itemPriceVnd * item.qty)} VND</span>
            </div>
            ${vouchersHtml}
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
                let vouchersHtml = '';
                if (item.appliedVouchers && item.appliedVouchers.length > 0) {
                    item.appliedVouchers.forEach(v => {
                        vouchersHtml += `<div style="display:inline-flex; align-items:center; background:#ffebee; color:#C2185B; border:1px solid #f07c7c; padding:2px 8px; border-radius:4px; font-size:0.7rem; font-weight:bold; margin-right:5px; margin-top:4px;"><i class="fas fa-tag" style="margin-right:3px;"></i> ${v.code} (-${new Intl.NumberFormat('vi-VN').format(v.discount)}đ)</div>`;
                    });
                }
                
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
                    ${vouchersHtml}
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
                this.style.border = '1.5px dashed #f07c7c';
                this.style.background = '#fffdfb';
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
            window.checkoutState.appliedVouchers = selected;
        } else {
            container.style.display = 'none';
            window.checkoutState.discount = 0;
            window.checkoutState.appliedVouchers = [];
        }
    }

    const addressModal = document.getElementById('checkout-address-modal-overlay');
    const addressContent = document.getElementById('checkout-address-modal-content');
    const btnCloseAddress = document.getElementById('btn-close-checkout-address');
    const btnCancelAddress = document.getElementById('btn-cancel-checkout-address');
    const btnSaveAddress = document.getElementById('btn-save-checkout-address');
    const btnChangeBlindAddr = document.getElementById('btn-change-blind-shipping');

    const addressListOverlay = document.getElementById('checkout-address-list-overlay');
    const addressListContent = document.getElementById('checkout-address-list-content');
    const btnCloseAddressList = document.getElementById('btn-close-address-list');
    const btnAddNewCheckoutAddr = document.getElementById('btn-add-new-checkout-addr');
    const btnConfirmAddressSelection = document.getElementById('btn-confirm-address-selection');
    const btnChangeCheckoutAddr = document.getElementById('btn-change-checkout-addr');
    const btnAddCheckoutAddrFirst = document.getElementById('btn-add-checkout-addr-first');

    async function loadUserAddresses(autoSelectId = null) {
        if (!window.apiClient) return;
        try {
            const resp = await window.apiClient.apiGet('/api/Order/address');
            window.userAddresses = resp || [];
            
            if (window.userAddresses.length === 0) {
                window.selectedAddress = null;
                renderActiveAddressCard(null);
                updateDetailsPagePreviews(null);
                return;
            }
            
            let selectAddr = null;
            if (autoSelectId) {
                selectAddr = window.userAddresses.find(a => (a.addressID || a.AddressID || a.addressId) === autoSelectId);
            }
            if (!selectAddr && window.selectedAddress) {
                const currentId = window.selectedAddress.addressID || window.selectedAddress.AddressID || window.selectedAddress.addressId;
                selectAddr = window.userAddresses.find(a => (a.addressID || a.AddressID || a.addressId) === currentId);
            }
            if (!selectAddr) {
                selectAddr = window.userAddresses.find(a => a.isDefault || a.IsDefault);
            }
            if (!selectAddr && window.userAddresses.length > 0) {
                selectAddr = window.userAddresses[0];
            }
            
            window.selectedAddress = selectAddr;
            renderActiveAddressCard(selectAddr);
            updateDetailsPagePreviews(selectAddr);
        } catch (err) {
            console.error("Failed to load user addresses:", err);
        }
    }

    function renderActiveAddressCard(address) {
        const detailsDiv = document.getElementById('checkout-active-address-details');
        const emptyDiv = document.getElementById('checkout-active-address-empty');
        const changeBtn = document.getElementById('btn-change-checkout-addr');
        
        if (address) {
            if (detailsDiv) detailsDiv.style.display = 'block';
            if (emptyDiv) emptyDiv.style.display = 'none';
            if (changeBtn) changeBtn.style.display = 'block';
            
            const nameSpan = document.getElementById('checkout-active-name');
            const defaultBadge = document.getElementById('checkout-active-default-badge');
            const phoneDiv = document.getElementById('checkout-active-phone');
            const addrTextDiv = document.getElementById('checkout-active-address-text');
            
            const receiverName = address.receiverName || address.ReceiverName || '';
            const phoneNumber = address.phoneNumber || address.PhoneNumber || '';
            const detailAddress = address.detailAddress || address.DetailAddress || '';
            const isDefault = address.isDefault || address.IsDefault || false;
            
            if (nameSpan) nameSpan.innerText = receiverName;
            if (defaultBadge) defaultBadge.style.display = isDefault ? 'inline-block' : 'none';
            if (phoneDiv) phoneDiv.innerHTML = `<i class="fas fa-phone me-1" style="color: #aaa; font-size: 0.75rem;"></i>${phoneNumber}`;
            if (addrTextDiv) addrTextDiv.innerHTML = `<i class="fas fa-map-pin me-1" style="color: #C2185B; font-size: 0.75rem;"></i>${detailAddress}`;
        } else {
            if (detailsDiv) detailsDiv.style.display = 'none';
            if (emptyDiv) emptyDiv.style.display = 'flex';
            if (changeBtn) changeBtn.style.display = 'none';
        }
    }

    function updateDetailsPagePreviews(address) {
        const detailText = address ? (address.detailAddress || address.DetailAddress || '') : "No shipping address saved. Please add one.";
        const previewEl = document.getElementById('detail-shipping-addr-text');
        if (previewEl) previewEl.innerText = detailText;
        const blindPreviewEl = document.getElementById('blind-detail-shipping-addr-text');
        if (blindPreviewEl) blindPreviewEl.innerText = detailText;
    }

    function openAddressListModal() {
        temporarySelectedAddressId = window.selectedAddress ? (window.selectedAddress.addressID || window.selectedAddress.AddressID || window.selectedAddress.addressId) : null;
        renderAddressListModalContent();
        
        if (addressListOverlay) {
            addressListOverlay.style.display = 'flex';
            void addressListOverlay.offsetWidth;
            addressListContent.style.opacity = '1';
            addressListContent.style.transform = 'scale(1)';
        }
    }

    function closeAddressListModal() {
        if (addressListContent) {
            addressListContent.style.opacity = '0';
            addressListContent.style.transform = 'scale(0.95)';
            setTimeout(() => {
                addressListOverlay.style.display = 'none';
            }, 300);
        }
    }

    function renderAddressListModalContent() {
        const container = document.getElementById('checkout-address-list-container');
        if (!container) return;
        container.innerHTML = '';
        
        if (window.userAddresses.length === 0) {
            container.innerHTML = `
                <div style="text-align: center; padding: 30px 0; color: #aaa; font-size: 0.9rem;">
                    <i class="fas fa-map-marked-alt" style="font-size: 2.5rem; margin-bottom: 10px; display: block; color: #ddd;"></i>
                    No saved addresses yet.
                </div>
            `;
            return;
        }
        
        window.userAddresses.forEach(addr => {
            const addrId = addr.addressID || addr.AddressID || addr.addressId;
            const receiverName = addr.receiverName || addr.ReceiverName || '';
            const phoneNumber = addr.phoneNumber || addr.PhoneNumber || '';
            const detailAddress = addr.detailAddress || addr.DetailAddress || '';
            const isDefault = addr.isDefault || addr.IsDefault || false;
            
            const isSelected = temporarySelectedAddressId === addrId;
            
            const item = document.createElement('div');
            item.className = 'checkout-address-item';
            item.dataset.id = addrId;
            item.style.cssText = `
                background: ${isSelected ? '#fff8fb' : '#fafafa'};
                border: 1.5px solid ${isSelected ? '#f5c6d8' : '#eee'};
                border-radius: 12px;
                padding: 14px 16px;
                cursor: pointer;
                transition: all 0.2s;
                display: flex;
                align-items: flex-start;
                gap: 12px;
                margin-bottom: 10px;
            `;
            
            item.innerHTML = `
                <input type="radio" name="checkout_addr_sel" value="${addrId}" ${isSelected ? 'checked' : ''} style="margin-top: 4px; accent-color: #C2185B; cursor: pointer;">
                <div style="flex-grow: 1; text-align: left;">
                    <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 4px;">
                        <span style="font-weight: 700; color: #222; font-size: 0.95rem;">${receiverName}</span>
                        ${isDefault ? '<span style="background: #C2185B; color: #fff; font-size: 0.65rem; font-weight: 700; padding: 2px 8px; border-radius: 10px; text-transform: uppercase; letter-spacing: 0.3px;">Default</span>' : ''}
                    </div>
                    <div style="color: #666; font-size: 0.85rem; margin-bottom: 2px;"><i class="fas fa-phone me-1" style="color: #aaa; font-size: 0.75rem;"></i>${phoneNumber}</div>
                    <div style="color: #555; font-size: 0.85rem; line-height: 1.4;"><i class="fas fa-map-pin me-1" style="color: #C2185B; font-size: 0.75rem;"></i>${detailAddress}</div>
                </div>
                <div style="display: flex; gap: 8px; align-self: center;" onclick="event.stopPropagation();">
                    <button type="button" class="btn-edit-checkout-addr-item" data-id="${addrId}" style="background: #f1f5f9; border: none; color: #475569; width: 28px; height: 28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; cursor: pointer; font-size: 0.75rem; transition: all 0.2s;" title="Edit">
                        <i class="fas fa-pen"></i>
                    </button>
                    <button type="button" class="btn-delete-checkout-addr-item" data-id="${addrId}" style="background: #fee2e2; border: none; color: #dc2626; width: 28px; height: 28px; border-radius: 50%; display: flex; align-items: center; justify-content: center; cursor: pointer; font-size: 0.75rem; transition: all 0.2s;" title="Delete">
                        <i class="fas fa-trash-alt"></i>
                    </button>
                </div>
            `;
            
            item.addEventListener('click', () => {
                temporarySelectedAddressId = addrId;
                renderAddressListModalContent();
            });
            
            container.appendChild(item);
        });
    }

    function openAddressFormModal(addressId = null) {
        const inputId = document.getElementById('checkout-address-input-id');
        if (inputId) inputId.value = addressId || '';
        
        const titleSpan = document.getElementById('checkout-address-form-title');
        const defaultCheckbox = document.getElementById('checkout-address-input-default');
        
        if (addressId) {
            if (titleSpan) titleSpan.innerText = "Edit Shipping Address";
            const addr = window.userAddresses.find(a => (a.addressID || a.AddressID || a.addressId) === addressId);
            if (addr) {
                document.getElementById('checkout-address-input-name').value = addr.receiverName || addr.ReceiverName || '';
                document.getElementById('checkout-address-input-phone').value = addr.phoneNumber || addr.PhoneNumber || '';
                document.getElementById('checkout-address-input-text').value = addr.detailAddress || addr.DetailAddress || '';
                if (defaultCheckbox) defaultCheckbox.checked = addr.isDefault || addr.IsDefault || false;
            }
        } else {
            if (titleSpan) titleSpan.innerText = "Add New Shipping Address";
            document.getElementById('checkout-address-input-name').value = '';
            document.getElementById('checkout-address-input-phone').value = '';
            document.getElementById('checkout-address-input-text').value = '';
            if (defaultCheckbox) defaultCheckbox.checked = window.userAddresses.length === 0;
        }
        
        if (addressModal) {
            addressModal.style.display = 'flex';
            void addressModal.offsetWidth;
            addressContent.style.opacity = '1';
            addressContent.style.transform = 'scale(1)';
        }
    }

    function closeAddressModal() {
        if (addressContent) {
            addressContent.style.opacity = '0';
            addressContent.style.transform = 'scale(0.95)';
            setTimeout(() => {
                addressModal.style.display = 'none';
            }, 300);
        }
    }

    if (btnChangeAddr) {
        btnChangeAddr.addEventListener('click', function (e) {
            e.preventDefault();
            openAddressListModal();
        });
    }

    if (btnChangeBlindAddr) {
        btnChangeBlindAddr.addEventListener('click', function (e) {
            e.preventDefault();
            openAddressListModal();
        });
    }

    if (btnChangeCheckoutAddr) {
        btnChangeCheckoutAddr.addEventListener('click', openAddressListModal);
    }
    
    $(document).on('click', '#btn-add-checkout-addr-first', function() {
        openAddressFormModal(null);
    });

    if (btnAddNewCheckoutAddr) {
        btnAddNewCheckoutAddr.addEventListener('click', function() {
            openAddressFormModal(null);
        });
    }

    if (btnCloseAddressList) btnCloseAddressList.addEventListener('click', closeAddressListModal);
    
    if (addressListOverlay) {
        addressListOverlay.addEventListener('click', function (e) {
            if (e.target === addressListOverlay) closeAddressListModal();
        });
    }

    if (btnConfirmAddressSelection) {
        btnConfirmAddressSelection.addEventListener('click', () => {
            if (!temporarySelectedAddressId) {
                showToast("Please select an address first!", "error");
                return;
            }
            const addr = window.userAddresses.find(a => (a.addressID || a.AddressID || a.addressId) === temporarySelectedAddressId);
            if (addr) {
                window.selectedAddress = addr;
                renderActiveAddressCard(addr);
                updateDetailsPagePreviews(addr);
                closeAddressListModal();
                showToast("Shipping address selected.");
            }
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
        btnSaveAddress.addEventListener('click', async function () {
            const addressIdVal = document.getElementById('checkout-address-input-id').value;
            const receiverName = document.getElementById('checkout-address-input-name').value.trim();
            const phoneNumber = document.getElementById('checkout-address-input-phone').value.trim();
            const detailAddress = document.getElementById('checkout-address-input-text').value.trim();
            const defaultCheckbox = document.getElementById('checkout-address-input-default');
            const isDefault = defaultCheckbox ? defaultCheckbox.checked : false;
            
            if (!receiverName || !phoneNumber || !detailAddress) {
                showToast("Please fill in all address details!", "error");
                return;
            }
            
            if (!/^\d{9,11}$/.test(phoneNumber)) {
                showToast("Please enter a valid phone number (9-11 digits)!", "error");
                return;
            }
            
            const payload = { receiverName, phoneNumber, detailAddress, isDefault };
            
            try {
                let savedAddr = null;
                if (addressIdVal) {
                    const addressId = parseInt(addressIdVal);
                    const resp = await window.apiClient.apiPut(`/api/Order/address/${addressId}`, payload);
                    savedAddr = resp;
                    showToast("Address updated successfully!");
                } else {
                    const resp = await window.apiClient.apiPost('/api/Order/address', payload);
                    savedAddr = resp;
                    showToast("Address added successfully!");
                }
                
                const newId = savedAddr ? (savedAddr.addressID || savedAddr.AddressID || savedAddr.addressId) : null;
                
                closeAddressModal();
                closeAddressListModal();
                await loadUserAddresses(newId);
            } catch (err) {
                console.error("Failed to save address:", err);
                showToast(err.message || "Failed to save address.", "error");
            }
        });
    }

    $(document).on('click', '.btn-edit-checkout-addr-item', function (e) {
        e.stopPropagation();
        const id = parseInt($(this).data('id'));
        if (id) {
            openAddressFormModal(id);
        }
    });

    $(document).on('click', '.btn-delete-checkout-addr-item', async function (e) {
        e.stopPropagation();
        const id = parseInt($(this).data('id'));
        if (!id) return;
        
        if (!confirm("Are you sure you want to delete this address?")) return;
        
        try {
            await window.apiClient.apiDelete(`/api/Order/address/${id}`);
            showToast("Address deleted successfully!");
            
            await loadUserAddresses();
            
            temporarySelectedAddressId = window.selectedAddress ? (window.selectedAddress.addressID || window.selectedAddress.AddressID || window.selectedAddress.addressId) : null;
            renderAddressListModalContent();
        } catch (err) {
            console.error("Failed to delete address:", err);
            showToast(err.message || "Failed to delete address.", "error");
        }
    });

    // Load initial addresses at page load
    if (window.apiClient) {
        loadUserAddresses();
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
        // Validate shipping address
        if (!window.selectedAddress) {
            showToast("Please add or select a shipping address to proceed!", "error");
            return;
        }
        const addressId = window.selectedAddress.addressID || window.selectedAddress.AddressID || window.selectedAddress.addressId;

        // Read the Order Note value and persist it on the shared checkout state
        const orderNote = window.getOrderNote ? window.getOrderNote() : '';
        window.checkoutState.orderNote = orderNote;

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
                // Checkout
                const checkoutPayload = {
                    addressID: addressId,
                    paymentMethod: 1, // VNPay
                    voucherCode: voucherCode,
                    note: window.checkoutState.orderNote,
                    cartItems: cartItemsDto
                };

                const orderResp = await window.apiClient.apiPost('/api/Order/checkout', checkoutPayload);
                if (orderResp && (orderResp.orderID || orderResp.orderId)) {
                    const orderId = orderResp.orderID || orderResp.orderId;
                    const paymentResp = await window.apiClient.apiPost('/api/payment/vnpay/create', { orderId: orderId });
                    
                    if (paymentResp && paymentResp.paymentUrl) {
                        window.location.href = paymentResp.paymentUrl;
                    } else {
                        loadingOverlay.style.display = 'none';
                        showToast('Không thể tạo liên kết thanh toán VNPay.', 'error');
                    }
                } else {
                    loadingOverlay.style.display = 'none';
                    showToast('Tạo đơn hàng thất bại.', 'error');
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
                // Checkout
                const checkoutPayload = {
                    addressID: addressId,
                    paymentMethod: 0, // COD
                    voucherCode: voucherCode,
                    note: window.checkoutState.orderNote,
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

    // VNPay Return is now handled directly by the server side and redirects to /Order/PaymentResult
    // No need to handle it via JS on the client side.

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
        window.location.href = '/Orders';
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
