(function (window, document, $) {
    if (!$) {
        console.error('layout.js requires jQuery.');
        return;
    }

    $(document).ready(function () {
        initUserDropdown();
        initCartButton();
        initCartBadge();
        initActiveNavbar();
        initIdleTindbookPopup();
    });

    function initUserDropdown() {
        const $userDropdown = $('.user-dropdown');

        $userDropdown.off('click.bookblossomLayout').on('click.bookblossomLayout', function (e) {
            e.stopPropagation();
            $(this).toggleClass('active');
            // Always hide notification dropdown when user dropdown is clicked
            $('#notification-dropdown').removeClass('active');
            $('#notification-bell').removeClass('active');
        });

        $(document).off('click.bookblossomLayoutDropdown').on('click.bookblossomLayoutDropdown', function (e) {
            if (!$userDropdown.is(e.target) && $userDropdown.has(e.target).length === 0) {
                $userDropdown.removeClass('active');
            }
        });
    }

    function initCartButton() {
        $('#nav-cart-btn')
            .off('click.bookblossomLayout')
            .on('click.bookblossomLayout', function (e) {
                e.preventDefault();
                window.location.href = '/Cart';
            });
    }

    function initCartBadge() {
        if (window.BookBlossomCart) {
            window.BookBlossomCart.updateBadge();
        }
    }

    function initActiveNavbar() {
        const path = normalizePath(window.location.pathname);

        $('.nav-links a').removeClass('active');
        $('#nav-cart-btn').removeClass('active');

        $('#main-navbar').addClass('navbar-light');

        if (path === '/') {
            $('#nav-home').addClass('active');
            $('#main-navbar').removeClass('navbar-light');
            return;
        }

        const navMap = {
            '/Explore': '#nav-explore',
            '/Tindbook': '#nav-tindbook',
            '/Community': '#nav-community',
            '/BlindDate': '#nav-blind-date',
            '/Reviews': '#nav-reviews',
            '/Cart': '#nav-cart-btn'
        };

        const activeSelector = navMap[path];

        if (activeSelector) {
            $(activeSelector).addClass('active');
        }
    }

    function normalizePath(path) {
        if (!path || path === '') return '/';

        if (path.length > 1 && path.endsWith('/')) {
            path = path.slice(0, -1);
        }

        return path;
    }

    function initIdleTindbookPopup() {
        const path = normalizePath(window.location.pathname).toLowerCase();

        // Exclude pages: Product details, Checkout, Tindbook itself, etc.
        const isProductDetail = path.includes('/book') || path.includes('/product') || path.includes('/realbook');
        const isCheckout = path.includes('/checkout');
        const isTindbook = path.includes('/tindbook');

        if (isProductDetail || isCheckout || isTindbook) {
            return;
        }

        let idleTimeoutMs = 0;
        if (path === '/' || path.includes('/search') || path.includes('/explore')) {
            idleTimeoutMs = 2 * 60 * 1000; // 2 minutes
        } else if (path.includes('/community') || path.includes('/thread')) {
            idleTimeoutMs = 5 * 60 * 1000; // 5 minutes
        } else {
            // Other pages: default 5 minutes
            idleTimeoutMs = 5 * 60 * 1000;
        }

        let idleTimer;

        function resetIdleTimer() {
            clearTimeout(idleTimer);
            idleTimer = setTimeout(showTindbookPopup, idleTimeoutMs);
        }

        function showTindbookPopup() {
            // Check if popup already exists
            if (document.getElementById('tindbook-idle-popup')) return;

            // Create popup element with stunning HSL glassmorphic design
            const $popup = $(`
                <div id="tindbook-idle-popup" class="tindbook-idle-modal-overlay">
                    <div class="tindbook-idle-modal-content">
                        <button class="tindbook-idle-modal-close" type="button">&times;</button>
                        <div class="tindbook-idle-modal-header">
                            <div class="tindbook-idle-icon-pulse">
                                <i class="fas fa-heart"></i>
                            </div>
                        </div>
                        <div class="tindbook-idle-modal-body">
                            <h3>Looking for Your Next Book Soulmate?</h3>
                            <p>
                                Not sure what to read next? Try our <strong>Tindbook</strong> swipe feature and discover the perfect book match waiting for you!
                            </p>
                            <button id="btn-tindbook-go" class="tindbook-idle-btn-primary">
                                Start Swiping!
                            </button>
                        </div>
                    </div>
                </div>
            `);

            $('body').append($popup);

            // Add CSS styles if not already injected
            if (!document.getElementById('tindbook-idle-popup-styles')) {
                const styles = `
                    <style id="tindbook-idle-popup-styles">
                        .tindbook-idle-modal-overlay {
                            position: fixed;
                            top: 0;
                            left: 0;
                            width: 100%;
                            height: 100%;
                            background: rgba(15, 12, 12, 0.6);
                            backdrop-filter: blur(8px);
                            z-index: 10000;
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            opacity: 0;
                            transition: opacity 0.4s ease;
                        }
                        .tindbook-idle-modal-overlay.active {
                            opacity: 1;
                        }
                        .tindbook-idle-modal-content {
                            background: rgba(255, 255, 255, 0.95);
                            border-radius: 24px;
                            padding: 40px;
                            max-width: 450px;
                            width: 90%;
                            box-shadow: 0 20px 40px rgba(0, 0, 0, 0.2);
                            border: 1px solid rgba(255, 255, 255, 0.4);
                            position: relative;
                            text-align: center;
                            transform: scale(0.8) translateY(20px);
                            transition: transform 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
                            font-family: 'Outfit', 'Inter', sans-serif;
                        }
                        .tindbook-idle-modal-overlay.active .tindbook-idle-modal-content {
                            transform: scale(1) translateY(0);
                        }
                        .tindbook-idle-modal-close {
                            position: absolute;
                            top: 15px;
                            right: 20px;
                            background: none;
                            border: none;
                            font-size: 28px;
                            cursor: pointer;
                            color: #888;
                            transition: color 0.2s;
                        }
                        .tindbook-idle-modal-close:hover {
                            color: #e57373;
                        }
                        .tindbook-idle-icon-pulse {
                            width: 80px;
                            height: 80px;
                            border-radius: 50%;
                            background: linear-gradient(135deg, #f06292, #f50057);
                            display: flex;
                            align-items: center;
                            justify-content: center;
                            margin: 0 auto 24px;
                            box-shadow: 0 10px 20px rgba(245, 0, 87, 0.3);
                            animation: idlePulse 2s infinite;
                        }
                        .tindbook-idle-icon-pulse i {
                            font-size: 36px;
                            color: #fff;
                        }
                        .tindbook-idle-modal-body h3 {
                            font-family: 'Lora', 'Playfair Display', serif;
                            font-size: 22px;
                            color: #2c3e50;
                            margin-bottom: 12px;
                            font-weight: 700;
                        }
                        .tindbook-idle-modal-body p {
                            font-size: 15px;
                            color: #555;
                            line-height: 1.6;
                            margin-bottom: 28px;
                        }
                        .tindbook-idle-btn-primary {
                            background: linear-gradient(135deg, #EBBAB9, #D99593);
                            color: #fff;
                            border: none;
                            border-radius: 50px;
                            padding: 14px 40px;
                            font-size: 16px;
                            font-weight: 600;
                            cursor: pointer;
                            box-shadow: 0 6px 15px rgba(217, 149, 147, 0.4);
                            transition: all 0.3s ease;
                            outline: none;
                            width: 100%;
                        }
                        .tindbook-idle-btn-primary:hover {
                            transform: translateY(-2px);
                            box-shadow: 0 8px 20px rgba(217, 149, 147, 0.6);
                        }
                        .tindbook-idle-btn-primary:active {
                            transform: translateY(0);
                        }
                        @keyframes idlePulse {
                            0% {
                                transform: scale(0.95);
                                box-shadow: 0 0 0 0 rgba(245, 0, 87, 0.7);
                            }
                            70% {
                                transform: scale(1);
                                box-shadow: 0 0 0 15px rgba(245, 0, 87, 0);
                            }
                            100% {
                                transform: scale(0.95);
                                box-shadow: 0 0 0 0 rgba(245, 0, 87, 0);
                            }
                        }
                    </style>
                `;
                $('head').append(styles);
            }

            // Fade in
            setTimeout(() => {
                $popup.addClass('active');
            }, 50);

            // Event Handlers
            $popup.find('.tindbook-idle-modal-close').on('click', () => {
                dismissPopup();
            });

            $popup.on('click', (e) => {
                if ($(e.target).hasClass('tindbook-idle-modal-overlay')) {
                    dismissPopup();
                }
            });

            $popup.find('#btn-tindbook-go').on('click', () => {
                dismissPopup();
                window.location.href = '/Tindbook';
            });

            // Disable event listeners when shown
            $(document).off('.tindbookIdle');
        }

        function dismissPopup() {
            const $popup = $('#tindbook-idle-popup');
            if ($popup.length) {
                $popup.removeClass('active');
                setTimeout(() => {
                    $popup.remove();
                }, 400);
            }
        }

        // Activity listeners to reset the timer
        $(document).on('mousemove.tindbookIdle click.tindbookIdle keydown.tindbookIdle scroll.tindbookIdle touchstart.tindbookIdle', resetIdleTimer);

        // Initial start
        resetIdleTimer();
    }

    window.BookBlossomLayout = {
        refreshNavbar: initActiveNavbar,
        refreshCartBadge: initCartBadge
    };
})(window, document, window.jQuery);