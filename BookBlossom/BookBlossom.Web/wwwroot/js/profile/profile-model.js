/**
 * BOOKBLOSSOM PROFILE MODEL (MVC PATTERN)
 * Handles state, LocalStorage persistence, data queries, and badge migrations.
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
}

// Attach to window namespace for global access
window.ProfileModel = ProfileModel;
