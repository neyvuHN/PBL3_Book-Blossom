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
                        applicableBooks: v.applicableBookIDs || []
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
                    <span style="font-size: 0.7rem; color: #aaa;">Expiry: ${v.expiry}</span>
                </div>
                <div style="padding: 15px; display: flex; align-items: center; justify-content: center; min-width: 90px; z-index: 2;">
                    <button class="btn-modal-apply-blind-voucher" style="background: #fff; color: #6a4f8c; border: 1.5px solid #6a4f8c; border-radius: 20px; padding: 6px 14px; font-size: 0.8rem; font-weight: 700; cursor: pointer; transition: all 0.2s;">Apply</button>
                </div>
            </div>
        `;
    }
};

$(document).ready(function() {
    window.GlobalVouchers.init();
});
