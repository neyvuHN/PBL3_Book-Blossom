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

    loadUser() {
        let stored = localStorage.getItem(this.storageKey);
        if (!stored) {
            this.saveUser(this.defaultUser);
            return { ...this.defaultUser };
        }
        try {
            return JSON.parse(stored);
        } catch (e) {
            console.error("Error parsing user data, resetting to default", e);
            return { ...this.defaultUser };
        }
    }

    saveUser(userState = this.user) {
        this.user = userState;
        localStorage.setItem(this.storageKey, JSON.stringify(this.user));
    }

    updateField(field, value) {
        this.user[field] = value;
        this.saveUser();
    }

    updateMultipleFields(fieldsObj) {
        this.user = { ...this.user, ...fieldsObj };
        this.saveUser();
    }

    /**
     * Ensures user badges collection is valid.
     */
    validateBadges() {
        if (!this.user.badges || !Array.isArray(this.user.badges)) {
            this.user.badges = [...this.defaultUser.badges];
            this.saveUser();
        }
        if (!this.user.badgeEarnedDates || typeof this.user.badgeEarnedDates !== 'object') {
            this.user.badgeEarnedDates = { ...this.defaultUser.badgeEarnedDates };
            this.saveUser();
        }
    }
}

// Attach to window namespace for global access
window.ProfileModel = ProfileModel;
