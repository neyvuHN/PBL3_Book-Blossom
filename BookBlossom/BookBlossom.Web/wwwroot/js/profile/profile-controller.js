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

        // Show Premium plans modal overlay on clicking Upgrade button
        $('#btn-upgrade-pkg, #btn-upgrade-pkg-footer').on('click', function () {
            self.view.openSubscriptionModal(self.model.user.subscriptionPackage);
        });

        // Close Premium plans modal overlay
        $('#btn-close-subscription').on('click', function () {
            self.view.closeSubscriptionModal();
        });

        // Close premium plans modal on overlay background click
        $('#subscription-modal-overlay').on('click', function (e) {
            if (e.target.id === 'subscription-modal-overlay') {
                self.view.closeSubscriptionModal();
            }
        });

        // Select Premium plan upgrade option (standard Free vs VNPay basic/pro checkout)
        $(document).on('click', '.btn-select-plan', function () {
            const planName = $(this).attr('data-plan');
            const price = parseInt($(this).attr('data-price')) || 0;
            const currentPkg = self.model.user.subscriptionPackage;

            if (planName === currentPkg) return;

            // Close the plans modal
            self.view.closeSubscriptionModal();

            if (planName === "Free") {
                // Free package does not require VNPay simulation, downgrade instantly!
                let threadLimit = 3;
                let undoLimit = 2;
                let badges = ["True Bookworm"];

                // Reset limits if exceeded
                if (self.model.user.currentMonthThreadCount > 3) {
                    self.model.user.currentMonthThreadCount = 2;
                }
                if (self.model.user.dailyUndoCount > 2) {
                    self.model.user.dailyUndoCount = 1;
                }

                self.model.updateMultipleFields({
                    subscriptionPackage: "Free",
                    maxMonthlyThreadLimit: threadLimit,
                    maxDailyUndoLimit: undoLimit,
                    badges: badges
                });

                self.view.render(self.model.user);
                self.view.showToast("Downgraded to Free package successfully!");
            } else {
                // Basic or Pro package checkout flow - VNPay Redirect Simulation!
                self.view.showVNPayLoading();

                // 1. Simulate VNPay loading screen redirect (2.5 seconds)
                setTimeout(() => {
                    self.view.hideVNPayLoading();
                    self.view.showVNPayReturn();

                    // 2. Simulate payment status verification screen (2 seconds)
                    setTimeout(() => {
                        self.view.hideVNPayReturn();

                        // Set correct limits & badges based on plan choice
                        let threadLimit = 20;
                        let undoLimit = 5;
                        let badges = [
                            "Review Champion - Critic", 
                            "Knowledge Ambassador", 
                            "Blind Date Adventurer - Seeker", 
                            "True Bookworm"
                        ];

                        if (planName === "Pro") {
                            threadLimit = 9999;
                            undoLimit = 9999;
                            badges = [
                                "Review Champion - Sage", 
                                "Knowledge Ambassador", 
                                "Blind Date Adventurer - Destiny", 
                                "True Bookworm",
                                "Exemplary User",
                                "Moderator Assistant"
                            ];
                        }

                        // Save updated state back to the model
                        self.model.updateMultipleFields({
                            subscriptionPackage: planName,
                            maxMonthlyThreadLimit: threadLimit,
                            maxDailyUndoLimit: undoLimit,
                            badges: badges
                        });

                        // Re-render display view
                        self.view.render(self.model.user);

                        // Trigger payment success overlay
                        self.view.showPaymentSuccess(planName, price);
                        self.view.showToast(`Upgraded to ${planName} package successfully!`);
                    }, 2000);
                }, 2500);
            }
        });

        // Continue shopping / Enjoy privileges button actions in payment success overlay
        $(document).on('click', '#payment-success-overlay .btn-continue-shopping, #payment-success-overlay .btn-view-order', function () {
            self.view.hidePaymentSuccess();
        });

        // Retry upgrade action in failed overlay
        $(document).on('click', '#payment-failed-overlay .btn-retry-payment', function () {
            self.view.hidePaymentFailed();
            self.view.openSubscriptionModal(self.model.user.subscriptionPackage);
        });

        // Close action in failed overlay
        $(document).on('click', '#payment-failed-overlay .btn-cancel-order', function () {
            self.view.hidePaymentFailed();
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
