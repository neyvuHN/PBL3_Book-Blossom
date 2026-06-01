class BlindDateModel {
    constructor() {
        this.blindDates = [];
    }

    async loadBlindDates() {
        const response = await fetch('/api/blindbook', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
            }
        });
        if (!response.ok) {
            throw new Error(await response.text() || 'Failed to fetch blind books.');
        }
        const data = await response.json();
        this.blindDates = data.map(item => this.mapBackendToFrontend(item));
        return this.blindDates;
    }

    mapBackendToFrontend(item) {
        return {
            id: item.blindBookID,
            realBookId: item.realBookID,
            realBookTitle: item.realBookTitle || `Mystery Book #${item.blindBookID}`,
            realBookImage: `/images/Book/cover_${item.realBookID}.jpg`,
            price: item.price,
            quantity: item.status === 0 ? item.requestQuantity : item.stockQuantity, 
            requestQuantity: item.requestQuantity,
            stockQuantity: item.stockQuantity,
            keywords: item.keywords,
            quotes: item.quotes,
            hashtags: item.hashtags,
            images: [],
            stockInfo: `Current stock: ${item.stockQuantity}`,
            barcode: item.barcode || 'Awaiting Approval',
            realBookCategoryName: item.category,
            status: item.status, // Pending = 0, Approved = 1, Rejected = 2
            rejectReason: item.rejectReason,
            isLocked: item.isLocked,
            hasOrders: item.hasOrders
        };
    }

    getBlindDate(id) {
        return this.blindDates.find(b => b.id === id);
    }

    getBlindDates() {
        return this.blindDates;
    }

    async addBlindDate(data) {
        const payload = {
            realBookID: parseInt(data.realBookId, 10),
            keywords: data.keywords,
            quotes: data.quotes,
            category: data.realBookCategoryName,
            hashtags: data.hashtags,
            price: parseFloat(data.price),
            requestQuantity: parseInt(data.quantity, 10)
        };

        const response = await fetch('/api/blindbook', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const errData = await response.json().catch(() => ({}));
            throw new Error(errData.message || 'Failed to create blind date package request.');
        }

        return await response.json();
    }

    async updateBlindDate(id, data) {
        const payload = {
            keywords: data.keywords,
            quotes: data.quotes,
            category: data.realBookCategoryName,
            hashtags: data.hashtags,
            price: parseFloat(data.price)
        };

        const response = await fetch(`/api/blindbook/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const errData = await response.json().catch(() => ({}));
            throw new Error(errData.message || 'Failed to update blind date package.');
        }

        return await response.json();
    }

    async approveBlindDate(id) {
        const response = await fetch(`/api/blindbook/approve/${id}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
            }
        });

        if (!response.ok) {
            const errData = await response.json().catch(() => ({}));
            throw new Error(errData.message || 'Failed to approve blind date package.');
        }

        return await response.json();
    }

    async rejectBlindDate(id, reason) {
        const response = await fetch(`/api/blindbook/reject/${id}?reason=${encodeURIComponent(reason)}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
            }
        });

        if (!response.ok) {
            const errData = await response.json().catch(() => ({}));
            throw new Error(errData.message || 'Failed to reject blind date package.');
        }

        return await response.json();
    }

    async restockBlindDate(id, quantity) {
        const payload = {
            blindBookID: parseInt(id, 10),
            requestQuantity: parseInt(quantity, 10)
        };

        const response = await fetch('/api/blindbook/restock', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const errData = await response.json().catch(() => ({}));
            throw new Error(errData.message || 'Failed to request restocking.');
        }

        return await response.json();
    }

    async approveRestock(id, quantity) {
        const response = await fetch(`/api/blindbook/restock/approve/${id}?approvedQuantity=${parseInt(quantity, 10)}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
            }
        });

        if (!response.ok) {
            const errData = await response.json().catch(() => ({}));
            throw new Error(errData.message || 'Failed to approve restocking.');
        }

        return await response.json();
    }

    async toggleLock(id) {
        const response = await fetch(`/api/blindbook/toggle-lock/${id}`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
            }
        });

        if (!response.ok) {
            const errData = await response.json().catch(() => ({}));
            throw new Error(errData.message || 'Failed to toggle lock status.');
        }

        return await response.json();
    }
}
