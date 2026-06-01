/**
 * Frontend MVC - Model
 * Manages the state, filtering, and data modifications of the admin management screen.
 */
class UsersModel {
    constructor(initialData) {
        this.buyers = initialData.buyers || [];
        this.staff = initialData.staff || [];
        this.banned = initialData.banned || [];
        
        // Active Filter States
        this.activeTab = 'buyers'; // 'buyers', 'staff', 'banned'
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
        const lists = [this.buyers, this.staff, this.banned];
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
                } else if (this.activeTab === 'staff') {
                    // Internal Score: high (>80) or caution (<50)
                    if (this.filterScore === 'high' && score <= 80) return false;
                    if (this.filterScore === 'caution' && score >= 50) return false;
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
     * Returns filtered staff list.
     */
    getFilteredStaff() {
        return this._filterList(this.staff);
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
            // Remove from buyers or staff and append to banned
            this.buyers = this.buyers.filter(u => u.id !== userId);
            this.staff = this.staff.filter(u => u.id !== userId);
            
            // Check if already in banned
            if (!this.banned.some(u => u.id === userId)) {
                this.banned.push(user);
            }
        } else if (newStatus === 'Active') {
            // Remove from banned
            this.banned = this.banned.filter(u => u.id !== userId);
            
            // Re-allocate based on Role
            const isStaffRole = user.role !== 'User' && user.role !== 'Customer';
            if (isStaffRole) {
                if (!this.staff.some(u => u.id === userId)) {
                    this.staff.push(user);
                }
            } else {
                if (!this.buyers.some(u => u.id === userId)) {
                    this.buyers.push(user);
                }
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
                this.staff = this.staff.filter(u => u.id !== userId);
                if (!this.buyers.some(u => u.id === userId)) {
                    this.buyers.push(user);
                }
            } else if (!wasStaff && isStaff) {
                // Promoted: Buyer -> Staff
                this.buyers = this.buyers.filter(u => u.id !== userId);
                if (!this.staff.some(u => u.id === userId)) {
                    this.staff.push(user);
                }
            }
        }
        return true;
    }

    /**
     * Creates and adds a new staff member to the database via API.
     */
    async addStaffMember(staffData) {
        let newStaff = null;
        if (window.apiClient) {
            const resp = await window.apiClient.apiPost('/Admin/AddAdmin', {
                username: staffData.username,
                email: staffData.email,
                password: staffData.password,
                lastName: staffData.lastName,
                firstName: staffData.firstName,
                phoneNumber: staffData.phoneNumber,
                gender: staffData.gender,
                birthday: staffData.birthday,
                contractType: staffData.contractType,
                salary: parseFloat(staffData.salary),
                bankAccount: staffData.bankAccount,
                avatarUrl: staffData.avatarUrl || 'https://i.pravatar.cc/150?img=1',
                qualifications: staffData.qualifications || ''
            });

            if (!resp || !resp.success || !resp.user) {
                throw new Error(resp?.message || "Failed to create staff member on server");
            }
            newStaff = resp.user;
        } else {
            // Fallback for visual mock
            const today = new Date();
            const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
            const formattedDate = `${months[today.getMonth()]} ${String(today.getDate()).padStart(2, '0')}, ${today.getFullYear()}`;
            newStaff = {
                id: `staff_${Date.now()}`,
                username: staffData.username,
                email: staffData.email,
                role: 'Admin',
                plan: 'Pro',
                internalScore: 100,
                joinDate: formattedDate,
                status: 'Active',
                avatarUrl: staffData.avatarUrl || 'https://i.pravatar.cc/150?img=1'
            };
        }

        this.staff.push(newStaff);
        return newStaff;
    }

    /**
     * Updates a buyer's administrative notes.
     */
    updateBuyerNote(userId, noteText) {
        const user = this.findUserById(userId);
        if (!user) return false;
        
        user.note = noteText;
        return true;
    }

    /**
     * Updates an existing staff member's administrative records via backend API.
     */
    async updateStaffMember(userId, updatedData) {
        const staffObj = this.findUserById(userId);
        if (!staffObj) return null;

        if (window.apiClient) {
            const resp = await window.apiClient.apiPost(`/Admin/EditAdmin?userId=${userId}`, {
                username: updatedData.username,
                email: updatedData.email,
                password: updatedData.password || "123456",
                lastName: updatedData.lastName,
                firstName: updatedData.firstName,
                phoneNumber: updatedData.phoneNumber,
                gender: updatedData.gender,
                birthday: updatedData.birthday,
                contractType: updatedData.contractType,
                salary: parseFloat(updatedData.salary),
                bankAccount: updatedData.bankAccount,
                avatarUrl: updatedData.avatarUrl || staffObj.avatarUrl,
                qualifications: updatedData.qualifications || ''
            });

            if (!resp || !resp.success || !resp.user) {
                throw new Error(resp?.message || "Failed to update staff member on server");
            }

            // Sync updated details from backend response
            Object.assign(staffObj, resp.user);
        } else {
            // Update fields locally
            staffObj.username = updatedData.username;
            staffObj.email = updatedData.email;
            staffObj.role = 'Admin';
            staffObj.avatarUrl = updatedData.avatarUrl;
            
            staffObj.lastName = updatedData.lastName;
            staffObj.firstName = updatedData.firstName;
            staffObj.phoneNumber = updatedData.phoneNumber;
            staffObj.gender = updatedData.gender;
            staffObj.birthday = updatedData.birthday;
            staffObj.contractType = updatedData.contractType;
            staffObj.salary = updatedData.salary;
            staffObj.bankAccount = updatedData.bankAccount;
            staffObj.qualifications = updatedData.qualifications;
        }

        return staffObj;
    }
}
