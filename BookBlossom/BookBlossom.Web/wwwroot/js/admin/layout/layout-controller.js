/**
 * Frontend MVC - Layout Controller
 * Binds DOM events and coordinates layout states between the Model and View.
 */
class LayoutController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
    }

    /**
     * Initializes layout states and event listeners.
     */
    init() {
        console.log("Layout MVC Controller initialized.");
        // Set the initial visual state on load based on persisted model preference
        this.view.setSidebarState(this.model.isCollapsed);

        // Bind events
        this.bindEvents();
    }

    /**
     * Binds event listeners to view elements.
     */
    bindEvents() {
        this.view.bindToggleSidebar(() => {
            const newState = this.model.toggleSidebar();
            this.view.setSidebarState(newState);
        });
    }
}
