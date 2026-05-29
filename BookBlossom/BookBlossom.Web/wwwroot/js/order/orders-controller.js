class OrdersController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
    }

    init() {
        // Initial setup
        this.updateView();
        this.updateBadge();

        // Bind events
        this.view.bindTabChange(this.handleTabChange.bind(this));
        this.view.bindSearch(this.handleSearch.bind(this));
    }

    handleTabChange(tab) {
        this.model.setTab(tab);
        this.updateView();
    }

    handleSearch(query) {
        this.model.setSearchQuery(query);
        this.updateView();
    }

    updateView() {
        const filteredOrders = this.model.getFilteredOrders();
        this.view.renderOrders(filteredOrders);
    }

    updateBadge() {
        const toReceiveCount = this.model.getToReceiveCount();
        this.view.updateToReceiveBadge(toReceiveCount);
    }
}
