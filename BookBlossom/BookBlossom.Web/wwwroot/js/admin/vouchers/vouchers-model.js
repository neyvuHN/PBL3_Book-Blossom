class VouchersModel {
    constructor() {
        this.vouchers = [
            {
                id: 1,
                code: 'SUMMER2024',
                type: 'Percentage',
                value: 20,
                maxDiscount: 50000,
                minOrder: 150000,
                minPlan: 'Basic',
                minScore: 50,
                minBadges: 1,
                budget: 100,
                used: 45,
                scope: 'All',
                startDate: '2024-06-01T00:00',
                endDate: '2024-06-30T23:59',
                stackable: false,
                revocable: true,
                status: 'Active',
                roi: '12.5M'
            },
            {
                id: 2,
                code: 'WELCOMEBK',
                type: 'Fixed',
                value: 30000,
                maxDiscount: 30000,
                minOrder: 100000,
                minPlan: 'None',
                minScore: 0,
                minBadges: 0,
                budget: 500,
                used: 0,
                scope: 'SpecificCategory',
                startDate: '2024-07-01T00:00',
                endDate: '2024-07-31T23:59',
                stackable: true,
                revocable: false,
                status: 'Schedule',
                roi: '0'
            },
            {
                id: 3,
                code: 'VIPONLY50',
                type: 'Percentage',
                value: 50,
                maxDiscount: 200000,
                minOrder: 500000,
                minPlan: 'Premium',
                minScore: 90,
                minBadges: 5,
                budget: 50,
                used: 50,
                scope: 'All',
                startDate: '2024-01-01T00:00',
                endDate: '2024-01-31T23:59',
                stackable: true,
                revocable: true,
                status: 'End',
                roi: '45.2M'
            }
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
        this.vouchers.push(voucher);
        return voucher;
    }

    updateVoucher(updatedVoucher) {
        const index = this.vouchers.findIndex(v => v.id === parseInt(updatedVoucher.id));
        if (index !== -1) {
            // Keep used and roi properties
            updatedVoucher.used = this.vouchers[index].used;
            updatedVoucher.roi = this.vouchers[index].roi;
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
