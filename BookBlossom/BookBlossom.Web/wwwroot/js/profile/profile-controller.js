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

        // Initialize cropper state values for drag-and-drop and zoom scale tracking
        self.cropper = {
            scale: 1,
            x: 0,
            y: 0,
            isDragging: false,
            startX: 0,
            startY: 0
        };

        // Open custom avatar cropper fullscreen modal on button click
        $('#btn-change-avatar').on('click', function () {
            self.cropper.scale = 1;
            self.cropper.x = 0;
            self.cropper.y = 0;
            self.view.openCropperModal();
        });

        // Trigger file input upload when clicking placeholder card or retry action button
        $(document).on('click', '#cropper-upload-placeholder, #btn-reupload', function () {
            $('#avatar-file-input').click();
        });

        // Handle Close and Cancel button clicks for cropper modal
        $(document).on('click', '#btn-close-cropper, #btn-cancel-cropper', function () {
            self.view.closeCropperModal();
        });

        // Close cropper modal when clicking dim backdrop overlay
        $(document).on('click', '#avatar-cropper-overlay', function (e) {
            if (e.target.id === 'avatar-cropper-overlay') {
                self.view.closeCropperModal();
            }
        });

        // Load custom image file via FileReader API when selected
        $(document).on('change', '#avatar-file-input', function (e) {
            const files = e.target.files;
            if (files && files.length > 0) {
                const file = files[0];
                const reader = new FileReader();
                reader.onload = function (event) {
                    self.cropper.scale = 1;
                    self.cropper.x = 0;
                    self.cropper.y = 0;
                    self.view.loadCropperImage(event.target.result);
                };
                reader.readAsDataURL(file);
            }
        });

        // Adjust image scale via dynamic range slider input
        $(document).on('input change', '#zoom-range', function () {
            self.cropper.scale = parseFloat($(this).val()) || 1;
            self.view.updateCropperImageTransform(self.cropper.scale, self.cropper.x, self.cropper.y);
        });

        // Increment scale factor on Zoom In button click
        $(document).on('click', '#btn-zoom-in', function () {
            self.cropper.scale = Math.min(3, self.cropper.scale + 0.1);
            self.view.updateCropperImageTransform(self.cropper.scale, self.cropper.x, self.cropper.y);
        });

        // Decrement scale factor on Zoom Out button click
        $(document).on('click', '#btn-zoom-out', function () {
            self.cropper.scale = Math.max(0.1, self.cropper.scale - 0.1);
            self.view.updateCropperImageTransform(self.cropper.scale, self.cropper.x, self.cropper.y);
        });

        // Track drag movement coordinates via mouse dragging
        $(document).on('mousedown touchstart', '#cropper-image', function (e) {
            e.preventDefault();
            self.cropper.isDragging = true;
            
            const clientX = e.type === 'touchstart' ? e.originalEvent.touches[0].clientX : e.clientX;
            const clientY = e.type === 'touchstart' ? e.originalEvent.touches[0].clientY : e.clientY;
            
            self.cropper.startX = clientX - self.cropper.x;
            self.cropper.startY = clientY - self.cropper.y;
        });

        // Calculate and update translation variables on mouse/touch drag
        $(document).on('mousemove touchmove', function (e) {
            if (!self.cropper.isDragging) return;
            
            const clientX = e.type === 'touchmove' ? e.originalEvent.touches[0].clientX : e.clientX;
            const clientY = e.type === 'touchmove' ? e.originalEvent.touches[0].clientY : e.clientY;
            
            self.cropper.x = clientX - self.cropper.startX;
            self.cropper.y = clientY - self.cropper.startY;
            
            self.view.updateCropperImageTransform(self.cropper.scale, self.cropper.x, self.cropper.y);
        });

        // Stop tracking drag movements on release
        $(document).on('mouseup touchend', function () {
            self.cropper.isDragging = false;
        });

        // Render cropped canvas and apply final avatar changes to Model and View
        $(document).on('click', '#btn-save-cropper', function () {
            self.view.getCroppedImage(self.cropper.scale, self.cropper.x, self.cropper.y, function (croppedDataUrl) {
                // Persist new base64 image data in localstorage via model
                self.model.updateField('avatar', croppedDataUrl);
                
                // Re-render user view
                self.view.render(self.model.user);
                
                // Close cropping overlay
                self.view.closeCropperModal();
                
                // Trigger toast notification
                self.view.showToast("Avatar image updated successfully!");
            });
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
