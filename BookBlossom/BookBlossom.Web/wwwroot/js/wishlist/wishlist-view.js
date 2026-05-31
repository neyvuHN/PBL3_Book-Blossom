class WishlistView {
    constructor() {
        this.container = document.getElementById('wishlist-grid');
        this.template = document.getElementById('wishlist-card-template');
        this.loadingState = document.getElementById('wishlist-loading');
        this.contentState = document.getElementById('wishlist-content');
        this.emptyState = document.getElementById('empty-wishlist-state');
        this.countBadge = document.getElementById('wishlist-count');
        this.btnClearAll = document.getElementById('btn-clear-wishlist');
    }

    showLoading() {
        this.loadingState.style.display = 'block';
        this.contentState.style.display = 'none';
        this.emptyState.style.display = 'none';
        this.btnClearAll.style.display = 'none';
    }

    hideLoading() {
        this.loadingState.style.display = 'none';
    }

    render(items) {
        this.hideLoading();

        this.countBadge.textContent = `${items.length} ${items.length === 1 ? 'item' : 'items'}`;

        if (items.length === 0) {
            this.contentState.style.display = 'none';
            this.emptyState.style.display = 'block';
            this.btnClearAll.style.display = 'none';
            return;
        }

        this.emptyState.style.display = 'none';
        this.contentState.style.display = 'block';
        this.btnClearAll.style.display = 'inline-block';
        
        this.container.innerHTML = '';
        
        items.forEach(item => {
            const clone = this.template.content.cloneNode(true);
            const card = clone.querySelector('.wishlist-card');
            
            // Image
            const img = clone.querySelector('.wishlist-card-img');
            img.src = item.imageUrl;
            img.alt = item.title;
            
            // Badge
            const badgeContainer = clone.querySelector('.wishlist-badge-container');
            if (item.isBlindDate) {
                badgeContainer.innerHTML = '<span class="badge blind-date">Blind Date</span>';
            }
            
            const imgWrapper = clone.querySelector('.wishlist-card-img-wrapper');
            imgWrapper.dataset.id = item.id;
            imgWrapper.dataset.isBlind = item.isBlindDate;
            imgWrapper.style.cursor = 'pointer';
            
            const titleEl = clone.querySelector('.wishlist-book-title');
            titleEl.textContent = item.title;
            titleEl.dataset.id = item.id;
            titleEl.dataset.isBlind = item.isBlindDate;

            clone.querySelector('.wishlist-book-author').textContent = item.author;
            const formattedPrice = new Intl.NumberFormat('vi-VN').format(item.price) + ' VNĐ';
            clone.querySelector('.wishlist-book-price').textContent = formattedPrice;
            
            // Setup events
            const btnRemove = clone.querySelector('.btn-remove-wishlist');
            btnRemove.dataset.id = item.id;
            
            const btnAddToCart = clone.querySelector('.btn-add-to-cart-wishlist');
            btnAddToCart.dataset.id = item.id;
            
            this.container.appendChild(clone);
        });
    }

    bindRemoveItem(handler) {
        this.container.addEventListener('click', e => {
            const btn = e.target.closest('.btn-remove-wishlist');
            if (btn) {
                e.stopPropagation();
                const id = btn.dataset.id;
                handler(id);
            }
        });
    }

    bindClearAll(handler) {
        const modal = document.getElementById('wishlist-confirm-modal');
        if (!modal) {
            // Fallback if modal is not in DOM
            this.btnClearAll.addEventListener('click', () => {
                if (confirm("Are you sure you want to clear your wishlist?")) {
                    handler();
                }
            });
            return;
        }

        const btnOk = document.getElementById('btn-wishlist-confirm-ok');
        const btnCancel = document.getElementById('btn-wishlist-confirm-cancel');
        const backdrop = modal.querySelector('.modal-backdrop');

        this.btnClearAll.addEventListener('click', () => {
            modal.style.display = 'flex';
        });

        const closeModal = () => {
            modal.style.display = 'none';
        };

        btnCancel.addEventListener('click', closeModal);
        if (backdrop) {
            backdrop.addEventListener('click', closeModal);
        }

        btnOk.onclick = () => {
            closeModal();
            handler();
        };
    }

    bindAddToCart(handler) {
        this.container.addEventListener('click', e => {
            const btn = e.target.closest('.btn-add-to-cart-wishlist');
            if (btn) {
                e.stopPropagation();
                const id = btn.dataset.id;
                handler(id);
            }
        });
    }

    bindNavigateToDetails(handler) {
        this.container.addEventListener('click', e => {
            const titleEl = e.target.closest('.wishlist-book-title');
            const imgEl = e.target.closest('.wishlist-card-img-wrapper');
            
            const target = titleEl || imgEl;
            
            if (target && !e.target.closest('.btn-remove-wishlist') && !e.target.closest('.btn-add-to-cart-wishlist')) {
                const id = target.dataset.id;
                const isBlind = target.dataset.isBlind === 'true';
                handler(id, isBlind);
            }
        });
    }
}
