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
            .removeClass('tier-Copper tier-Silver tier-Gold tier-Diamond')
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
        $('#display-thread-counter').text(`${user.currentMonthThreadCount} / ${user.maxMonthlyThreadLimit} posts`);
        const threadPercent = Math.min(100, (user.currentMonthThreadCount / user.maxMonthlyThreadLimit) * 100);
        $('#display-thread-progress').css('width', threadPercent + '%');

        $('#display-undo-counter').text(`${user.dailyUndoCount} / ${user.maxDailyUndoLimit} undos`);
        const undoPercent = Math.min(100, (user.dailyUndoCount / user.maxDailyUndoLimit) * 100);
        $('#display-undo-progress').css('width', undoPercent + '%');

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
     * Renders badge medals to cabinet grid layout
     */
    renderBadgesGrid(cabinetElement, badges, maxDisplay) {
        cabinetElement.empty();
        let displayCount = 0;

        badges.forEach(badge => {
            if (displayCount >= maxDisplay) return;
            cabinetElement.append(this.getMedalHtml(badge, 'small'));
            displayCount++;
        });

        if (badges.length > maxDisplay) {
            cabinetElement.append(`
                <div class="badge-medal-view-all" data-bs-toggle="modal" data-bs-target="#allBadgesModal">
                    <div class="view-all-circle">
                        <i class="fas fa-plus"></i>
                        <span>${badges.length - maxDisplay}</span>
                    </div>
                    <span class="view-all-label">View All</span>
                </div>
            `);
        }
    }

    /**
     * Renders badge medals list in the modal view
     */
    renderBadgesModal(modalBodyElement, badges, earnedDates) {
        modalBodyElement.empty();
        badges.forEach(badge => {
            const dateVal = (earnedDates && earnedDates[badge]) ? earnedDates[badge] : new Date().toISOString().split('T')[0];
            modalBodyElement.append(this.getMedalHtml(badge, 'large', dateVal));
        });
    }

    /**
     * Formulates complete HTML structure of a single 3D physical-like medal using custom SVG
     */
    getMedalHtml(badgeName, sizeClass, earnedDate = null) {
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

        // Return modular HTML: Grid cabinet vs detailed Modal item
        if (sizeClass === 'small') {
            return `
                <div class="badge-medal ${themeClass} ${levelClass}" title="${titleText}: ${desc}">
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
                            <i class="fas ${iconClass}"></i>
                        </div>
                    </div>
                    <span class="medal-title">${titleText}</span>
                </div>
            `;
        } else {
            // Large list item in modal
            let formattedDate = '';
            if (earnedDate) {
                const dateObj = new Date(earnedDate);
                const options = { year: 'numeric', month: 'long', day: 'numeric' };
                formattedDate = dateObj.toLocaleDateString('en-US', options);
            } else {
                formattedDate = new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
            }

            return `
                <div class="badge-modal-medal-row ${themeClass} ${levelClass}">
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
                            <i class="fas ${iconClass}"></i>
                        </div>
                    </div>
                    <div class="badge-modal-info">
                        <h6>${titleText}</h6>
                        <p>${desc}</p>
                        <span class="badge-earned-date"><i class="far fa-calendar-alt me-1"></i> Earned on: ${formattedDate}</span>
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
}

// Attach to window namespace for global access
window.ProfileView = ProfileView;
