class CategoryCarouselController {
    constructor(view) {
        this.view = view;
    }

    init() {
        this.bindEvents();
        this.view.updateScrollButtons();
    }

    bindEvents() {
        // Event listeners for scroll buttons
        this.view.$btnLeft.off('click.carousel').on('click.carousel', (e) => {
            e.preventDefault();
            this.view.scrollLeft();
        });

        this.view.$btnRight.off('click.carousel').on('click.carousel', (e) => {
            e.preventDefault();
            this.view.scrollRight();
        });

        // Event listener for container scroll to update button visibility
        this.view.$container.off('scroll.carousel').on('scroll.carousel', () => {
            this.view.updateScrollButtons();
        });

        // Event listener for window resize to recalculate scroll widths
        $(window).off('resize.carousel').on('resize.carousel', () => {
            this.view.updateScrollButtons();
        });

        // Listen for custom event from explore.js when categories are re-rendered
        $(document).off('categoriesRendered.carousel').on('categoriesRendered.carousel', () => {
            // Need a slight timeout to allow DOM to render new tags before calculating width
            setTimeout(() => {
                if (this.view.$container.length > 0) {
                    this.view.$container[0].scrollLeft = 0;
                }
                this.view.updateScrollButtons();
            }, 50);
        });
    }
}

// Initialize when document is ready
$(document).ready(() => {
    if (window.CategoryCarouselView) {
        const view = new window.CategoryCarouselView();
        const controller = new CategoryCarouselController(view);
        controller.init();
        
        // Expose controller globally if needed
        window.CategoryCarouselController = controller;
    }
});
