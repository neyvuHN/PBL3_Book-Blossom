// BookBlossom Profile JS Logic
$(document).ready(function () {
    
    // Core User Model Default State
    const defaultUser = {
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
        badges: ["Review Champion", "Blind Date Boss", "Perfect Streak", "Bookworm Elite"],
        subscriptionPackage: "Basic", // Free, Basic, Pro
        currentMonthThreadCount: 8,
        maxMonthlyThreadLimit: 20,
        dailyUndoCount: 3,
        maxDailyUndoLimit: 5
    };

    let currentUser = JSON.parse(localStorage.getItem('BookBlossomUser'));
    if (!currentUser) {
        currentUser = defaultUser;
        localStorage.setItem('BookBlossomUser', JSON.stringify(currentUser));
    }
    
    // Migration for old badge format
    if (currentUser.badges && currentUser.badges.includes("Review Champion")) {
        currentUser.badges = [
            "Chiến thần Review - Cấp 2", 
            "Sứ giả Tri thức", 
            "Trùm Blind Date - Cấp 3", 
            "Mọt sách chính hiệu",
            "Người dùng gương mẫu",
            "Cánh tay đắc lực"
        ];
        localStorage.setItem('BookBlossomUser', JSON.stringify(currentUser));
    }

    // List of gorgeous preset avatars to cycle through
    const presetAvatars = [
        "/images/Avatar/avatar1.jpg",
        "/images/Avatar/avatar2.jpg",
        "/images/Avatar/avatar3.jpg",
        "/images/Avatar/avatar4.jpg"
    ];

    // Core function to synchronize and update the entire DOM with user state
    function updateProfileUI() {
        // Public Details
        $('#display-fullname').text(currentUser.fullName);
        $('#display-username').text('@' + currentUser.username);
        $('#display-bio').text('"' + currentUser.bio + '"');
        $('#display-avatar-img').attr('src', currentUser.avatar);

        // Contact & Personal
        $('#display-phone').text(currentUser.phoneNumber);
        $('#display-email').text(currentUser.email);
        $('#display-gender').text(currentUser.gender);
        
        // Format Birthdate nicely
        if (currentUser.birthdate) {
            const dateObj = new Date(currentUser.birthdate);
            const options = { year: 'numeric', month: 'long', day: 'numeric' };
            $('#display-birthdate').text(dateObj.toLocaleDateString('en-US', options));
        }

        // Subscription Tier Badge
        $('#display-sub-badge')
            .text(currentUser.subscriptionPackage + ' Package')
            .removeClass('sub-tier-Free sub-tier-Basic sub-tier-Pro')
            .addClass('sub-tier-' + currentUser.subscriptionPackage);

        // Membership Tier
        $('#display-tier-text').text(currentUser.membershipTier);
        $('#display-tier-badge')
            .removeClass('tier-Copper tier-Silver tier-Gold tier-Diamond')
            .addClass('tier-' + currentUser.membershipTier);

        // Spending details & calculations
        const spendingVal = Number(currentUser.totalSpending) || 0;
        const thresholdVal = Number(currentUser.nextTierThreshold) || 5000000;
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
        const repScore = Math.min(150, Math.max(0, Number(currentUser.reputationScore) || 0));
        $('#display-rep-score').text(repScore);
        const repPercent = (repScore / 150) * 100;
        $('#reputation-svg-fill').attr('stroke-dasharray', `${repPercent}, 100`);

        // Recalculate privileges warnings
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
        const streak = Math.min(3, Math.max(0, Number(currentUser.currentOrderStreak) || 0));
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
        $('#display-thread-counter').text(`${currentUser.currentMonthThreadCount} / ${currentUser.maxMonthlyThreadLimit} posts`);
        const threadPercent = Math.min(100, (currentUser.currentMonthThreadCount / currentUser.maxMonthlyThreadLimit) * 100);
        $('#display-thread-progress').css('width', threadPercent + '%');

        $('#display-undo-counter').text(`${currentUser.dailyUndoCount} / ${currentUser.maxDailyUndoLimit} undos`);
        const undoPercent = Math.min(100, (currentUser.dailyUndoCount / currentUser.maxDailyUndoLimit) * 100);
        $('#display-undo-progress').css('width', undoPercent + '%');

        // Badge Cabinet Rendering
        const cabinet = $('#display-badges-cabinet');
        cabinet.empty();
        
        let displayCount = 0;
        let maxDisplay = 4;
        
        currentUser.badges.forEach(badge => {
            if (displayCount >= maxDisplay) return;
            
            let badgeClass = "badge-default";
            let badgeIcon = "fa-medal";
            
            if (badge.includes("Chiến thần Review")) { badgeClass = "badge-review-champ"; badgeIcon = "fa-star"; }
            else if (badge === "Sứ giả Tri thức") { badgeClass = "badge-knowledge-envoy"; badgeIcon = "fa-share-nodes"; }
            else if (badge.includes("Trùm Blind Date")) { badgeClass = "badge-blind-date-boss"; badgeIcon = "fa-mask"; }
            else if (badge === "Mọt sách chính hiệu") { badgeClass = "badge-bookworm"; badgeIcon = "fa-book-open"; }
            else if (badge === "Người dùng gương mẫu") { badgeClass = "badge-model-user"; badgeIcon = "fa-shield-halved"; }
            else if (badge === "Cánh tay đắc lực") { badgeClass = "badge-mod-assistant"; badgeIcon = "fa-hands-helping"; }
            
            let levelClass = "";
            if (badge.includes("Cấp 1")) levelClass = "badge-frame-lvl1";
            if (badge.includes("Cấp 2")) levelClass = "badge-frame-lvl2";
            if (badge.includes("Cấp 3")) levelClass = "badge-frame-lvl3";

            cabinet.append(`
                <div class="badge-chip ${badgeClass} ${levelClass}" title="${badge}">
                    <i class="fas ${badgeIcon}"></i>
                    <span>${badge}</span>
                </div>
            `);
            displayCount++;
        });

        if (currentUser.badges.length > maxDisplay) {
            cabinet.append(`
                <div class="badge-chip badge-view-all" data-bs-toggle="modal" data-bs-target="#allBadgesModal">
                    <span>+${currentUser.badges.length - maxDisplay} View All</span>
                </div>
            `);
        }

        // Update modal body
        const modalBody = $('#allBadgesModal .modal-body .d-flex');
        if (modalBody.length) {
            modalBody.empty();
            currentUser.badges.forEach(badge => {
                let badgeClass = "badge-default";
                let badgeIcon = "fa-medal";
                let badgeDesc = "A well-deserved badge.";
                
                if (badge.includes("Chiến thần Review")) { badgeClass = "badge-review-champ"; badgeIcon = "fa-star"; badgeDesc = "Awarded for writing high-quality book reviews with photos."; }
                else if (badge === "Sứ giả Tri thức") { badgeClass = "badge-knowledge-envoy"; badgeIcon = "fa-share-nodes"; badgeDesc = "Awarded for actively sharing book links to social media."; }
                else if (badge.includes("Trùm Blind Date")) { badgeClass = "badge-blind-date-boss"; badgeIcon = "fa-mask"; badgeDesc = "Awarded for courageous exploration of Blind Date Books."; }
                else if (badge === "Mọt sách chính hiệu") { badgeClass = "badge-bookworm"; badgeIcon = "fa-book-open"; badgeDesc = "Awarded for purchasing books across 5 different genres."; }
                else if (badge === "Người dùng gương mẫu") { badgeClass = "badge-model-user"; badgeIcon = "fa-shield-halved"; badgeDesc = "Maintained max reputation for 3 consecutive months."; }
                else if (badge === "Cánh tay đắc lực") { badgeClass = "badge-mod-assistant"; badgeIcon = "fa-hands-helping"; badgeDesc = "Provided accurate reports to help moderation."; }

                let levelClass = "";
                if (badge.includes("Cấp 1")) levelClass = "badge-frame-lvl1";
                if (badge.includes("Cấp 2")) levelClass = "badge-frame-lvl2";
                if (badge.includes("Cấp 3")) levelClass = "badge-frame-lvl3";

                modalBody.append(`
                    <div class="badge-modal-item ${badgeClass} ${levelClass}">
                        <div class="badge-modal-icon">
                            <i class="fas ${badgeIcon}"></i>
                        </div>
                        <div class="badge-modal-info">
                            <h6>${badge}</h6>
                            <p>${badgeDesc}</p>
                        </div>
                    </div>
                `);
            });
        }
        
        // Mystic Aura Update
        if (currentUser.badges.includes("Trùm Blind Date - Cấp 3")) {
            $('.profile-avatar-wrapper').addClass("mystic-aura");
        } else {
            $('.profile-avatar-wrapper').removeClass("mystic-aura");
        }

        // Set values inside Edit Form inputs to match state
        $('#input-fullname').val(currentUser.fullName);
        $('#input-username').val(currentUser.username);
        $('#input-bio').val(currentUser.bio);
        $('#input-phone').val(currentUser.phoneNumber);
        $('#input-email').val(currentUser.email);
        $('#input-gender').val(currentUser.gender);
        $('#input-birthdate').val(currentUser.birthdate);
    }

    // Helper to display a sleek visual toast
    function showProfileToast(message) {
        if (window.showToast) {
            window.showToast(message);
        } else {
            // Fallback premium toast injection if global showToast is unavailable
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

    // Interactive Edit Profile Toggling
    $('#btn-edit-profile-toggle').on('click', function () {
        $('#edit-profile-form').slideToggle(300);
        $('html, body').animate({
            scrollTop: $("#edit-profile-form").offset().top - 120
        }, 500);
    });

    $('#btn-edit-profile-cancel').on('click', function () {
        $('#edit-profile-form').slideUp(300);
    });

    // Form Submission & State persistence
    $('#edit-profile-form').on('submit', function (e) {
        e.preventDefault();
        
        currentUser.fullName = $('#input-fullname').val().trim() || currentUser.fullName;
        currentUser.username = $('#input-username').val().trim().toLowerCase() || currentUser.username;
        currentUser.bio = $('#input-bio').val().trim() || currentUser.bio;
        currentUser.phoneNumber = $('#input-phone').val().trim() || currentUser.phoneNumber;
        currentUser.email = $('#input-email').val().trim() || currentUser.email;
        currentUser.gender = $('#input-gender').val();
        currentUser.birthdate = $('#input-birthdate').val();

        // Save back to LocalStorage
        localStorage.setItem('BookBlossomUser', JSON.stringify(currentUser));
        
        // Update view
        updateProfileUI();
        
        $('#edit-profile-form').slideUp(300);
        showProfileToast("Profile updated successfully!");
    });

    // Premium Interactive: Avatar Preset Cycling
    $('#btn-change-avatar').on('click', function () {
        let currentIdx = presetAvatars.indexOf(currentUser.avatar);
        let nextIdx = (currentIdx + 1) % presetAvatars.length;
        currentUser.avatar = presetAvatars[nextIdx];
        
        localStorage.setItem('BookBlossomUser', JSON.stringify(currentUser));
        updateProfileUI();
        
        showProfileToast("Avatar updated successfully!");
    });

    // Premium Interactive: Package Upgrading (Free -> Basic -> Pro)
    $('#btn-upgrade-pkg, #btn-upgrade-pkg-footer').on('click', function () {
        let nextPkg = "Free";
        if (currentUser.subscriptionPackage === "Free") {
            nextPkg = "Basic";
            currentUser.maxMonthlyThreadLimit = 20;
            currentUser.maxDailyUndoLimit = 5;
            currentUser.badges = ["Review Champion", "Blind Date Boss", "Perfect Streak", "Bookworm Elite"];
        } else if (currentUser.subscriptionPackage === "Basic") {
            nextPkg = "Pro";
            currentUser.maxMonthlyThreadLimit = 100;
            currentUser.maxDailyUndoLimit = 15;
            currentUser.badges = ["Review Champion", "Blind Date Boss", "Perfect Streak", "Bookworm Elite", "Pro Legend", "COD Master"];
        } else {
            nextPkg = "Free";
            currentUser.maxMonthlyThreadLimit = 3;
            currentUser.maxDailyUndoLimit = 2;
            currentUser.badges = ["Bookworm Elite"];
            // Normalize counts if they exceed Free limit
            if (currentUser.currentMonthThreadCount > 3) currentUser.currentMonthThreadCount = 2;
            if (currentUser.dailyUndoCount > 2) currentUser.dailyUndoCount = 1;
        }

        currentUser.subscriptionPackage = nextPkg;
        localStorage.setItem('BookBlossomUser', JSON.stringify(currentUser));
        updateProfileUI();
        
        showProfileToast(`Subscription upgraded to ${nextPkg} successfully!`);
    });

    // Initialize UI
    updateProfileUI();
});
