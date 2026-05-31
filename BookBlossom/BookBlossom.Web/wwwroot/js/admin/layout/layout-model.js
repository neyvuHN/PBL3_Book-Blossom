/**
 * Frontend MVC - Layout Model
 * Manages the state of the admin layout, such as sidebar collapse status.
 */
class LayoutModel {
    constructor() {
        this._storageKey = 'bookblossom_admin_sidebar_collapsed';
        this.isCollapsed = this._loadState();
    }

    /**
     * Loads the sidebar collapsed state from localStorage.
     * @returns {boolean} True if collapsed, false otherwise.
     * @private
     */
    _loadState() {
        const stored = localStorage.getItem(this._storageKey);
        return stored === 'true';
    }

    /**
     * Toggles the collapsed state and saves it.
     * @returns {boolean} The new collapsed state.
     */
    toggleSidebar() {
        this.isCollapsed = !this.isCollapsed;
        localStorage.setItem(this._storageKey, this.isCollapsed);
        return this.isCollapsed;
    }
}
