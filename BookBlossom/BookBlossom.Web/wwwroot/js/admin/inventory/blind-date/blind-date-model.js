class BlindDateModel {
    constructor() {
        this.blindDates = [];
        this.nextId = 1;
    }

    addBlindDate(data) {
        // Format barcode based on requirement: book.Barcode = $"BLD{DateTime.UtcNow:yyyyMMddHHmmssfff}{book.BlindBookID}";
        const now = new Date();
        const dateStr = now.toISOString().replace(/[-:T.Z]/g, '').slice(0, 17); // e.g. 20260531123456789
        const bdId = this.nextId++;
        
        const newPackage = {
            id: bdId,
            realBookId: data.realBookId,
            realBookTitle: data.realBookTitle,
            realBookImage: data.realBookImage,
            price: data.price,
            quantity: data.quantity,
            keywords: data.keywords,
            quotes: data.quotes,
            hashtags: data.hashtags,
            images: data.images, // Array of File objects or URLs for preview
            stockInfo: data.stockInfo,
            barcode: `BLD${dateStr}${bdId}`,
            createdAt: new Date().toISOString()
        };
        
        this.blindDates.push(newPackage);
        return newPackage;
    }

    updateBlindDate(id, data) {
        const index = this.blindDates.findIndex(b => b.id === id);
        if (index !== -1) {
            this.blindDates[index] = {
                ...this.blindDates[index],
                price: data.price,
                quantity: data.quantity,
                keywords: data.keywords,
                quotes: data.quotes,
                hashtags: data.hashtags,
                images: data.images
            };
            return this.blindDates[index];
        }
        return null;
    }

    getBlindDate(id) {
        return this.blindDates.find(b => b.id === id);
    }

    getBlindDates() {
        return this.blindDates;
    }
}
