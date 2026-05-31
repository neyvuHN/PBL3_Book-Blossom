class VouchersModel {
    constructor() {
        this.vouchers = [
            {
                id: 1,
                campaignName: 'Summer Sale 2024',
                code: 'SUMMER2024',
                type: 'Percentage',
                value: 20,
                maxDiscount: 50000,
                minOrder: 150000,
                minPlan: 'Basic',
                minScore: 50,
                minRank: 'Bronze',
                budget: 100,
                limitPerUser: 1,
                used: 45,
                scope: 'All',
                startDate: '2024-06-01T00:00',
                endDate: '2024-06-30T23:59',
                stackable: false,
                autoRestore: true,
                status: 'Active',
                roi: '12.5M',
                selectedCategories: [],
                selectedBooks: []
            },
            {
                id: 2,
                campaignName: 'Welcome New Members',
                code: 'WELCOMEBK',
                type: 'Fixed',
                value: 30000,
                maxDiscount: 30000,
                minOrder: 100000,
                minPlan: 'None',
                minScore: 0,
                minRank: 'None',
                budget: 500,
                limitPerUser: 2,
                used: 0,
                scope: 'SpecificCategory',
                startDate: '2024-07-01T00:00',
                endDate: '2024-07-31T23:59',
                stackable: true,
                autoRestore: false,
                status: 'Schedule',
                roi: '0',
                selectedCategories: [],
                selectedBooks: []
            },
            {
                id: 3,
                campaignName: 'VIP Appreciation',
                code: 'VIPONLY50',
                type: 'Percentage',
                value: 50,
                maxDiscount: 200000,
                minOrder: 500000,
                minPlan: 'Premium',
                minScore: 90,
                minRank: 'Gold',
                budget: 50,
                limitPerUser: 1,
                used: 50,
                scope: 'All',
                startDate: '2024-01-01T00:00',
                endDate: '2024-01-31T23:59',
                stackable: true,
                autoRestore: true,
                status: 'End',
                roi: '45.2M',
                selectedCategories: [],
                selectedBooks: []
            }
        ];

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

    getAllVouchers() {
        return this.vouchers;
    }

    getVoucherById(id) {
        return this.vouchers.find(v => v.id === parseInt(id));
    }

    addVoucher(voucher) {
        voucher.id = this.vouchers.length ? Math.max(...this.vouchers.map(v => v.id)) + 1 : 1;
        voucher.used = 0;
        voucher.roi = '0';
        voucher.selectedCategories = voucher.selectedCategories || [];
        voucher.selectedBooks = voucher.selectedBooks || [];
        this.vouchers.push(voucher);
        return voucher;
    }

    updateVoucher(updatedVoucher) {
        const index = this.vouchers.findIndex(v => v.id === parseInt(updatedVoucher.id));
        if (index !== -1) {
            // Keep used and roi properties
            updatedVoucher.used = this.vouchers[index].used;
            updatedVoucher.roi = this.vouchers[index].roi;
            updatedVoucher.selectedCategories = updatedVoucher.selectedCategories || [];
            updatedVoucher.selectedBooks = updatedVoucher.selectedBooks || [];
            this.vouchers[index] = updatedVoucher;
            return true;
        }
        return false;
    }

    deleteVoucher(id) {
        const index = this.vouchers.findIndex(v => v.id === parseInt(id));
        if (index !== -1) {
            this.vouchers.splice(index, 1);
            return true;
        }
        return false;
    }
}
