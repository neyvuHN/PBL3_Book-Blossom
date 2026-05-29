/**
 * BOOKBLOSSOM PROFILE CONTROLLER (MVC PATTERN)
 * Orchestrates event handlers, click listeners, state mutations,
 * and ties Model and View layers together cleanly.
 * 
 * UPDATED AREA: Created as a separate Controller file to decouple execution logic.
 */
class ProfileController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
        this.presetAvatars = [
            "/images/Avatar/avatar1.jpg",
            "/images/Avatar/avatar2.jpg",
            "/images/Avatar/avatar3.jpg",
            "/images/Avatar/avatar4.jpg"
        ];
    }

    /**
     * Initializes and binds all event listeners and triggers initial render
     */
    init() {
        // Initial presentation load
        this.view.render(this.model.user);
        this.bindEvents();
    }

    /**
     * Binds jQuery selectors to user interaction events
     */
    bindEvents() {
        const self = this;

        // Toggle Edit Form visibility
        $('#btn-edit-profile-toggle').on('click', function () {
            $('#edit-profile-form').slideToggle(300);
            $('html, body').animate({
                scrollTop: $("#edit-profile-form").offset().top - 120
            }, 500);
        });

        // Cancel Edit Form
        $('#btn-edit-profile-cancel').on('click', function () {
            $('#edit-profile-form').slideUp(300);
        });

        // Submit form details
        $('#edit-profile-form').on('submit', function (e) {
            e.preventDefault();
            
            const updatedData = {
                fullName: $('#input-fullname').val().trim() || self.model.user.fullName,
                username: $('#input-username').val().trim().toLowerCase() || self.model.user.username,
                bio: $('#input-bio').val().trim() || self.model.user.bio,
                phoneNumber: $('#input-phone').val().trim() || self.model.user.phoneNumber,
                email: $('#input-email').val().trim() || self.model.user.email,
                gender: $('#input-gender').val(),
                birthdate: $('#input-birthdate').val()
            };

            // Save details through model
            self.model.updateMultipleFields(updatedData);

            // Re-render display view
            self.view.render(self.model.user);
            
            $('#edit-profile-form').slideUp(300);
            self.view.showToast("Profile details updated successfully!");
        });

        // Cycling through local premium preset avatars
        $('#btn-change-avatar').on('click', function () {
            let currentIdx = self.presetAvatars.indexOf(self.model.user.avatar);
            let nextIdx = (currentIdx + 1) % self.presetAvatars.length;
            
            self.model.updateField('avatar', self.presetAvatars[nextIdx]);
            self.view.render(self.model.user);
            
            self.view.showToast("Avatar image updated successfully!");
        });

        // Interactive Subscription Tier simulation
        $('#btn-upgrade-pkg, #btn-upgrade-pkg-footer').on('click', function () {
            let currentPkg = self.model.user.subscriptionPackage;
            let nextPkg = "Free";
            let threadLimit = 3;
            let undoLimit = 2;
            let badges = ["True Bookworm"];

            if (currentPkg === "Free") {
                nextPkg = "Basic";
                threadLimit = 20;
                undoLimit = 5;
                badges = [
                    "Review Champion - Critic", 
                    "Knowledge Ambassador", 
                    "Blind Date Adventurer - Seeker", 
                    "True Bookworm"
                ];
            } else if (currentPkg === "Basic") {
                nextPkg = "Pro";
                threadLimit = 100;
                undoLimit = 15;
                badges = [
                    "Review Champion - Sage", 
                    "Knowledge Ambassador", 
                    "Blind Date Adventurer - Destiny", 
                    "True Bookworm",
                    "Exemplary User",
                    "Moderator Assistant"
                ];
            } else {
                // Return to Free
                // Normalize limits if needed
                if (self.model.user.currentMonthThreadCount > 3) {
                    self.model.user.currentMonthThreadCount = 2;
                }
                if (self.model.user.dailyUndoCount > 2) {
                    self.model.user.dailyUndoCount = 1;
                }
            }

            // Perform batch state update on the model
            self.model.updateMultipleFields({
                subscriptionPackage: nextPkg,
                maxMonthlyThreadLimit: threadLimit,
                maxDailyUndoLimit: undoLimit,
                badges: badges
            });

            // Refresh UI presentation
            self.view.render(self.model.user);
            
            self.view.showToast(`Subscription packages updated to ${nextPkg} successfully!`);
        });
    }
}

// Instantiate and initialize when DOM is ready
$(document).ready(function () {
    const model = new window.ProfileModel();
    const view = new window.ProfileView();
    const controller = new ProfileController(model, view);
    controller.init();
});
