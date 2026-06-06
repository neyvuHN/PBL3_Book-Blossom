// Model
const BlindVoucherModel = {
    selectedVouchers: new Set(),
    applicableVouchers: [],

    getApplicableVouchers: async function(categoryId, categoryName) {
        if (!window.VOUCHERS_DATA) return [];
        
        // resolve categoryId if missing
        if (!categoryId && categoryName) {
             const catName = categoryName.toLowerCase();
             try {
                if (!window.cachedCategories) {
                    const response = await fetch('/api/category?status=Active');
                    if (response.ok) window.cachedCategories = await response.json();
                }
                if (window.cachedCategories) {
                    const catObj = window.cachedCategories.find(c => c.categoryName.toLowerCase() === catName);
                    if (catObj) categoryId = catObj.categoryID || catObj.categoryId || catObj.id;
                }
            } catch (e) {
                console.error("Failed to fetch categories", e);
            }
        }

        return window.VOUCHERS_DATA.filter(v => {
            const hasCategories = v.applicableCategories && v.applicableCategories.length > 0;
            const hasBooks = v.applicableBooks && v.applicableBooks.length > 0;

            if (hasCategories) {
                return categoryId && v.applicableCategories.some(c => String(c) === String(categoryId));
            } else if (hasBooks) {
                return false;
            } else {
                return true;
            }
        });
    },

    toggleVoucher: function(code) {
        if (this.selectedVouchers.has(code)) {
            this.selectedVouchers.delete(code);
            return { action: 'removed', code: code };
        } else {
            const voucher = window.VOUCHERS_DATA ? window.VOUCHERS_DATA.find(v => v.code === code) : null;
            
            // Validation logic...
            if (voucher && voucher.isStackable === false && this.selectedVouchers.size > 0) {
                return { action: 'error', message: `Voucher <b>${code}</b> cannot be used in conjunction with other vouchers.` };
            }

            let hasNonStackable = false;
            let nonStackableCode = '';
            this.selectedVouchers.forEach(sc => {
                const sv = window.VOUCHERS_DATA ? window.VOUCHERS_DATA.find(v => v.code === sc) : null;
                if (sv && sv.isStackable === false) {
                    hasNonStackable = true;
                    nonStackableCode = sc;
                }
            });

            if (hasNonStackable) {
                return { action: 'error', message: `Voucher <b>${nonStackableCode}</b> cannot be used in conjunction with other vouchers.` };
            }

            if (voucher && window.currentUser) {
                if (voucher.minPlan > (window.currentUser.plan || 0)) {
                    return { action: 'error', message: `Voucher <b>${code}</b> requires a minimum subscription plan of <b>${window.getPlanName(voucher.minPlan)}</b>.` };
                }
                if (voucher.minReputation > (window.currentUser.reputation || 0)) {
                    return { action: 'error', message: `Voucher <b>${code}</b> requires a minimum reputation score of <b>${voucher.minReputation}</b>.` };
                }
                if (voucher.minRank > (window.currentUser.rank || 0)) {
                     return { action: 'error', message: `Voucher <b>${code}</b> requires a minimum membership rank of <b>${window.getRankName(voucher.minRank)}</b>.` };
                }
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
    renderVouchers: function(vouchers) {
        const $container = $('.blind-coupons-container');
        $container.empty();

        if (vouchers.length === 0) {
            $container.append('<p style="padding: 15px; color: #888;">No mystery vouchers available for this book.</p>');
        } else {
            vouchers.forEach(v => {
                if (window.GlobalVouchers && window.GlobalVouchers.getBlindVoucherCardHtml) {
                    const html = window.GlobalVouchers.getBlindVoucherCardHtml(v);
                    $container.append(html);
                }
            });
        }
    },

    renderModalVouchers: function(vouchers) {
        const $modalContainer = $('#blind-vouchers-container');
        $modalContainer.empty();
        
        if (vouchers.length === 0) {
            $modalContainer.append('<p style="text-align:center; color:#888; margin-top:20px;">No mystery vouchers available for this book.</p>');
            return;
        }
        
        vouchers.forEach(v => {
            if (window.GlobalVouchers && window.GlobalVouchers.getBlindVoucherHtml) {
                const html = window.GlobalVouchers.getBlindVoucherHtml(v);
                $modalContainer.append(html);
            }
        });
    },

    syncUI: function(selectedVouchersSet) {
        $('.blind-coupon-card').each(function () {
            const $card = $(this);
            const code = $card.attr('data-code');
            const isSelected = selectedVouchersSet.has(code);

            $card.toggleClass('selected', isSelected);

            if (isSelected) {
                $card.css({
                    border: '2px solid #6a4f8c',
                    background: '#faf8fc',
                    boxShadow: '0 6px 18px rgba(106, 79, 140, 0.16)'
                });
            } else {
                $card.css({
                    border: '1.5px dashed #a291b5',
                    background: '#faf8fc',
                    boxShadow: '0 2px 6px rgba(0,0,0,0.01)'
                });
            }

            $card.find('.checkmark-badge, .checkmark-badge-blind').css('display', isSelected ? 'flex' : 'none');
        });

        $('#blind-vouchers-modal .modal-blind-voucher-item').each(function () {
            const $item = $(this);
            const code = $item.attr('data-code');
            const isSelected = selectedVouchersSet.has(code);

            $item.toggleClass('selected', isSelected);

            if (isSelected) {
                $item.css({ border: '2px solid #6a4f8c', background: '#faf8fc' });
                $item.find('.btn-modal-apply-blind-voucher').text('Remove').css({ background: '#6a4f8c', color: '#fff' });
            } else {
                $item.css({ border: '1.5px dashed #a291b5', background: '#faf8fc' });
                $item.find('.btn-modal-apply-blind-voucher').text('Apply').css({ background: '#fff', color: '#6a4f8c' });
            }
        });
    },

    showToast: function(message) {
        // Fallback or hook to existing showToast from blind-date.js
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
    },

    ensureCheckmarks: function() {
        $('.blind-coupon-card').each(function () {
            const $card = $(this);

            if ($card.find('.checkmark-badge').length === 0) {
                $card.css('position', 'relative');
                $card.append(`
                    <div class="checkmark-badge" style="
                        display: none;
                        position: absolute;
                        top: -7px;
                        right: -7px;
                        width: 20px;
                        height: 20px;
                        border-radius: 50%;
                        background: #6a4f8c;
                        color: #fff;
                        align-items: center;
                        justify-content: center;
                        font-size: 0.7rem;
                        z-index: 5;
                        box-shadow: 0 2px 6px rgba(0,0,0,0.15);
                    ">
                        <i class="fas fa-check"></i>
                    </div>
                `);
            }
        });
    }
};

// Controller
const BlindVoucherController = {
    init: function() {
        this.bindEvents();
        
        // Listen to global voucher loaded event to render vouchers if detail section is visible
        $(document).on('vouchersLoaded', () => {
             if ($('#blind-date-details-section').is(':visible') && window.currentBlindBookData) {
                 this.loadAndRenderVouchers(window.currentBlindBookData);
             }
        });
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
            .on('click.seeAllBlindVouchers', '#btn-see-all-blind-vouchers', (e) => {
                e.preventDefault();
                BlindVoucherView.syncUI(BlindVoucherModel.selectedVouchers);
                $('#blind-vouchers-modal').fadeIn(250);
            });

        $(document)
            .off('click.blindModalVoucher')
            .on('click.blindModalVoucher', '#blind-vouchers-modal .modal-blind-voucher-item', (e) => {
                e.preventDefault();
                const code = $(e.currentTarget).attr('data-code');
                if (code) this.handleToggleVoucher(code);
            });

        $(document)
            .off('click.blindModalVoucherButton')
            .on('click.blindModalVoucherButton', '#blind-vouchers-modal .btn-modal-apply-blind-voucher', (e) => {
                e.preventDefault();
                e.stopPropagation();
                const code = $(e.currentTarget).closest('.modal-blind-voucher-item').attr('data-code');
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
                } else {
                    BlindVoucherView.showToast('No mystery voucher selected.');
                }
                BlindVoucherView.syncUI(BlindVoucherModel.selectedVouchers);
            });
    },

    loadAndRenderVouchers: async function(blindBookData) {
        if (!blindBookData) return;
        
        BlindVoucherModel.clearVouchers();
        const vouchers = await BlindVoucherModel.getApplicableVouchers(blindBookData.categoryId, blindBookData.category);
        BlindVoucherModel.applicableVouchers = vouchers;
        
        BlindVoucherView.renderVouchers(vouchers);
        BlindVoucherView.renderModalVouchers(vouchers);
        BlindVoucherView.ensureCheckmarks();
        BlindVoucherView.syncUI(BlindVoucherModel.selectedVouchers);
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
        BlindVoucherView.syncUI(BlindVoucherModel.selectedVouchers);
    }
};

// Export to window
window.BookBlossomBlindDateVouchers = BlindVoucherController;

$(document).ready(function() {
    BlindVoucherController.init();
});
