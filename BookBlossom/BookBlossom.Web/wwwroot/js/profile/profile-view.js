/**
 * BOOKBLOSSOM PROFILE VIEW (MVC PATTERN)
 * Handles rendering the user state, layout adjustments, limit charts,
 * custom SVG medal elements, and animations.
 * 
 * UPDATED AREA: Created as a separate View file to decouple presentation logic.
 */
class ProfileView {
    constructor() {
        // Preset avatars to match controller preset
        this.presetAvatars = [
            "/images/Avatar/avatar1.jpg",
            "/images/Avatar/avatar2.jpg",
            "/images/Avatar/avatar3.jpg",
            "/images/Avatar/avatar4.jpg"
        ];
    }

    /**
     * Renders the entire profile UI using user model state
     */
    render(user) {
        // Public details
        $('#display-fullname').text(user.fullName);
        $('#display-username').text('@' + user.username);
        $('#display-bio').text('"' + user.bio + '"');
        $('#display-avatar-img').attr('src', user.avatar);

        // Instantly synchronize top-right Navbar elements
        $('.user-dropdown img.avatar').attr('src', user.avatar);
        $('.user-dropdown span.username').text(user.fullName);

        // Contact info
        $('#display-phone').text(user.phoneNumber);
        $('#display-email').text(user.email);
        $('#display-gender').text(user.gender);

        // Format birthdate nicely
        if (user.birthdate) {
            const dateObj = new Date(user.birthdate);
            const options = { year: 'numeric', month: 'long', day: 'numeric' };
            $('#display-birthdate').text(dateObj.toLocaleDateString('en-US', options));
        }

        // Subscription Tier Badge
        $('#display-sub-badge')
            .text(user.subscriptionPackage + ' Package')
            .removeClass('sub-tier-Free sub-tier-Basic sub-tier-Pro')
            .addClass('sub-tier-' + user.subscriptionPackage);

        // Membership Tier
        $('#display-tier-text').text(user.membershipTier);
        $('#display-tier-badge')
            .removeClass('tier-Bronze tier-Silver tier-Gold tier-Diamond')
            .addClass('tier-' + user.membershipTier);

        // Spending metrics
        const spendingVal = Number(user.totalSpending) || 0;
        const thresholdVal = Number(user.nextTierThreshold) || 5000000;
        $('#display-spending-text').text(
            new Intl.NumberFormat('vi-VN').format(spendingVal) + ' / ' +
            new Intl.NumberFormat('vi-VN').format(thresholdVal) + ' VND'
        );
        const spendPercent = Math.min(100, (spendingVal / thresholdVal) * 100);
        $('#display-spending-progress').css('width', spendPercent + '%');

        const diffSpending = Math.max(0, thresholdVal - spendingVal);
        if (diffSpending > 0) {
            $('#display-spending-hint').html(
                `Spend <strong>${new Intl.NumberFormat('vi-VN').format(diffSpending)} VND</strong> more to reach Diamond Tier!`
            );
        } else {
            $('#display-spending-hint').html('🎉 You have reached the maximum Tier spending milestone!');
        }

        // Reputation Score
        const repScore = Math.min(150, Math.max(0, Number(user.reputationScore) || 0));
        $('#display-rep-score').text(repScore);
        const repPercent = (repScore / 150) * 100;
        $('#reputation-svg-fill').attr('stroke-dasharray', `${repPercent}, 100`);

        // Recalculate privileges warnings (English output)
        const warningBox = $('#reputation-warning-box');
        const warningMsg = $('#reputation-warning-message');
        warningBox.removeClass('warning-safe warning-warning warning-danger');

        if (repScore < 60) {
            warningBox.addClass('warning-danger').find('i').attr('class', 'fas fa-exclamation-triangle');
            warningMsg.html('Warning: Reputation score too low (&lt; 60). Commenting &amp; Thread posting is BANNED, and Cash on Delivery (COD) method is DISABLED.');
        } else if (repScore < 80) {
            warningBox.addClass('warning-warning').find('i').attr('class', 'fas fa-exclamation-circle');
            warningMsg.html('Warning: Reputation score low (&lt; 80). Thread posting &amp; Commenting are BANNED. Keep score above 80 to restore community privileges.');
        } else {
            warningBox.addClass('warning-safe').find('i').attr('class', 'fas fa-check-circle');
            warningMsg.html('Your reputation score is stellar! You have full commenting, posting privileges and COD checkout active.');
        }

        // Order Streak
        const streak = Math.min(3, Math.max(0, Number(user.currentOrderStreak) || 0));
        const streakPercent = streak * 50;
        $('#display-streak-line').css('width', streakPercent + '%');

        // Update Step dots
        $('.streak-step').removeClass('completed active');
        if (streak >= 1) $('#streak-step-1').addClass('completed'); else $('#streak-step-1').addClass('active');
        if (streak >= 2) $('#streak-step-2').addClass('completed'); else if (streak === 1) $('#streak-step-2').addClass('active');
        if (streak >= 3) {
            $('#streak-step-3').addClass('completed');
            $('#display-streak-status').html('🎉 Streak Goal Achieved! Bonus claimed.');
        } else {
            if (streak === 2) $('#streak-step-3').addClass('active');
            $('#display-streak-status').html(`Current Streak: <strong>${streak}/3 successful orders</strong>`);
        }

        // Subscription Limit counters & progress bars
        if (user.subscriptionPackage === "Pro") {
            $('#display-thread-counter').text(`${user.currentMonthThreadCount} / Unlimited`);
            $('#display-thread-progress').css('width', '100%');
            $('#display-undo-counter').text(`${user.dailyUndoCount} / Unlimited`);
            $('#display-undo-progress').css('width', '100%');
        } else {
            $('#display-thread-counter').text(`${user.currentMonthThreadCount} / ${user.maxMonthlyThreadLimit} posts`);
            const threadPercent = Math.min(100, (user.currentMonthThreadCount / user.maxMonthlyThreadLimit) * 100);
            $('#display-thread-progress').css('width', threadPercent + '%');

            $('#display-undo-counter').text(`${user.dailyUndoCount} / ${user.maxDailyUndoLimit} undos`);
            const undoPercent = Math.min(100, (user.dailyUndoCount / user.maxDailyUndoLimit) * 100);
            $('#display-undo-progress').css('width', undoPercent + '%');
        }

        // Badge Cabinet Rendering
        const cabinet = $('#display-badges-cabinet');
        if (cabinet.length) {
            this.renderBadgesGrid(cabinet, user.badges, 4);
        }

        // Update modal body
        const modalBody = $('#allBadgesModal .modal-body .d-flex');
        if (modalBody.length) {
            this.renderBadgesModal(modalBody, user.badges, user.badgeEarnedDates);
        }

        // Mystic Aura Update (Blind Date Destiny)
        if (user.badges.includes("Blind Date Adventurer - Destiny")) {
            $('.profile-avatar-wrapper').addClass("mystic-aura");
        } else {
            $('.profile-avatar-wrapper').removeClass("mystic-aura");
        }

        // Sync values inside inputs
        $('#input-fullname').val(user.fullName);
        $('#input-username').val(user.username);
        $('#input-bio').val(user.bio);
        $('#input-phone').val(user.phoneNumber);
        $('#input-email').val(user.email);
        $('#input-gender').val(user.gender);
        $('#input-birthdate').val(user.birthdate);
    }

    /**
     * Renders badge medals to cabinet grid layout (including locked ones)
     */
    renderBadgesGrid(cabinetElement, allBadges, earnedBadgeNames, maxDisplay) {
        cabinetElement.empty();
        let displayCount = 0;

        // Sort allBadges: earned badges first
        const sortedBadges = [...allBadges].sort((a, b) => {
            const aName = a.badgeName || a.BadgeName;
            const bName = b.badgeName || b.BadgeName;
            const aEarned = earnedBadgeNames.includes(aName);
            const bEarned = earnedBadgeNames.includes(bName);
            if (aEarned && !bEarned) return -1;
            if (!aEarned && bEarned) return 1;
            return 0;
        });

        sortedBadges.forEach(badge => {
            if (displayCount >= maxDisplay) return;
            const name = badge.badgeName || badge.BadgeName;
            const isEarned = earnedBadgeNames.includes(name);
            cabinetElement.append(this.getMedalHtml(name, 'small', isEarned));
            displayCount++;
        });

        // Always append View All button
        cabinetElement.append(`
            <div class="badge-medal-view-all" data-bs-toggle="modal" data-bs-target="#allBadgesModal">
                <div class="view-all-circle">
                    <i class="fas fa-eye" style="font-size: 1.15rem; margin-bottom: 0;"></i>
                </div>
                <span class="view-all-label">View All</span>
            </div>
        `);
    }

    /**
     * Renders badge medals list in the modal view (including locked ones)
     */
    renderBadgesModal(modalBodyElement, allBadges, earnedBadgeNames, earnedDates) {
        modalBodyElement.empty();

        // Sort allBadges: earned badges first
        const sortedBadges = [...allBadges].sort((a, b) => {
            const aName = a.badgeName || a.BadgeName;
            const bName = b.badgeName || b.BadgeName;
            const aEarned = earnedBadgeNames.includes(aName);
            const bEarned = earnedBadgeNames.includes(bName);
            if (aEarned && !bEarned) return -1;
            if (!aEarned && bEarned) return 1;
            return 0;
        });

        sortedBadges.forEach(badge => {
            const name = badge.badgeName || badge.BadgeName;
            const isEarned = earnedBadgeNames.includes(name);
            const dateVal = (earnedDates && earnedDates[name]) ? earnedDates[name] : null;
            modalBodyElement.append(this.getMedalHtml(name, 'large', isEarned, dateVal));
        });
    }

    /**
     * Formulates complete HTML structure of a single 3D physical-like medal using custom SVG
     */
    getMedalHtml(badgeName, sizeClass, isEarned = true, earnedDate = null) {
        let scallopGrad = "goldGrad";
        let innerGrad = "goldInner";
        let iconClass = "fa-medal";
        let titleText = badgeName;
        let desc = "A well-deserved badge of honor.";
        let themeClass = "theme-gold";
        let levelClass = "";

        if (badgeName === "Review Champion - Explorer") {
            scallopGrad = "bronzeGrad";
            innerGrad = "bronzeInner";
            iconClass = "fa-star";
            desc = "Write 5 book reviews with photos.";
            themeClass = "theme-bronze";
            levelClass = "medal-lvl1";
        } else if (badgeName === "Review Champion - Critic") {
            scallopGrad = "silverGrad";
            innerGrad = "silverInner";
            iconClass = "fa-star";
            desc = "Write 20 high-quality reviews (with >= 5 likes).";
            themeClass = "theme-silver";
            levelClass = "medal-lvl2";
        } else if (badgeName === "Review Champion - Sage") {
            scallopGrad = "goldGrad";
            innerGrad = "goldInner";
            iconClass = "fa-star";
            desc = "Write 50 high-quality reviews and rank in the Top 'Reviews of the Month'.";
            themeClass = "theme-gold";
            levelClass = "medal-lvl3";
        } else if (badgeName === "Knowledge Ambassador") {
            scallopGrad = "blueGrad";
            innerGrad = "blueInner";
            iconClass = "fa-share-nodes";
            desc = "Actively share book links or posts to Facebook or Instagram from Web.";
            themeClass = "theme-blue";
        } else if (badgeName === "Blind Date Adventurer - Curious") {
            scallopGrad = "purpleGrad";
            innerGrad = "purpleInner";
            iconClass = "fa-mask";
            desc = "Purchase 3 Blind Date book orders.";
            themeClass = "theme-purple";
            levelClass = "medal-lvl1";
        } else if (badgeName === "Blind Date Adventurer - Seeker") {
            scallopGrad = "purpleGrad";
            innerGrad = "purpleInner";
            iconClass = "fa-mask";
            desc = "Purchase 10 Blind Date book orders.";
            themeClass = "theme-purple";
            levelClass = "medal-lvl2";
        } else if (badgeName === "Blind Date Adventurer - Destiny") {
            scallopGrad = "purpleGrad";
            innerGrad = "purpleInner";
            iconClass = "fa-mask";
            desc = "Purchase 25 Blind Date book orders to unlock a mysterious purple avatar glow.";
            themeClass = "theme-purple theme-destiny";
            levelClass = "medal-lvl3";
        } else if (badgeName === "True Bookworm") {
            scallopGrad = "greenGrad";
            innerGrad = "greenInner";
            iconClass = "fa-book-open";
            desc = "Purchase books across 5 diverse genres (e.g. Psychology, Novel, Horror).";
            themeClass = "theme-green";
        } else if (badgeName === "Exemplary User") {
            scallopGrad = "tealGrad";
            innerGrad = "tealInner";
            iconClass = "fa-shield-halved";
            desc = "Maintain the maximum reputation score of 150 points for 3 consecutive months.";
            themeClass = "theme-teal";
        } else if (badgeName === "Moderator Assistant") {
            scallopGrad = "crimsonGrad";
            innerGrad = "crimsonInner";
            iconClass = "fa-hands-helping";
            desc = "Submit more than 10 accurate violation reports to help keep the community clean.";
            themeClass = "theme-crimson";
        }

        const lockedStyle = isEarned ? "" : "filter: grayscale(100%) opacity(0.45);";
        const lockedTitle = isEarned ? `${titleText}: ${desc}` : `🔒 ${titleText} (Locked)`;
        const finalIconClass = isEarned ? iconClass : "fa-lock";

        // Return modular HTML: Grid cabinet vs detailed Modal item
        if (sizeClass === 'small') {
            return `
                <div class="badge-medal ${themeClass} ${levelClass}" title="${lockedTitle}" style="${lockedStyle}">
                    <div class="medal-container">
                        <svg class="medal-svg" viewBox="0 0 100 100">
                            <!-- Left Ribbon Tail -->
                            <path class="ribbon-tail-left" d="M 36 50 L 15 92 L 32 83 L 45 92 Z" fill="url(#ribbonLeftGrad)" />
                            <!-- Right Ribbon Tail -->
                            <path class="ribbon-tail-right" d="M 64 50 L 85 92 L 68 83 L 55 92 Z" fill="url(#ribbonRightGrad)" />
                            <!-- 24-point Scallop star outer ring -->
                            <polygon class="medal-scallop-base" fill="url(#${scallopGrad})" points="
                                50,5 53.5,10 58.7,6.3 60.9,12.1 67.2,10.2 68.1,16.7 75,16.4 74.4,22.9 81.3,24.1 79.2,30.3 85.7,32.7 82.2,38.4 87.7,41.9 83,47.1 87.2,51.5 81.5,55.9 84.4,61.4 78,64.8 79.5,70.9 72.8,73.1 73.1,79.5 66.2,80.5 65.1,86.9 58.5,86.4 56.4,92.5 50,90.5 43.6,92.5 41.5,86.4 34.9,86.9 33.8,80.5 26.9,79.5 27.2,73.1 20.5,70.9 22,64.8 15.6,61.4 18.5,55.9 12.8,51.5 17,47.1 12.3,41.9 17.8,38.4 14.3,32.7 20.8,30.3 18.7,24.1 25.6,22.9 25,16.4 31.9,16.7 32.8,10.2 39.1,12.1 41.3,6.3 46.5,10
                            " />
                            <!-- Shiny inner medallion circle -->
                            <circle class="medal-center" cx="50" cy="50" r="31.5" fill="url(#${innerGrad})" />
                            <!-- Circular inner ring highlight -->
                            <circle cx="50" cy="50" r="28" fill="none" stroke="#FFFFFF" stroke-opacity="0.4" stroke-width="0.75" />
                            <!-- Glossy reflections -->
                            <path class="medal-gloss" d="M 23 35 A 31.5 31.5 0 0 1 77 35 A 31.5 28 0 0 0 23 35 Z" fill="rgba(255,255,255,0.18)" />
                        </svg>
                        <div class="medal-icon-container">
                            <i class="fas ${finalIconClass}"></i>
                        </div>
                    </div>
                    <span class="medal-title">${titleText}</span>
                </div>
            `;
        } else {
            // Large list item in modal
            const lockedRowStyle = isEarned ? "" : "filter: grayscale(100%) opacity(0.5);";
            let dateHtml = '';
            if (isEarned) {
                let formattedDate = '';
                if (earnedDate) {
                    const dateObj = new Date(earnedDate);
                    const options = { year: 'numeric', month: 'long', day: 'numeric' };
                    formattedDate = dateObj.toLocaleDateString('en-US', options);
                } else {
                    formattedDate = new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
                }
                dateHtml = `<span class="badge-earned-date"><i class="far fa-calendar-alt me-1"></i> Earned on: ${formattedDate}</span>`;
            } else {
                dateHtml = `<span class="badge-earned-date" style="color:#b0b0b0;"><i class="fas fa-lock me-1"></i> Not yet earned</span>`;
            }

            return `
                <div class="badge-modal-medal-row ${themeClass} ${levelClass}" style="${lockedRowStyle}">
                    <div class="medal-container" style="width: 70px; height: 80px;">
                        <svg class="medal-svg" viewBox="0 0 100 100">
                            <path class="ribbon-tail-left" d="M 36 50 L 15 92 L 32 83 L 45 92 Z" fill="url(#ribbonLeftGrad)" />
                            <path class="ribbon-tail-right" d="M 64 50 L 85 92 L 68 83 L 55 92 Z" fill="url(#ribbonRightGrad)" />
                            <polygon class="medal-scallop-base" fill="url(#${scallopGrad})" points="
                                50,5 53.5,10 58.7,6.3 60.9,12.1 67.2,10.2 68.1,16.7 75,16.4 74.4,22.9 81.3,24.1 79.2,30.3 85.7,32.7 82.2,38.4 87.7,41.9 83,47.1 87.2,51.5 81.5,55.9 84.4,61.4 78,64.8 79.5,70.9 72.8,73.1 73.1,79.5 66.2,80.5 65.1,86.9 58.5,86.4 56.4,92.5 50,90.5 43.6,92.5 41.5,86.4 34.9,86.9 33.8,80.5 26.9,79.5 27.2,73.1 20.5,70.9 22,64.8 15.6,61.4 18.5,55.9 12.8,51.5 17,47.1 12.3,41.9 17.8,38.4 14.3,32.7 20.8,30.3 18.7,24.1 25.6,22.9 25,16.4 31.9,16.7 32.8,10.2 39.1,12.1 41.3,6.3 46.5,10
                            " />
                            <circle class="medal-center" cx="50" cy="50" r="31.5" fill="url(#${innerGrad})" />
                            <circle cx="50" cy="50" r="28" fill="none" stroke="#FFFFFF" stroke-opacity="0.4" stroke-width="0.75" />
                            <path class="medal-gloss" d="M 23 35 A 31.5 31.5 0 0 1 77 35 A 31.5 28 0 0 0 23 35 Z" fill="rgba(255,255,255,0.18)" />
                        </svg>
                        <div class="medal-icon-container" style="font-size: 1.15rem; transform: translate(-50%, -50%) translateY(-4px);">
                            <i class="fas ${isEarned ? iconClass : "fa-lock"}"></i>
                        </div>
                    </div>
                    <div class="badge-modal-info">
                        <h6>${titleText}</h6>
                        <p>${desc}</p>
                        ${dateHtml}
                    </div>
                </div>
            `;
        }
    }

    /**
     * Sleek visual toast notifier
     */
    showToast(message) {
        if (window.showToast) {
            window.showToast(message);
        } else {
            const toastId = 'profile-fallback-toast-' + Date.now();
            const $toast = $(`
                <div id="${toastId}" class="toast-notification" style="border-left: 4px solid #C2185B;">
                    <i class="fas fa-check-circle" style="color: #C2185B;"></i>
                    <span>${message}</span>
                    <div class="toast-progress"></div>
                </div>
            `);
            $('body').append($toast);
            $toast.fadeIn(300);
            setTimeout(() => {
                $toast.fadeOut(300, function () { $(this).remove(); });
            }, 4000);
        }
    }

    /**
     * Updates the subscription plan cards in the modal with real data from the API.
     * Maps API packages to the existing Free/Basic/Pro card layout.
     * @param {Array} packages - Array of ServicePackage objects from API
     */
    updateSubscriptionPlans(packages) {
        if (!packages || !Array.isArray(packages)) return;

        packages.forEach(pkg => {
            const planName = pkg.packageName; // "Free", "Basic", "Pro"
            const $card = $(`.sub-plan-card[data-plan-card="${planName}"]`);
            if (!$card.length) return;

            // Update price display
            const priceFormatted = new Intl.NumberFormat('vi-VN').format(pkg.price);
            $card.find('.plan-price-display').text(`${priceFormatted} VND`);

            // Update data attributes on the button
            const $btn = $card.find('.btn-select-plan');
            $btn.attr('data-package-id', pkg.packageID);
            $btn.attr('data-price', pkg.price);

            // Update thread/undo limit text
            const isUnlimitedThread = pkg.threadLimit >= 999999;
            const isUnlimitedUndo = pkg.undoLimit >= 999999;

            $card.find('.plan-thread-limit').text(isUnlimitedThread ? 'Unlimited' : pkg.threadLimit);
            $card.find('.plan-undo-limit').text(isUnlimitedUndo ? 'Unlimited' : pkg.undoLimit);
        });
    }

    /**
     * Opens subscription plans overlay with dark slate glassmorphism
     */
    openSubscriptionModal(currentPlan) {
        const overlay = document.getElementById('subscription-modal-overlay');
        const content = document.getElementById('subscription-modal-content');
        if (!overlay || !content) return;

        overlay.style.display = 'flex';
        // Trigger layout reflow for transitions
        void overlay.offsetWidth;
        content.style.opacity = '1';
        content.style.transform = 'scale(1)';

        // Iterate plans cards to highlight active one
        $('.sub-plan-card').each(function () {
            const plan = $(this).attr('data-plan-card');
            const $btn = $(this).find('.btn-select-plan');
            const $tag = $(this).find('.pro-tag');

            if (plan === currentPlan) {
                $(this).addClass('active-package');
                $btn.text('Active Plan')
                    .attr('disabled', 'disabled')
                    .css({
                        'background': '#f1f5f9',
                        'color': '#475569',
                        'cursor': 'default',
                        'box-shadow': 'none',
                        'pointer-events': 'none'
                    });
                if ($tag.length) $tag.show();
            } else {
                $(this).removeClass('active-package');
                $btn.removeAttr('disabled').css({
                    'cursor': 'pointer',
                    'pointer-events': 'auto'
                });

                if (plan === 'Free') {
                    $btn.text('Activate Standard').css({
                        'background': '#f1f5f9',
                        'color': '#475569',
                        'box-shadow': 'none'
                    });
                } else if (plan === 'Basic') {
                    $btn.text('Upgrade Now').css({
                        'background': 'linear-gradient(135deg, #c2185b 0%, #ec4899 100%)',
                        'color': '#fff',
                        'box-shadow': '0 4px 12px rgba(194, 24, 91, 0.25)'
                    });
                } else if (plan === 'Pro') {
                    $btn.text('Upgrade Now').css({
                        'background': 'linear-gradient(135deg, #7b1fa2 0%, #c084fc 100%)',
                        'color': '#fff',
                        'box-shadow': '0 4px 12px rgba(123, 31, 162, 0.25)'
                    });
                }
                if ($tag.length) $tag.hide();
            }
        });
    }

    /**
     * Closes subscription plans overlay
     */
    closeSubscriptionModal() {
        const overlay = document.getElementById('subscription-modal-overlay');
        const content = document.getElementById('subscription-modal-content');
        if (!overlay || !content) return;

        content.style.opacity = '0';
        content.style.transform = 'scale(0.95)';
        setTimeout(() => {
            overlay.style.display = 'none';
        }, 300);
    }

    /**
     * Displays redirect loading overlay for VNPay Payment Gateway
     */
    showVNPayLoading() {
        const loading = document.getElementById('vnpay-loading-overlay');
        if (loading) loading.style.display = 'flex';
    }

    /**
     * Hides loading overlay
     */
    hideVNPayLoading() {
        const loading = document.getElementById('vnpay-loading-overlay');
        if (loading) loading.style.display = 'none';
    }

    /**
     * Displays VNPay status verification overlay
     */
    showVNPayReturn() {
        const verify = document.getElementById('vnpay-return-overlay');
        if (verify) verify.style.display = 'flex';
    }

    /**
     * Hides VNPay return verification overlay
     */
    hideVNPayReturn() {
        const verify = document.getElementById('vnpay-return-overlay');
        if (verify) verify.style.display = 'none';
    }

    /**
     * Displays payment successful screen overlay customized for Package Upgrades
     */
    showPaymentSuccess(planName, price) {
        const success = document.getElementById('payment-success-overlay');
        if (!success) return;

        // Customize the text for subscription business
        $('#payment-success-overlay h2').text("Subscription Upgraded!");

        // Find the details card wrapper inside success screen
        const cardBox = $('#payment-success-overlay div[style*="background: #f8fafc"]');
        if (cardBox.length) {
            cardBox.html(`
                <div style="display: flex; justify-content: space-between; margin-bottom: 10px;">
                    <span style="color: #64748b;">Selected Plan:</span>
                    <strong style="color: #0f172a;">${planName} Package</strong>
                </div>
                <div style="display: flex; justify-content: space-between; margin-bottom: 10px;">
                    <span style="color: #64748b;">Amount:</span>
                    <strong style="color: #C2185B;">${new Intl.NumberFormat('vi-VN').format(price)} VND</strong>
                </div>
                <div style="display: flex; justify-content: space-between;">
                    <span style="color: #64748b;">Status:</span>
                    <strong style="color: #166534;">Active, instant privileges unlocked</strong>
                </div>
            `);
        }

        // Customize action buttons
        const $viewOrderBtn = $('#payment-success-overlay .btn-view-order');
        const $continueBtn = $('#payment-success-overlay .btn-continue-shopping');
        if ($viewOrderBtn.length) {
            // Clone the button to remove checkout.js event listeners (preventing alert)
            const $newBtn = $viewOrderBtn.clone();
            $newBtn.text("Enjoy Premium Privileges")
                .css({
                    'background': planName === 'Pro' ? 'linear-gradient(135deg, #7b1fa2 0%, #c084fc 100%)' : 'linear-gradient(135deg, #c2185b 0%, #ec4899 100%)',
                    'box-shadow': '0 6px 20px rgba(0, 0, 0, 0.15)',
                    'padding': '12px 30px',
                    'border-radius': '30px'
                });
            $viewOrderBtn.replaceWith($newBtn);
        }
        if ($continueBtn.length) {
            $continueBtn.hide();
        }

        success.style.display = 'flex';
    }

    /**
     * Hides payment success screen overlay
     */
    hidePaymentSuccess() {
        const success = document.getElementById('payment-success-overlay');
        if (success) {
            success.style.display = 'none';
            // Restore continue button visibility for other contexts
            $('#payment-success-overlay .btn-continue-shopping').show();
        }
    }

    /**
     * Displays payment failed screen overlay customized for Package Upgrades
     */
    showPaymentFailed(planName) {
        const failed = document.getElementById('payment-failed-overlay');
        if (!failed) return;

        // Customize text descriptions
        $('#payment-failed-overlay h2').text("Upgrade Failed");
        $('#payment-failed-overlay p').html(`The payment transaction for upgrading to the <strong>${planName} Plan</strong> has failed or was cancelled. Please try again to unlock premium privileges.`);

        // Customize action buttons
        const $retryBtn = $('#payment-failed-overlay .btn-retry-payment');
        const $changeMethodBtn = $('#payment-failed-overlay .btn-change-method');
        const $cancelBtn = $('#payment-failed-overlay .btn-cancel-order');

        if ($retryBtn.length) {
            const $newRetry = $retryBtn.clone();
            $newRetry.text("Retry Upgrade")
                .css({
                    'background': '#c2185b',
                    'box-shadow': '0 4px 12px rgba(194, 24, 91, 0.2)'
                });
            $retryBtn.replaceWith($newRetry);
        }
        if ($changeMethodBtn.length) {
            $changeMethodBtn.hide();
        }
        if ($cancelBtn.length) {
            const $newCancel = $cancelBtn.clone();
            $newCancel.text("Close").css({
                'background': 'transparent',
                'color': '#64748b',
                'border': 'none',
                'font-weight': '700',
                'margin-top': '5px'
            });
            $cancelBtn.replaceWith($newCancel);
        }

        failed.style.display = 'flex';
    }

    /**
     * Hides payment failed screen overlay
     */
    hidePaymentFailed() {
        const failed = document.getElementById('payment-failed-overlay');
        if (failed) {
            failed.style.display = 'none';
            // Restore change method visibility for other contexts
            $('#payment-failed-overlay .btn-change-method').show();
        }
    }

    /**
     * Opens fullscreen avatar cropper modal overlay
     */
    openCropperModal() {
        const overlay = document.getElementById('avatar-cropper-overlay');
        const content = document.getElementById('avatar-cropper-content');
        if (!overlay || !content) return;

        overlay.style.display = 'flex';
        // Trigger layout reflow
        void overlay.offsetWidth;
        content.style.opacity = '1';
        content.style.transform = 'scale(1)';

        // Reset elements
        $('#avatar-file-input').val('');
        $('#cropper-upload-placeholder').show();
        $('#cropper-workspace').hide();
        $('#cropper-controls').hide();
        $('#btn-save-cropper').attr('disabled', 'disabled');
        $('#cropper-image').attr('src', '');
    }

    /**
     * Closes fullscreen avatar cropper modal overlay
     */
    closeCropperModal() {
        const overlay = document.getElementById('avatar-cropper-overlay');
        const content = document.getElementById('avatar-cropper-content');
        if (!overlay || !content) return;

        content.style.opacity = '0';
        content.style.transform = 'scale(0.95)';
        setTimeout(() => {
            overlay.style.display = 'none';
        }, 300);
    }

    /**
     * Loads selected image into cropper workspace
     */
    loadCropperImage(src) {
        $('#cropper-upload-placeholder').hide();
        $('#cropper-workspace').css('display', 'flex');
        $('#cropper-controls').css('display', 'flex');
        $('#btn-save-cropper').removeAttr('disabled');

        const $img = $('#cropper-image');
        $img.attr('src', src);

        // Reset transform values
        $img.css({
            'left': '50%',
            'top': '50%',
            'transform': 'translate(-50%, -50%) translate(0px, 0px) scale(1)'
        });
        $('#zoom-range').val(1);
    }

    /**
     * Adjusts current scale & translation styling of cropper preview image
     */
    updateCropperImageTransform(scale, x, y) {
        $('#cropper-image').css({
            'transform': `translate(-50%, -50%) translate(${x}px, ${y}px) scale(${scale})`
        });
        $('#zoom-range').val(scale);
    }

    /**
     * Renders cropped circular area onto offscreen Canvas and returns Base64 Data URL
     */
    getCroppedImage(scale, x, y, callback) {
        const img = document.getElementById('cropper-image');
        if (!img || !img.src) return;

        const originalImg = new Image();
        originalImg.onload = function () {
            const canvas = document.createElement('canvas');
            canvas.width = 200;
            canvas.height = 200;
            const ctx = canvas.getContext('2d');

            const wImg = originalImg.naturalWidth;
            const hImg = originalImg.naturalHeight;

            // Crop center coordinates on the source image, taking into account zoom & pan offset
            const origCenterX = wImg / 2 - (x / scale);
            const origCenterY = hImg / 2 - (y / scale);
            const origSize = 200 / scale;
            const origX = origCenterX - origSize / 2;
            const origY = origCenterY - origSize / 2;

            // Draw circular clip path for perfect cropping preview export
            ctx.beginPath();
            ctx.arc(100, 100, 100, 0, Math.PI * 2);
            ctx.clip();

            // Draw to offscreen canvas
            ctx.drawImage(
                originalImg,
                origX, origY, origSize, origSize,
                0, 0, 200, 200
            );

            // Export as JPEG Data URI
            const croppedDataUrl = canvas.toDataURL('image/jpeg', 0.9);
            callback(croppedDataUrl);
        };
        originalImg.src = img.src;
    }
}

// Attach to window namespace for global access
window.ProfileView = ProfileView;
