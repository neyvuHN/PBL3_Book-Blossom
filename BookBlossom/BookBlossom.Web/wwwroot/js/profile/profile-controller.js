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
        // Cached packages from API for subscription modal
        this._packages = [];
        this._currentPlanName = null;
    }

    /**
     * Initializes and binds all event listeners and triggers initial render
     */
    async init() {
        // Initial presentation load is handled by server-side rendering (SSR)
        this.bindEvents();

        // Load subscription data from API in background
        await this.loadSubscriptionData();

        // Auto-open upgrade modal if redirected with ?openUpgrade=true
        const urlParams = new URLSearchParams(window.location.search);
        if (urlParams.get('openUpgrade') === 'true') {
            this.openSubscriptionModalWithData();
            // Clean up URL without reload
            window.history.replaceState({}, '', window.location.pathname);
        }
    }

    /**
     * Loads all packages + current user's service from API (background, non-blocking)
     */
    async loadSubscriptionData() {
        try {
            // Fetch all available packages
            const packages = await this.model.fetchAllPackages();
            this._packages = packages || [];

            // Try to fetch current user's service (may fail for new users)
            try {
                const myService = await this.model.fetchMyService();
                if (myService && myService.packageName) {
                    this._currentPlanName = myService.packageName;
                } else {
                    this._currentPlanName = 'Free';
                }
            } catch (e) {
                // User has no subscription → Free
                this._currentPlanName = 'Free';
            }

            // Update the modal plan cards with real API data
            if (this._packages.length > 0) {
                this.view.updateSubscriptionPlans(this._packages);
            }
        } catch (error) {
            console.warn('Failed to load subscription data (non-critical):', error);
        }
    }

    /**
     * Opens the subscription modal with API-loaded data
     */
    openSubscriptionModalWithData() {
        // Use the SSR-provided current plan as fallback (from the badge text in the page)
        const ssrPlan = $('#display-sub-badge').text().replace(' Package', '').trim();
        const currentPlan = this._currentPlanName || ssrPlan || 'Free';
        this.view.openSubscriptionModal(currentPlan);
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
        $('#edit-profile-form').on('submit', async function (e) {
            e.preventDefault();
            
            const fullName = $('#input-fullname').val().trim();
            const lastSpaceIndex = fullName.lastIndexOf(' ');
            const firstName = lastSpaceIndex === -1 ? fullName : fullName.substring(lastSpaceIndex + 1);
            const lastName = lastSpaceIndex === -1 ? "" : fullName.substring(0, lastSpaceIndex);

            const formData = new FormData();
            formData.append('firstName', firstName);
            formData.append('lastName', lastName);
            formData.append('userName', $('#input-username').val().trim().toLowerCase());
            formData.append('gender', $('#input-gender').val());
            formData.append('birthdate', $('#input-birthdate').val());
            formData.append('bio', $('#input-bio').val().trim());

            try {
                // Show loading state
                const $btn = $('#btn-edit-profile-save');
                const originalText = $btn.text();
                $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Saving...');

                await self.model.updateProfileData(formData);
                self.view.showToast("Profile details updated successfully!");
                
                $('#edit-profile-form').slideUp(300);

                // Reload the page to reflect changes properly via SSR
                setTimeout(() => window.location.reload(), 1500);
            } catch (error) {
                // Toast is handled by apiClient
                $('#btn-edit-profile-save').prop('disabled', false).text('Save Changes');
            }
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
            const $btn = $(this);
            $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Uploading...');

            self.view.getCroppedImage(self.cropper.scale, self.cropper.x, self.cropper.y, async function (croppedDataUrl) {
                // Convert base64 DataURL to Blob
                const fetchRes = await fetch(croppedDataUrl);
                const blob = await fetchRes.blob();
                
                const formData = new FormData();
                formData.append('avatarImage', blob, 'avatar.png');

                try {
                    await self.model.updateProfileData(formData);
                    self.view.showToast("Avatar image updated successfully!");
                    self.view.closeCropperModal();
                    
                    // Reload to reflect new avatar generated from server
                    setTimeout(() => window.location.reload(), 1500);
                } catch (error) {
                    $btn.prop('disabled', false).text('Apply Changes');
                }
            });
        });

        // Show Premium plans modal overlay on clicking Upgrade button
        $('#btn-upgrade-pkg, #btn-upgrade-pkg-footer').on('click', function () {
            self.openSubscriptionModalWithData();
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

        // ═══════════════════════════════════════════════════════════════════
        // SELECT PLAN: Real API integration (replaces mock VNPay simulation)
        // Route: POST /api/ServicePackage/subscribe/{packageId}
        // ═══════════════════════════════════════════════════════════════════
        $(document).on('click', '.btn-select-plan', async function () {
            const $btn = $(this);
            const planName = $btn.attr('data-plan');
            const packageId = parseInt($btn.attr('data-package-id')) || 0;
            const price = parseInt($btn.attr('data-price')) || 0;

            // Don't proceed if it's the current plan
            const ssrPlan = $('#display-sub-badge').text().replace(' Package', '').trim();
            const currentPkg = self._currentPlanName || ssrPlan || 'Free';
            if (planName === currentPkg) return;

            // Check if user is authenticated
            const token = localStorage.getItem('accessToken');
            if (!token) {
                self.view.closeSubscriptionModal();
                window.apiClient.showToast('Vui lòng đăng nhập để nâng cấp gói dịch vụ.', 'error', 'Yêu cầu đăng nhập');
                setTimeout(() => {
                    window.location.href = '/Auth/Login';
                }, 1500);
                return;
            }

            // Validate packageId
            if (packageId <= 0) {
                window.apiClient.showToast('Không thể xác định gói dịch vụ. Vui lòng tải lại trang.', 'error');
                return;
            }

            // Close the plans modal
            self.view.closeSubscriptionModal();

            // Disable button to prevent double-click
            $btn.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Processing...');

            if (planName === "Free") {
                // ─── Downgrade to Free ───────────────────────────────────
                try {
                    await self.model.subscribeToPackage(packageId, 1);
                    self.view.showToast("Đã chuyển về gói Free thành công!");
                    // Reload page to reflect SSR changes
                    setTimeout(() => window.location.reload(), 1500);
                } catch (error) {
                    $btn.prop('disabled', false).text('Activate Standard');
                    window.apiClient.showToast(error.message || 'Chuyển gói thất bại. Vui lòng thử lại.', 'error');
                }
            } else {
                // ─── Upgrade to Basic/Pro (with UX animation) ────────────
                // Show VNPay loading animation for UX polish
                self.view.showVNPayLoading();

                try {
                    // Call real API to subscribe
                    await self.model.subscribeToPackage(packageId, 1);

                    // Transition: loading → verifying → success
                    setTimeout(() => {
                        self.view.hideVNPayLoading();
                        self.view.showVNPayReturn();

                        setTimeout(() => {
                            self.view.hideVNPayReturn();

                            // Show payment success overlay
                            self.view.showPaymentSuccess(planName, price);
                            self.view.showToast(`Nâng cấp lên gói ${planName} thành công!`);
                        }, 2000);
                    }, 2000);
                } catch (error) {
                    // Hide loading screens on error
                    self.view.hideVNPayLoading();
                    self.view.hideVNPayReturn();

                    // Show payment failed overlay
                    self.view.showPaymentFailed(planName);
                    $btn.prop('disabled', false).text('Upgrade Now');
                }
            }
        });

        // Continue / Enjoy Privileges button in payment success overlay → reload page
        $(document).on('click', '#payment-success-overlay .btn-continue-shopping, #payment-success-overlay .btn-view-order', function () {
            self.view.hidePaymentSuccess();
            // Reload page to get updated SSR data (new package, new limits)
            window.location.reload();
        });

        // Retry upgrade action in failed overlay
        $(document).on('click', '#payment-failed-overlay .btn-retry-payment', function () {
            self.view.hidePaymentFailed();
            self.openSubscriptionModalWithData();
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
