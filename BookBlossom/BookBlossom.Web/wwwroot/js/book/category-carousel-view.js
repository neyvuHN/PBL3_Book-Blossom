class CategoryCarouselView {
    constructor() {
        this.$container = $('#category-tags-container');
        this.$wrapper = $('.category-carousel-wrapper');
        this.$btnLeft = $('#btn-scroll-left');
        this.$btnRight = $('#btn-scroll-right');
    }

    updateScrollButtons() {
        const container = this.$container[0];
        if (!container) return;

        const maxScrollLeft = container.scrollWidth - container.clientWidth;
        
        // Show/hide buttons based on scroll position and content width
        if (container.scrollWidth > container.clientWidth) {
            if (container.scrollLeft > 0) {
                this.$btnLeft.css('display', 'flex');
            } else {
                this.$btnLeft.hide();
            }

            if (Math.ceil(container.scrollLeft) >= maxScrollLeft - 1) {
                this.$btnRight.hide();
            } else {
                this.$btnRight.css('display', 'flex');
            }
        } else {
            this.$btnLeft.hide();
            this.$btnRight.hide();
        }
    }

    scrollLeft() {
        const container = this.$container[0];
        if (container) {
            container.scrollBy({ left: -200, behavior: 'smooth' });
        }
    }

    scrollRight() {
        const container = this.$container[0];
        if (container) {
            container.scrollBy({ left: 200, behavior: 'smooth' });
        }
    }
}

window.CategoryCarouselView = CategoryCarouselView;
