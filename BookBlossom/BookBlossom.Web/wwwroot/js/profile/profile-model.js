/**
 * BOOKBLOSSOM PROFILE MODEL (MVC PATTERN)
 * Handles state, data queries, and API calls for the Profile module.
 * 
 * UPDATED AREA: Created as a separate Model file to decouple data logic.
 */
class ProfileModel {
    constructor() {
        this.storageKey = 'BookBlossomUser';
        this.defaultUser = {
            fullName: "Jane Doe",
            username: "janedoe_bookworm",
            bio: "Reading is a discount ticket to everywhere! Passionate about psychological thrillers, magic realism, and mystery blind dates.",
            phoneNumber: "0987654321",
            email: "janedoe@example.com",
            gender: "Female",
            birthdate: "2000-05-15",
            avatar: "/images/Avatar/avatar1.jpg",
            membershipTier: "Gold", // Copper, Silver, Gold, Diamond
            totalSpending: 4250000,
            nextTierThreshold: 5000000,
            reputationScore: 110,
            maxReputationScore: 150,
            currentOrderStreak: 2,
            badges: [
                "Review Champion - Critic",
                "Knowledge Ambassador",
                "Blind Date Adventurer - Destiny",
                "True Bookworm",
                "Exemplary User",
                "Moderator Assistant"
            ],
            badgeEarnedDates: {
                "Review Champion - Explorer": "2025-02-14",
                "Review Champion - Critic": "2025-04-18",
                "Review Champion - Sage": "2026-05-12",
                "Knowledge Ambassador": "2025-03-05",
                "Blind Date Adventurer - Curious": "2025-01-20",
                "Blind Date Adventurer - Seeker": "2025-03-22",
                "Blind Date Adventurer - Destiny": "2025-11-15",
                "True Bookworm": "2025-02-01",
                "Exemplary User": "2025-08-10",
                "Moderator Assistant": "2025-12-05"
            },
            subscriptionPackage: "Basic", // Free, Basic, Pro
            currentMonthThreadCount: 8,
            maxMonthlyThreadLimit: 20,
            dailyUndoCount: 3,
            maxDailyUndoLimit: 5
        };

        this.user = this.loadUser();
        this.validateBadges();

        // Cache for packages loaded from API
        this._packagesCache = null;
    }

    // loadUser is not needed since SSR loads the initial data.
    // However, keeping an empty user object or basic state helps.
    loadUser() {
        return {};
    }

    // Replace localStorage with real API call
    async updateProfileData(formData) {
        try {
            return await window.apiClient.apiUpload('/api/profile', formData);
        } catch (error) {
            console.error('Failed to update profile', error);
            throw error;
        }
    }

    updateField(field, value) {
        // No longer storing in localStorage. Updates are handled via API.
    }

    updateMultipleFields(fieldsObj) {
        // No longer storing in localStorage. Updates are handled via API.
    }

    validateBadges() {
        // Not used anymore.
    }

    /**
     * Fetches current user's reputation score, rank, and privileges.
     * Route: GET /api/Reputation/my-reputation
     * @returns {Promise<Object>} Reputation data
     */
    async fetchMyReputation() {
        try {
            return await window.apiClient.apiGet('/api/Reputation/my-reputation');
        } catch (error) {
            console.error('Failed to fetch reputation:', error);
            throw error;
        }
    }

    /**
     * Fetches the current user's earned badges.
     * Route: GET /api/Badge/my-collection
     * @returns {Promise<Array>} List of earned badges
     */
    async fetchMyBadges() {
        try {
            return await window.apiClient.apiGet('/api/Badge/my-collection');
        } catch (error) {
            console.error('Failed to fetch user badges:', error);
            throw error;
        }
    }

    // ─── Service Package API Methods ────────────────────────────────────

    /**
     * Fetches all available service packages from the backend.
     * Route: GET /api/ServicePackage/all
     * @returns {Promise<Array>} Array of ServicePackage objects
     */
    async fetchAllPackages() {
        try {
            const packages = await window.apiClient.apiGet('/api/ServicePackage/all');
            this._packagesCache = packages;
            return packages;
        } catch (error) {
            console.error('Failed to fetch service packages:', error);
            throw error;
        }
    }

    /**
     * Fetches the current user's active subscription/service.
     * Route: GET /api/ServicePackage/my-service
     * @returns {Promise<Object|null>} Current service info or null
     */
    async fetchMyService() {
        try {
            const service = await window.apiClient.apiGet('/api/ServicePackage/my-service');
            return service;
        } catch (error) {
            // 404 means user has no subscription yet → treat as Free
            if (error.message && error.message.includes('Bạn chưa đăng ký')) {
                return null;
            }
            console.error('Failed to fetch current service:', error);
            throw error;
        }
    }

    /**
     * Subscribes the current user to a specific package.
     * Route: POST /api/ServicePackage/subscribe/{packageId}?paymentMethod={pm}
     * @param {number} packageId - The ID of the package to subscribe to
     * @param {number} paymentMethod - 0=COD, 1=VNPay (default 1)
     * @returns {Promise<string>} Success message
     */
    async subscribeToPackage(packageId, paymentMethod = 1) {
        try {
            const result = await window.apiClient.apiPost(
                `/api/ServicePackage/subscribe/${packageId}?paymentMethod=${paymentMethod}`
            );
            return result;
        } catch (error) {
            console.error('Failed to subscribe to package:', error);
            throw error;
        }
    }
}

// Attach to window namespace for global access
window.ProfileModel = ProfileModel;
