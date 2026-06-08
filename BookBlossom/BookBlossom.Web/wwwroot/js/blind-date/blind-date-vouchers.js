// Model
const BlindVoucherModel = {
    selectedVouchers: new Set(),
    
    toggleVoucher: function(code) {
        if (this.selectedVouchers.has(code)) {
            this.selectedVouchers.delete(code);
            return { action: 'removed', code: code };
        } else {
            const voucher = window.activeVouchers ? window.activeVouchers.find(v => v.voucherCode === code) : null;
            if (!voucher) return { action: 'error', message: 'Voucher not found.' };

            // Validation logic...
            if (voucher.isStackable === false && this.selectedVouchers.size > 0) {
                return { action: 'error', message: `Voucher <b>${code}</b> cannot be used in conjunction with other vouchers.` };
            }

            let hasNonStackable = false;
            let nonStackableCode = '';
            this.selectedVouchers.forEach(sc => {
                const sv = window.activeVouchers ? window.activeVouchers.find(v => v.voucherCode === sc) : null;
                if (sv && sv.isStackable === false) {
                    hasNonStackable = true;
                    nonStackableCode = sc;
                }
            });

            if (hasNonStackable) {
                return { action: 'error', message: `Voucher <b>${nonStackableCode}</b> cannot be used in conjunction with other vouchers.` };
            }

            this.selectedVouchers.add(code);
            return { action: 'added', code: code };
        }
    },
    
    clearVouchers: function() {
        this.selectedVouchers.clear();
    }
};

// View
const BlindVoucherView = {
    renderVouchers: function(categoryId) {
        const $slider = $('.blind-coupons-container');
        if (!$slider.length) return;
        
        $slider.empty();

        if (!window.activeVouchers || window.activeVouchers.length === 0) {
            return;
        }

        const sortedVouchers = [...window.activeVouchers].sort((a, b) => {
            const aSelected = BlindVoucherModel.selectedVouchers.has(a.voucherCode) ? 1 : 0;
            const bSelected = BlindVoucherModel.selectedVouchers.has(b.voucherCode) ? 1 : 0;
            return bSelected - aSelected;
        });

        sortedVouchers.forEach(v => {
            const hasCategory = v.applicableCategoryIDs && v.applicableCategoryIDs.length > 0;
            const hasBook = v.applicableBookIDs && v.applicableBookIDs.length > 0;
            
            // EXCLUDE VOUCHERS SPECIFYING BOOKS FOR BLIND DATE
            if (hasBook) return;
            
            if (hasCategory) {
                if (!categoryId || !v.applicableCategoryIDs.includes(categoryId)) {
                    return;
                }
            }

            const code = v.voucherCode;
            const isSelected = BlindVoucherModel.selectedVouchers.has(code);
            const selectedClass = isSelected ? 'selected' : '';

            const borderStyle = isSelected
                ? `2px solid #6a4f8c`
                : `1.5px dashed #a291b5`;
                
            const valueDisplay = v.discountType === 'Percentage' ? `${v.discountValue}%` : `${new Intl.NumberFormat('vi-VN').format(v.discountValue)} VND`;

            const cardHtml = `
                <div class="blind-coupon-card ${selectedClass}" data-code="${code}"
                    style="flex: 0 0 190px; background: #faf8fc; border: ${borderStyle}; border-radius: 10px; padding: 12px; position: relative; box-shadow: 0 2px 6px rgba(0,0,0,0.02); transition: all 0.2s; cursor: pointer; text-align: left;">
                    <div class="checkmark-badge" style="${isSelected ? 'display:flex;' : 'display:none;'} position: absolute; top: -6px; right: -6px; background: #6a4f8c; color: #fff; border-radius: 50%; width: 18px; height: 18px; font-size: 0.65rem; align-items: center; justify-content: center; z-index: 5; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
                        <i class="fas fa-check"></i>
                    </div>
                    <div style="font-size: 0.7rem; font-weight: 700; color: #6a4f8c; background: #f0eaf7; padding: 2px 6px; border-radius: 4px; width: fit-content; margin-bottom: 6px;">${code}</div>
                    <div style="font-size: 0.82rem; font-weight: 700; color: #333; margin-bottom: 4px;">${v.voucherName || 'Mystery Discount'}</div>
                    <div style="font-size: 0.72rem; color: #888;">Save ${valueDisplay}</div>
                </div>
            `;

            $slider.append(cardHtml);
        });
    },

    syncUI: function() {
        this.renderVouchers(window.currentBlindBookData ? window.currentBlindBookData.categoryId : null);
        
        $('#blind-vouchers-modal .modal-voucher-item').each(function () {
            const $item = $(this);
            const code = $item.attr('data-code');
            const isSelected = BlindVoucherModel.selectedVouchers.has(code);

            if (isSelected) {
                $item.addClass('selected');
                $item.css({ border: '2px solid #6a4f8c', background: '#faf8fc' });
                $item.find('.btn-modal-apply-voucher').text('Applied').css({ background: '#6a4f8c', color: '#fff' });
            } else {
                $item.removeClass('selected');
                $item.css({ border: '1.5px dashed #a291b5', background: '#fff' });
                $item.find('.btn-modal-apply-voucher').text('Apply').css({ background: '#fff', color: '#6a4f8c' });
            }
        });
        
        $('#blind-modal-selected-count').text(BlindVoucherModel.selectedVouchers.size);
    },

    showToast: function(message) {
        if ($('.toast-notification').length > 0) {
            $('.toast-notification').remove();
        }
        const $toast = $(`
            <div class="toast-notification">
                <i class="fas fa-check-circle" style="color: #28a745;"></i>
                <span>${message}</span>
                <div class="toast-progress"></div>
            </div>
        `);
        $('body').append($toast);
        $toast.fadeIn(300);
        setTimeout(() => {
            $toast.fadeOut(300, function () { $(this).remove(); });
        }, 3000);
    },
    
    showError: function(message) {
        if (window.showVoucherError) {
            window.showVoucherError(message);
        } else {
            alert(message);
        }
    }
};

// Controller
const BlindVoucherController = {
    init: function() {
        this.bindEvents();
    },

    bindEvents: function() {
        $(document)
            .off('click.blindCoupon')
            .on('click.blindCoupon', '.blind-coupon-card', (e) => {
                e.preventDefault();
                const code = $(e.currentTarget).attr('data-code');
                if (code) this.handleToggleVoucher(code);
            });

        $(document)
            .off('click.seeAllBlindVouchers')
            .on('click.seeAllBlindVouchers', '#btn-see-all-blind-vouchers', async (e) => {
                e.preventDefault();
                await this.fetchAndPopulateModalVouchers();
                BlindVoucherView.syncUI();
                $('#blind-vouchers-modal').fadeIn(250);
            });

        // Tab switching logic for blind date voucher modal
        $(document).off('click.bdVoucherTab').on('click.bdVoucherTab', '#blind-vouchers-modal .cart-voucher-tab-btn', function() {
            $('#blind-vouchers-modal .cart-voucher-tab-btn').removeClass('active');
            $(this).addClass('active');
            const target = $(this).attr('data-target');
            $('#blind-vouchers-modal .cart-voucher-tab-content').removeClass('active').hide();
            $('#' + target).addClass('active').css('display', 'flex');
        });

        $(document)
            .off('click.blindModalVoucher')
            .on('click.blindModalVoucher', '#blind-vouchers-modal .modal-voucher-item', (e) => {
                e.preventDefault();
                const code = $(e.currentTarget).attr('data-code');
                if (code) this.handleToggleVoucher(code);
            });

        $(document)
            .off('click.blindModalVoucherButton')
            .on('click.blindModalVoucherButton', '#blind-vouchers-modal .btn-modal-apply-voucher', (e) => {
                e.preventDefault();
                e.stopPropagation();
                const code = $(e.currentTarget).closest('.modal-voucher-item').attr('data-code');
                if (code) this.handleToggleVoucher(code);
            });

        $(document)
            .off('click.closeBlindVouchers')
            .on('click.closeBlindVouchers', '#btn-close-blind-vouchers, #blind-vouchers-modal .modal-backdrop', () => {
                $('#blind-vouchers-modal').fadeOut(250);
            });

        $(document)
            .off('click.confirmBlindVouchers')
            .on('click.confirmBlindVouchers', '#btn-confirm-blind-vouchers', () => {
                $('#blind-vouchers-modal').fadeOut(250);
                if (BlindVoucherModel.selectedVouchers.size > 0) {
                    BlindVoucherView.showToast(`Successfully applied ${BlindVoucherModel.selectedVouchers.size} mystery voucher(s)!`);
                }
                BlindVoucherView.syncUI();
            });
    },

    fetchAndPopulateModalVouchers: async function() {
        if (!window.activeVouchers) {
            if (window.apiClient) {
                try {
                    const res = await window.apiClient.apiGet('/api/VoucherAPI/active');
                    window.activeVouchers = res.data || res;
                } catch (e) {
                    console.error('Failed to load customer vouchers', e);
                    window.activeVouchers = [];
                }
            } else {
                window.activeVouchers = [];
            }
        }

        const $productVouchersList = $('#blind-product-vouchers-list');
        const $platformVouchersList = $('#blind-platform-vouchers-list');
        
        $productVouchersList.empty();
        $platformVouchersList.empty();

        if (window.activeVouchers.length === 0) {
            $productVouchersList.html('<div style="text-align: center; padding: 20px; color: #888;">No product vouchers available.</div>');
            $platformVouchersList.html('<div style="text-align: center; padding: 20px; color: #888;">No platform vouchers available.</div>');
            return;
        }

        const categoryId = window.currentBlindBookData ? window.currentBlindBookData.categoryId : null;
        let categoryIdResolved = categoryId;
        
        // resolve categoryId if missing but category string is there
        if (!categoryId && window.currentBlindBookData && window.currentBlindBookData.category) {
             const catName = window.currentBlindBookData.category.toLowerCase();
             try {
                if (!window.cachedCategories) {
                    const response = await fetch('/api/category?status=Active');
                    if (response.ok) window.cachedCategories = await response.json();
                }
                if (window.cachedCategories) {
                    const catObj = window.cachedCategories.find(c => c.categoryName.toLowerCase() === catName);
                    if (catObj) categoryIdResolved = catObj.categoryID || catObj.categoryId || catObj.id;
                }
            } catch (e) {}
        }

        window.activeVouchers.forEach(v => {
            const code = v.voucherCode;
            const name = v.voucherName || 'Mystery Discount';
            const valueDisplay = v.discountType === 'Percentage' ? `${v.discountValue}%` : `${new Intl.NumberFormat('vi-VN').format(v.discountValue)} VND`;
            const isStackable = v.isStackable;
            const stackableBadge = isStackable ? 
                `<span style="background: #e8f5e9; color: #2e7d32; font-size: 0.65rem; padding: 2px 6px; border-radius: 4px; font-weight: 600;">Stackable</span>` : 
                `<span style="background: #ffebee; color: #c62828; font-size: 0.65rem; padding: 2px 6px; border-radius: 4px; font-weight: 600;">Non-stackable</span>`;

            const hasCategory = v.applicableCategoryIDs && v.applicableCategoryIDs.length > 0;
            const hasBook = v.applicableBookIDs && v.applicableBookIDs.length > 0;
            
            // EXCLUDE SPECIFIC BOOK VOUCHERS FROM BLIND DATE
            if (hasBook) return;
            
            let isValid = true;
            if (hasCategory) {
                isValid = categoryIdResolved && v.applicableCategoryIDs.includes(categoryIdResolved);
            }

            if (!isValid) return;

            const isSelected = BlindVoucherModel.selectedVouchers.has(code);
            const selectedClass = isSelected ? 'selected' : '';
            const btnText = isSelected ? 'Applied' : 'Apply';

            const itemHtml = `
                <div class="modal-voucher-item ${selectedClass}" data-code="${code}" data-stackable="${isStackable}" style="display: flex; border: 1.5px dashed #a291b5; border-radius: 12px; overflow: hidden; background: #fff; transition: all 0.25s; cursor: pointer; position: relative;">
                    <div style="background: #fdfafb; padding: 20px; display: flex; flex-direction: column; align-items: center; justify-content: center; min-width: 110px; border-right: 1.5px dashed #a291b5; text-align: center;">
                        <span style="font-size: 0.85rem; font-weight: 700; color: #6a4f8c; letter-spacing: 0.5px;">${code}</span>
                        <span style="font-size: 0.65rem; color: #6a4f8c; font-weight: 600; margin-top: 4px; background: #fff; padding: 2px 6px; border-radius: 10px;">Code</span>
                    </div>
                    <div style="padding: 15px; flex-grow: 1; display: flex; flex-direction: column; justify-content: center; text-align: left;">
                        <div style="display: flex; gap: 5px; align-items: center; margin-bottom: 4px; flex-wrap: wrap;">
                            <h4 style="font-size: 0.95rem; font-weight: 700; color: #333; margin: 0;">${name} (Save ${valueDisplay})</h4>
                            ${stackableBadge}
                            ${v.isAutoRefundable ? '<span style="background: #e6f7ff; color: #0050b3; font-size: 0.65rem; padding: 2px 6px; border-radius: 4px; font-weight: 600;">Refundable</span>' : '<span style="background: #fff0f6; color: #c41d7f; font-size: 0.65rem; padding: 2px 6px; border-radius: 4px; font-weight: 600;">Non-refundable</span>'}
                            ${v.minPlan > 0 && window.getPlanName ? '<span style="background: #f6ffed; color: #389e0d; font-size: 0.65rem; padding: 2px 6px; border-radius: 4px; font-weight: 600;">Min Plan: ' + window.getPlanName(v.minPlan) + '</span>' : ''}
                            ${v.membershipRankRequired > 0 && window.getRankName ? '<span style="background: #fff7e6; color: #d46b08; font-size: 0.65rem; padding: 2px 6px; border-radius: 4px; font-weight: 600;">Min Rank: ' + window.getRankName(v.membershipRankRequired) + '</span>' : ''}
                            ${v.minReputationRequired > 0 ? '<span style="background: #f9f0ff; color: #531dab; font-size: 0.65rem; padding: 2px 6px; border-radius: 4px; font-weight: 600;">Min Rep: ' + v.minReputationRequired + '</span>' : ''}
                        </div>
                        <p style="font-size: 0.78rem; color: #666; margin: 0 0 6px 0;">Min order ${new Intl.NumberFormat('vi-VN').format(v.minOrderValue)} VND.${v.maxDiscountAmount > 0 ? ' Max discount ' + new Intl.NumberFormat('vi-VN').format(v.maxDiscountAmount) + ' VND.' : ''}</p>
                        <span style="font-size: 0.7rem; color: #aaa;">Expiry: ${new Date(v.endDate).toLocaleDateString()}</span>
                    </div>
                    <div style="padding: 15px; display: flex; align-items: center; justify-content: center; min-width: 90px; z-index: 2;">
                        <button class="btn-modal-apply-voucher" style="background: #fff; color: #6a4f8c; border: 1.5px solid #6a4f8c; border-radius: 20px; padding: 6px 14px; font-size: 0.8rem; font-weight: 700; cursor: pointer; transition: all 0.2s;">${btnText}</button>
                    </div>
                </div>
            `;
            
            if (hasCategory) {
                $productVouchersList.append(itemHtml);
            } else {
                $platformVouchersList.append(itemHtml);
            }
        });
        
        if ($productVouchersList.children().length === 0) {
            $productVouchersList.html('<div style="text-align: center; padding: 20px; color: #888;">No product vouchers applicable.</div>');
        }
        if ($platformVouchersList.children().length === 0) {
            $platformVouchersList.html('<div style="text-align: center; padding: 20px; color: #888;">No platform vouchers available.</div>');
        }
    },

    loadAndRenderVouchers: async function(blindBookData) {
        if (!blindBookData) return;
        BlindVoucherModel.clearVouchers();
        
        if (!window.activeVouchers && window.apiClient) {
            try {
                const res = await window.apiClient.apiGet('/api/VoucherAPI/active');
                window.activeVouchers = res.data || res;
            } catch (e) {}
        }
        
        let catId = blindBookData.categoryId;
        if (!catId && blindBookData.category) {
             const catName = blindBookData.category.toLowerCase();
             try {
                if (!window.cachedCategories) {
                    const response = await fetch('/api/category?status=Active');
                    if (response.ok) window.cachedCategories = await response.json();
                }
                if (window.cachedCategories) {
                    const catObj = window.cachedCategories.find(c => c.categoryName.toLowerCase() === catName);
                    if (catObj) catId = catObj.categoryID || catObj.categoryId || catObj.id;
                }
            } catch (e) {}
        }

        BlindVoucherView.renderVouchers(catId);
        BlindVoucherView.syncUI();
    },

    handleToggleVoucher: function(code) {
        const result = BlindVoucherModel.toggleVoucher(code);
        if (result.action === 'error') {
            BlindVoucherView.showError(result.message);
        } else if (result.action === 'added') {
            BlindVoucherView.showToast(`Mystery Voucher "${code}" applied!`);
        } else if (result.action === 'removed') {
            BlindVoucherView.showToast(`Mystery Voucher "${code}" removed.`);
        }
        BlindVoucherView.syncUI();
    },

    getSelectedVouchers: function() {
        return BlindVoucherModel.selectedVouchers;
    }
};

// Export to window
window.BookBlossomBlindDateVouchers = BlindVoucherController;

$(document).ready(function() {
    BlindVoucherController.init();
});
