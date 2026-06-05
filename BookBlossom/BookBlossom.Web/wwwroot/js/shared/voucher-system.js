// Centralized voucher fetching and rendering
window.GlobalVouchers = {
    vouchers: [],
    
    // Default color palette for vouchers
    colors: [
        { textColor: '#f07c7c', tagBg: '#ffebee', borderColor: '#f07c7c', bg: '#fffdfb' },
        { textColor: '#2b6cb0', tagBg: '#eef7fc', borderColor: '#2b6cb0', bg: '#fdfcff' },
        { textColor: '#d53f8c', tagBg: '#fdf2f8', borderColor: '#d53f8c', bg: '#fffafc' },
        { textColor: '#dd6b20', tagBg: '#feebc8', borderColor: '#dd6b20', bg: '#fffaf0' },
        { textColor: '#319795', tagBg: '#e6fffa', borderColor: '#319795', bg: '#f0fdf4' }
    ],

    async init() {
        try {
            // Use apiClient if available, otherwise fallback to standard fetch
            let data = null;
            if (typeof apiClient !== 'undefined' && apiClient.apiGet) {
                const json = await apiClient.apiGet('/api/VoucherAPI/active');
                if (json && json.success) data = json.data;
            } else {
                const res = await fetch('/api/VoucherAPI/active');
                const json = await res.json();
                if (json && json.success) data = json.data;
            }

            if (data && Array.isArray(data)) {
                this.vouchers = data.map((v, i) => {
                    const c = this.colors[i % this.colors.length];
                    const vType = v.discountType.toLowerCase() === 'percentage' ? 'percent' : 'fixed';
                    const expiryDate = new Date(v.endDate);
                    const expiryStr = expiryDate.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
                    
                    return {
                        code: v.voucherCode,
                        title: v.voucherName,
                        desc: `Min purchase ${v.minOrderValue.toLocaleString('vi-VN')}đ.`,
                        discount: vType === 'percent' ? v.discountValue / 100 : v.discountValue,
                        maxDiscount: v.maxDiscountAmount,
                        type: vType,
                        minOrder: v.minOrderValue,
                        expiry: expiryStr,
                        textColor: c.textColor,
                        tagBg: c.tagBg,
                        borderColor: c.borderColor,
                        bg: c.bg,
                        applicableCategories: v.applicableCategoryIDs || [],
                        applicableBooks: v.applicableBookIDs || [],
                        isStackable: v.isStackable,
                        isAutoRefundable: v.isAutoRefundable,
                        minPlan: v.minPlan,
                        minReputation: v.minReputationRequired,
                        minRank: v.membershipRankRequired
                    };
                });
                
                // Expose to window for legacy scripts (like product-details.js)
                window.VOUCHERS_DATA = this.vouchers;
                
                this.renderAllModals();
                $(document).trigger('vouchersLoaded');
            }
        } catch (error) {
            console.error("Failed to fetch vouchers:", error);
        }
    },

    renderAllModals() {
        this.renderVoucherList('#blossom-vouchers-container', false);
        this.renderVoucherList('#blossom-vouchers-list', false);
        
        this.renderVoucherList('#blind-vouchers-container', true);
        this.renderVoucherList('#mystery-vouchers-list', true);
    },

    renderVoucherList(containerSelector, isBlind) {
        const $container = $(containerSelector);
        if (!$container.length) return;
        
        $container.empty();
        
        if (this.vouchers.length === 0) {
            $container.html('<div style="padding: 20px; text-align: center; color: #888;">No vouchers available at the moment.</div>');
            return;
        }

        this.vouchers.forEach(v => {
            const html = isBlind ? this.getBlindVoucherHtml(v) : this.getNormalVoucherHtml(v);
            $container.append(html);
        });
    },

    getNormalVoucherHtml(v) {
        return `
            <div class="modal-voucher-item" data-code="${v.code}" style="display: flex; border: 1.5px dashed ${v.borderColor}; border-radius: 12px; overflow: hidden; background: ${v.bg}; transition: all 0.25s; cursor: pointer; position: relative;">
                <div style="background: ${v.tagBg}; padding: 20px; display: flex; flex-direction: column; align-items: center; justify-content: center; min-width: 110px; border-right: 1.5px dashed ${v.borderColor}; text-align: center;">
                    <span style="font-size: 0.85rem; font-weight: 700; color: ${v.textColor}; letter-spacing: 0.5px;">${v.code}</span>
                    <span style="font-size: 0.65rem; color: ${v.textColor}; font-weight: 600; margin-top: 4px; background: #fff; padding: 2px 6px; border-radius: 10px;">Code</span>
                </div>
                <div style="padding: 15px; flex-grow: 1; display: flex; flex-direction: column; justify-content: center; text-align: left;">
                    <h4 style="font-size: 0.95rem; font-weight: 700; color: #333; margin: 0 0 4px 0;">${v.title}</h4>
                    <p style="font-size: 0.78rem; color: #666; margin: 0 0 6px 0;">${v.desc}</p>
                    <div style="font-size: 0.7rem; color: #777; margin-bottom: 4px; display: flex; flex-wrap: wrap; gap: 4px;">
                        <span style="background: #f0f0f0; padding: 2px 6px; border-radius: 4px;">${v.isStackable ? 'Stackable' : 'Non-stackable'}</span>
                        <span style="background: #f0f0f0; padding: 2px 6px; border-radius: 4px;">${v.isAutoRefundable ? 'Refundable' : 'Non-refundable'}</span>
                        ${v.minPlan > 0 ? `<span style="background: #e6f7ff; padding: 2px 6px; border-radius: 4px;">Min Plan: ${window.getPlanName(v.minPlan)}</span>` : ''}
                        ${v.minReputation > 0 ? `<span style="background: #fff0f6; padding: 2px 6px; border-radius: 4px;">Min Score: ${v.minReputation}</span>` : ''}
                        ${v.minRank > 0 ? `<span style="background: #f6ffed; padding: 2px 6px; border-radius: 4px;">Min Rank: ${window.getRankName(v.minRank)}</span>` : ''}
                    </div>
                    <span style="font-size: 0.7rem; color: #aaa;">Expiry: ${v.expiry}</span>
                </div>
                <div style="padding: 15px; display: flex; align-items: center; justify-content: center; min-width: 90px; z-index: 2;">
                    <button class="btn-modal-apply-voucher" style="background: #fff; color: #C2185B; border: 1.5px solid #C2185B; border-radius: 20px; padding: 6px 14px; font-size: 0.8rem; font-weight: 700; cursor: pointer; transition: all 0.2s;">Apply</button>
                </div>
            </div>
        `;
    },

    getBlindVoucherHtml(v) {
        // Blind dates calculate discount from data-discount directly in blind-date-details.js 
        // We set it to actual VND value if fixed, or empty if percentage (but existing logic expected absolute number).
        // For simplicity, we just pass what the existing UI expected.
        const blindVal = v.type === 'percent' ? v.maxDiscount : v.discount;
        
        return `
            <div class="modal-blind-voucher-item" data-code="${v.code}" data-discount="${blindVal}" style="display: flex; border: 1.5px dashed #a291b5; border-radius: 12px; overflow: hidden; background: #faf8fc; transition: all 0.25s; cursor: pointer; position: relative;">
                <div style="background: #e8e3f0; padding: 20px; display: flex; flex-direction: column; align-items: center; justify-content: center; min-width: 110px; border-right: 1.5px dashed #a291b5; text-align: center;">
                    <span style="font-size: 0.85rem; font-weight: 700; color: #6a4f8c; letter-spacing: 0.5px;">${v.code}</span>
                    <span style="font-size: 0.65rem; color: #6a4f8c; font-weight: 600; margin-top: 4px; background: #fff; padding: 2px 6px; border-radius: 10px;">Code</span>
                </div>
                <div style="padding: 15px; flex-grow: 1; display: flex; flex-direction: column; justify-content: center; text-align: left;">
                    <h4 style="font-size: 0.95rem; font-weight: 700; color: #333; margin: 0 0 4px 0;">${v.title}</h4>
                    <p style="font-size: 0.78rem; color: #666; margin: 0 0 6px 0;">${v.desc}</p>
                    <div style="font-size: 0.7rem; color: #777; margin-bottom: 4px; display: flex; flex-wrap: wrap; gap: 4px;">
                        <span style="background: #f0f0f0; padding: 2px 6px; border-radius: 4px;">${v.isStackable ? 'Stackable' : 'Non-stackable'}</span>
                        <span style="background: #f0f0f0; padding: 2px 6px; border-radius: 4px;">${v.isAutoRefundable ? 'Refundable' : 'Non-refundable'}</span>
                        ${v.minPlan > 0 ? `<span style="background: #e6f7ff; padding: 2px 6px; border-radius: 4px;">Min Plan: ${window.getPlanName(v.minPlan)}</span>` : ''}
                        ${v.minReputation > 0 ? `<span style="background: #fff0f6; padding: 2px 6px; border-radius: 4px;">Min Score: ${v.minReputation}</span>` : ''}
                        ${v.minRank > 0 ? `<span style="background: #f6ffed; padding: 2px 6px; border-radius: 4px;">Min Rank: ${window.getRankName(v.minRank)}</span>` : ''}
                    </div>
                    <span style="font-size: 0.7rem; color: #aaa;">Expiry: ${v.expiry}</span>
                </div>
                <div style="padding: 15px; display: flex; align-items: center; justify-content: center; min-width: 90px; z-index: 2;">
                    <button class="btn-modal-apply-blind-voucher" style="background: #fff; color: #6a4f8c; border: 1.5px solid #6a4f8c; border-radius: 20px; padding: 6px 14px; font-size: 0.8rem; font-weight: 700; cursor: pointer; transition: all 0.2s;">Apply</button>
                </div>
            </div>
        `;
    },

    getBlindVoucherCardHtml(v) {
        const blindVal = v.type === 'percent' ? v.maxDiscount : v.discount;
        const saveText = v.type === 'percent' ? `Save ${v.discount}%` : `Save ${(v.discount / 1000).toLocaleString('vi-VN')}k VND`;
        
        return `
            <div class="blind-coupon-card" data-code="${v.code}" data-discount="${blindVal}" style="flex: 0 0 170px; background: #faf8fc; border: 1.5px dashed #a291b5; border-radius: 12px; padding: 12px; position: relative; box-shadow: 0 2px 6px rgba(0,0,0,0.01); transition: all 0.2s; cursor: pointer; text-align: left;" onmouseover="this.style.transform='translateY(-2px)'; this.style.borderColor='#6a4f8c';" onmouseout="this.style.transform='none'; this.style.borderColor='#a291b5';">
                <div class="checkmark-badge-blind" style="display: none; position: absolute; top: -6px; right: -6px; background: #6a4f8c; color: #fff; border-radius: 50%; width: 18px; height: 18px; font-size: 0.65rem; align-items: center; justify-content: center; z-index: 5; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"><i class="fas fa-check"></i></div>
                <div style="font-size: 0.7rem; font-weight: 700; color: #6a4f8c; background: #e8e3f0; padding: 2px 6px; border-radius: 4px; width: fit-content; margin-bottom: 6px;">${v.code}</div>
                <div style="font-size: 0.82rem; font-weight: 700; color: #333; margin-bottom: 4px;">${saveText}</div>
                <div style="font-size: 0.72rem; color: #888;">Min order ${(v.minOrder / 1000).toLocaleString('vi-VN')}k</div>
            </div>
        `;
    }
};

window.getPlanName = function(planId) {
    const plans = {0: 'Free', 1: 'Basic', 2: 'Pro', 3: 'Premium'};
    return plans[planId] || 'Basic';
};

window.getRankName = function(rankId) {
    const ranks = {0: 'None', 1: 'Bronze', 2: 'Silver', 3: 'Gold', 4: 'Platinum', 5: 'Diamond'};
    return ranks[rankId] || 'Bronze';
};

$(document).ready(function() {
    window.GlobalVouchers.init();
});

window.showVoucherError = function(message) {
    $('#voucher-error-modal').remove();

    const modalHtml = `
        <div id="voucher-error-modal" style="display:flex; position:fixed; inset:0; z-index:999999; background:rgba(0,0,0,0.5); align-items:center; justify-content:center;">
            <div style="background:#fff; border-radius:12px; width:90%; max-width:400px; padding:24px; text-align:center; box-shadow:0 10px 25px rgba(0,0,0,0.2); transform:scale(0.9); opacity:0; transition:all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);">
                <div style="width:60px; height:60px; border-radius:50%; background:#ffebee; color:#dc3545; display:flex; align-items:center; justify-content:center; font-size:2rem; margin:0 auto 16px;">
                    <i class="fas fa-exclamation-triangle"></i>
                </div>
                <h3 style="margin:0 0 10px; font-size:1.25rem; color:#333; font-weight:700;">Condition Not Met</h3>
                <p style="margin:0 0 24px; color:#666; font-size:0.95rem; line-height:1.5;">${message}</p>
                <button onclick="$('#voucher-error-modal').fadeOut(200, function(){ $(this).remove(); })" style="background:#C2185B; color:#fff; border:none; padding:10px 24px; border-radius:24px; font-weight:700; cursor:pointer; width:100%; font-size:1rem; transition:background 0.2s;">Got it</button>
            </div>
        </div>
    `;

    $('body').append(modalHtml);
    
    setTimeout(() => {
        $('#voucher-error-modal > div').css({ transform: 'scale(1)', opacity: '1' });
    }, 10);
};
