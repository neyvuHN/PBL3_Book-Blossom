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
    });

    function initUserDropdown() {
        const $userDropdown = $('.user-dropdown');

        $userDropdown.off('click.bookblossomLayout').on('click.bookblossomLayout', function (e) {
            e.stopPropagation();
            $(this).toggleClass('active');
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

    window.BookBlossomLayout = {
        refreshNavbar: initActiveNavbar,
        refreshCartBadge: initCartBadge
    };
})(window, document, window.jQuery);