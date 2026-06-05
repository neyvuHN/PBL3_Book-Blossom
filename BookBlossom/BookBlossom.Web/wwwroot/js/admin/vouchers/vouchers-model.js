function formatCompactVND(val) {
    if (!val || val === 0) return '0';
    if (val >= 1e9) {
        return (val / 1e9).toFixed(1).replace(/\.0$/, '') + 'B';
    }
    if (val >= 1e6) {
        return (val / 1e6).toFixed(1).replace(/\.0$/, '') + 'M';
    }
    if (val >= 1e3) {
        return (val / 1e3).toFixed(1).replace(/\.0$/, '') + 'K';
    }
    return val.toString();
}

class VouchersModel {
    constructor() {
        this.vouchers = [];
        this.mockCategories = [];
        this.mockBooks = [];
    }

    async init() {
        try {
            if (window.apiClient) {
                // Fetch real categories and books
                const cats = await window.apiClient.apiGet('/api/category');
                this.mockCategories = cats.map(c => ({ id: c.categoryID, name: c.categoryName }));

                const books = await window.apiClient.apiGet('/api/realbook?includeDiscontinued=true');
                this.mockBooks = books.map(b => ({ id: b.bookID, title: b.title }));
            }
        } catch (e) {
            console.error("Failed to load categories or books from API, falling back to static lists", e);
            this.mockCategories = [
                { id: 1, name: 'Literature & Fiction' },
                { id: 2, name: 'Business & Economics' },
                { id: 3, name: 'Self-Help & Skills' },
                { id: 4, name: 'Science Fiction' }
            ];
            this.mockBooks = [
                { id: 9001, title: 'The Great Gatsby' },
                { id: 9002, title: 'Atomic Habits' },
                { id: 9003, title: 'Dune' },
                { id: 9004, title: 'The Secret Garden' }
            ];
        }
    }

    async getAllVouchers(forceFetch = false) {
        if (this.vouchers.length > 0 && !forceFetch) {
            return this.vouchers;
        }
        if (!window.apiClient) return [];
        try {
            const rawVouchers = await window.apiClient.apiGet('/api/management/Voucher');
            
            let allStats = [];
            try {
                allStats = await window.apiClient.apiGet('/api/management/Voucher/stats/all');
            } catch (err) {
                console.warn('Could not load all stats array', err);
            }
            
            const statsMap = {};
            if (Array.isArray(allStats)) {
                allStats.forEach(s => {
                    statsMap[s.voucherID] = s;
                });
            }

            // Map raw vouchers to frontend DTO structure
            const mapped = [];
            for (const v of rawVouchers) {
                let stats = statsMap[v.voucherID] || { roi: 0, usageRate: 0, totalRevenueGenerated: 0, totalDiscountGranted: 0 };

                const invStatusMap = { 0: 'Draft', 1: 'Schedule', 2: 'Active', 3: 'Pause', 4: 'End' };
                const invRankMap = { 0: 'None', 1: 'Bronze', 2: 'Silver', 3: 'Gold', 4: 'Diamond' };

                mapped.push({
                    id: v.voucherID,
                    campaignName: v.voucherName,
                    code: v.voucherCode,
                    type: v.discountType,
                    value: v.discountValue,
                    maxDiscount: v.maxDiscountAmount,
                    minOrder: v.minOrderValue,
                    minPlan: { 0: 'None', 1: 'Basic', 2: 'Pro' }[v.minPlan] || 'None',
                    minScore: v.minReputationRequired,
                    minRank: invRankMap[v.membershipRankRequired] || 'None',
                    budget: v.totalLimit,
                    used: v.usedCount,
                    scope: (v.applicableCategoryIDs?.length > 0 && v.applicableBookIDs?.length > 0) ? 'Both' : 
                           (v.applicableCategoryIDs?.length > 0 ? 'SpecificCategory' : 
                           (v.applicableBookIDs?.length > 0 ? 'SpecificBook' : 'All')),
                    startDate: v.startDate ? v.startDate.substring(0, 16) : '',
                    endDate: v.endDate ? v.endDate.substring(0, 16) : '',
                    stackable: v.isStackable,
                    autoRestore: v.isAutoRefundable,
                    status: invStatusMap[v.statusVoucher] || 'Draft',
                    roi: stats.roi ? `${stats.roi}x` : '0.00x',
                    selectedCategories: v.applicableCategoryIDs || [],
                    selectedBooks: v.applicableBookIDs || [],
                    stats: stats
                });
            }
            this.vouchers = mapped;
            return this.vouchers;
        } catch (e) {
            console.error("Failed to load vouchers", e);
            if (window.apiClient && window.apiClient.showToast) {
                window.apiClient.showToast("Failed to load vouchers from API.", "error");
            }
            return [];
        }
    }

    async getVoucherById(id) {
        if (!window.apiClient) return null;
        try {
            const v = await window.apiClient.apiGet(`/api/management/Voucher/${id}`);
            const invStatusMap = { 0: 'Draft', 1: 'Schedule', 2: 'Active', 3: 'Pause', 4: 'End' };
            const invRankMap = { 0: 'None', 1: 'Bronze', 2: 'Silver', 3: 'Gold', 4: 'Diamond' };

            return {
                id: v.voucherID,
                campaignName: v.voucherName,
                code: v.voucherCode,
                type: v.discountType,
                value: v.discountValue,
                maxDiscount: v.maxDiscountAmount,
                minOrder: v.minOrderValue,
                minPlan: { 0: 'None', 1: 'Basic', 2: 'Pro', 3: 'Premium' }[v.minPlan] || 'None',
                minScore: v.minReputationRequired,
                minRank: invRankMap[v.membershipRankRequired] || 'None',
                budget: v.totalLimit,
                used: v.usedCount,
                scope: (v.applicableCategoryIDs?.length > 0 && v.applicableBookIDs?.length > 0) ? 'Both' : 
                       (v.applicableCategoryIDs?.length > 0 ? 'SpecificCategory' : 
                       (v.applicableBookIDs?.length > 0 ? 'SpecificBook' : 'All')),
                startDate: v.startDate ? v.startDate.substring(0, 16) : '',
                endDate: v.endDate ? v.endDate.substring(0, 16) : '',
                stackable: v.isStackable,
                autoRestore: v.isAutoRefundable,
                status: invStatusMap[v.statusVoucher] || 'Draft',
                selectedCategories: v.applicableCategoryIDs || [],
                selectedBooks: v.applicableBookIDs || []
            };
        } catch (e) {
            console.error("Failed to get voucher by ID", e);
            return null;
        }
    }

    async addVoucher(voucher) {
        if (!window.apiClient) return null;
        const rankMap = { 'None': 0, 'Bronze': 1, 'Silver': 2, 'Gold': 3, 'Diamond': 4 };
        const statusMap = { 'Draft': 0, 'Schedule': 1, 'Active': 2, 'Pause': 3, 'End': 4 };
        const payload = {
            voucherName: voucher.campaignName,
            voucherCode: voucher.code,
            discountType: voucher.type, // "Fixed" or "Percentage"
            discountValue: parseFloat(voucher.value),
            maxDiscountAmount: parseFloat(voucher.maxDiscount || 0),
            minOrderValue: parseFloat(voucher.minOrder || 0),
            totalLimit: parseInt(voucher.budget),
            startDate: voucher.startDate,
            endDate: voucher.endDate,
            minReputationRequired: parseInt(voucher.minScore || 0),
            membershipRankRequired: rankMap[voucher.minRank] || 0,
            minPlan: { 'None': 0, 'Basic': 1, 'Pro': 2 }[voucher.minPlan] || 0,
            isForNewUser: false,
            requiredBadgeID: null,
            isStackable: !!voucher.stackable,
            isAutoRefundable: !!voucher.autoRestore,
            maxUsagePerUser: 1,
            statusVoucher: statusMap[voucher.status] ?? 0,
            applicableCategoryIDs: voucher.scope === 'SpecificCategory' || voucher.scope === 'Both' ? voucher.selectedCategories : [],
            applicableBookIDs: voucher.scope === 'SpecificBook' || voucher.scope === 'Both' ? voucher.selectedBooks : []
        };

        try {
            const created = await window.apiClient.apiPost('/api/management/Voucher', payload);
            if (window.apiClient.showToast) {
                window.apiClient.showToast("Voucher created successfully!", "success");
            }
            return created;
        } catch (e) {
            console.error("Failed to create voucher", e);
            if (window.apiClient.showToast) {
                window.apiClient.showToast(e.message || "Failed to create voucher.", "error");
            }
            throw e;
        }
    }

    async updateVoucher(voucher) {
        if (!window.apiClient) return false;
        const rankMap = { 'None': 0, 'Bronze': 1, 'Silver': 2, 'Gold': 3, 'Diamond': 4 };
        const statusMap = { 'Draft': 0, 'Schedule': 1, 'Active': 2, 'Pause': 3, 'End': 4 };
        
        const payload = {
            voucherName: voucher.campaignName,
            discountType: voucher.type,
            discountValue: parseFloat(voucher.value),
            maxDiscountAmount: parseFloat(voucher.maxDiscount || 0),
            minOrderValue: parseFloat(voucher.minOrder || 0),
            totalLimit: parseInt(voucher.budget),
            startDate: voucher.startDate,
            endDate: voucher.endDate,
            statusVoucher: statusMap[voucher.status] ?? 0,
            minReputationRequired: parseInt(voucher.minScore || 0),
            membershipRankRequired: rankMap[voucher.minRank] || 0,
            minPlan: { 'None': 0, 'Basic': 1, 'Pro': 2 }[voucher.minPlan] || 0,
            isForNewUser: false,
            requiredBadgeID: null,
            isStackable: !!voucher.stackable,
            isAutoRefundable: !!voucher.autoRestore,
            maxUsagePerUser: 1,
            applicableCategoryIDs: voucher.scope === 'SpecificCategory' || voucher.scope === 'Both' ? voucher.selectedCategories : [],
            applicableBookIDs: voucher.scope === 'SpecificBook' || voucher.scope === 'Both' ? voucher.selectedBooks : []
        };

        try {
            await window.apiClient.apiPut(`/api/management/Voucher/${voucher.id}`, payload);
            if (window.apiClient.showToast) {
                window.apiClient.showToast("Voucher updated successfully!", "success");
            }
            return true;
        } catch (e) {
            console.error("Failed to update voucher", e);
            if (window.apiClient.showToast) {
                window.apiClient.showToast(e.message || "Failed to update voucher.", "error");
            }
            throw e;
        }
    }


}
