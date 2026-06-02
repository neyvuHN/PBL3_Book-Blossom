/**
 * Frontend MVC - Model
 * Manages the state, filtering, and data modifications of the admin management screen.
 */
class UsersModel {
    constructor(initialData) {
        this.buyers = initialData.buyers || [];
        this.banned = initialData.banned || [];
        
        // Active Filter States
        this.activeTab = 'buyers'; // 'buyers', 'banned'
        this.searchQuery = '';
        this.filterRole = '';
        this.filterScore = ''; // 'high' (>80) or 'caution' (<50)
        
        // Popover Selected State
        this.selectedUserId = null;
        this.selectedUserRole = '';
    }

    /**
     * Finds a user object by ID from any of the lists.
     */
    findUserById(userId) {
        const lists = [this.buyers, this.banned];
        for (const list of lists) {
            const user = list.find(u => u.id === userId);
            if (user) return user;
        }
        return null;
    }

    /**
     * Filters a list based on search term, role dropdown, and score dropdown.
     */
    _filterList(list) {
        return list.filter(item => {
            // 1. Search Query Filter (Username, Email, ID)
            if (this.searchQuery) {
                const query = this.searchQuery.toLowerCase();
                const matchesUsername = item.username && item.username.toLowerCase().includes(query);
                const matchesEmail = item.email && item.email.toLowerCase().includes(query);
                const matchesId = item.id && item.id.toLowerCase().includes(query);
                
                if (!matchesUsername && !matchesEmail && !matchesId) {
                    return false;
                }
            }

            /* Commented out per request: Bỏ luôn filter theo role
            // 2. Role Filter Dropdown
            if (this.filterRole && item.role !== this.filterRole) {
                return false;
            }
            */

            // 3. Score Filter Dropdown
            if (this.filterScore) {
                const score = item.internalScore;
                if (this.activeTab === 'buyers' || this.activeTab === 'banned') {
                    // Reputation Score Thresholds: A (<80), B (<60), C (<30)
                    if (this.filterScore === 'A' && score >= 80) return false;
                    if (this.filterScore === 'B' && score >= 60) return false;
                    if (this.filterScore === 'C' && score >= 30) return false;
                }
            }

            return true;
        });
    }

    /**
     * Returns filtered buyers list.
     */
    getFilteredBuyers() {
        return this._filterList(this.buyers);
    }



    /**
     * Returns filtered banned list.
     */
    getFilteredBanned() {
        return this._filterList(this.banned);
    }

    /**
     * Updates a user's status (Active vs Banned) via backend API.
     * Moves users between active lists and the banned list if status changes.
     */
    async updateUserStatus(userId, newStatus) {
        const user = this.findUserById(userId);
        if (!user) return false;

        const isBanned = newStatus === 'Banned';
        
        if (window.apiClient) {
            const resp = await window.apiClient.apiPost(`/Admin/ToggleLock?userId=${userId}&isBanned=${isBanned}`);
            if (!resp || !resp.success) {
                throw new Error(resp?.message || "Failed to update user status on server");
            }
        }

        user.status = newStatus;

        // Move item to appropriate list if needed
        if (newStatus === 'Banned') {
            // Remove from buyers and append to banned
            this.buyers = this.buyers.filter(u => u.id !== userId);
            
            // Check if already in banned
            if (!this.banned.some(u => u.id === userId)) {
                this.banned.push(user);
            }
        } else if (newStatus === 'Active') {
            // Remove from banned
            this.banned = this.banned.filter(u => u.id !== userId);
            
            if (!this.buyers.some(u => u.id === userId)) {
                this.buyers.push(user);
            }
        }
        return true;
    }

    /**
     * Updates a user's role via backend API.
     * Moves users between Buyers and Staff if role transitions across categories.
     */
    async updateUserRole(userId, newRole, adminPassword) {
        const user = this.findUserById(userId);
        if (!user) return false;

        if (window.apiClient) {
            const resp = await window.apiClient.apiPost(`/Admin/UpdateRole?userId=${userId}&newRole=${newRole}&adminPassword=${encodeURIComponent(adminPassword)}`);
            if (!resp || !resp.success) {
                throw new Error(resp?.message || "Failed to update user role on server");
            }
        }

        const oldRole = user.role;
        user.role = newRole;

        const wasStaff = oldRole !== 'User' && oldRole !== 'Customer';
        const isStaff = newRole !== 'User' && newRole !== 'Customer';

        // Check if user is active (not currently banned)
        if (user.status === 'Active') {
            if (wasStaff && !isStaff) {
                // Demoted: Staff -> Buyer
                if (!this.buyers.some(u => u.id === userId)) {
                    this.buyers.push(user);
                }
            } else if (!wasStaff && isStaff) {
                // Promoted: Buyer -> Staff
                this.buyers = this.buyers.filter(u => u.id !== userId);
            }
        }
        return true;
    }

    /**
     * Updates a buyer's administrative notes.
     */
    async updateBuyerNote(userId, noteText) {
        const user = this.findUserById(userId);
        if (!user) return false;
        
        if (window.apiClient) {
            const resp = await window.apiClient.apiPost(`/Admin/UpdateBuyerNote?userId=${userId}&noteText=${encodeURIComponent(noteText)}`);
            if (!resp || !resp.success) {
                throw new Error(resp?.message || "Failed to save note to server");
            }
        }

        user.note = noteText;
        return true;
    }


}
