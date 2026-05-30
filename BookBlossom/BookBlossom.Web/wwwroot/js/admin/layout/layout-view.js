/**
 * Frontend MVC - Layout View
 * Manages DOM manipulation and class states for the admin layout.
 */
class LayoutView {
    constructor() {
        this.wrapper = document.querySelector('.admin-wrapper');
        this.toggleBtn = document.getElementById('sidebar-toggle-btn');
    }

    /**
     * Applies the sidebar state to the DOM wrapper.
     * @param {boolean} isCollapsed - True if the sidebar should be hidden, false otherwise.
     */
    setSidebarState(isCollapsed) {
        if (isCollapsed) {
            this.wrapper.classList.add('sidebar-collapsed');
        } else {
            this.wrapper.classList.remove('sidebar-collapsed');
        }
    }

    /**
     * Binds the toggle button click event to a controller handler.
     * @param {Function} handler - The callback function to execute on click.
     */
    bindToggleSidebar(handler) {
        if (this.toggleBtn) {
            this.toggleBtn.addEventListener('click', (e) => {
                e.preventDefault();
                handler();
            });
        }
    }
}
