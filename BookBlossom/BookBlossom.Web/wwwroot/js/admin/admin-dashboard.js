document.addEventListener('DOMContentLoaded', function() {
    console.log("Admin Dashboard initialized.");

    // Khởi tạo SPA Router ngay khi DOM sẵn sàng
    if (window.spaRouter) {
        window.spaRouter.init();
    }
    
    // Highlight active menu dựa trên URL hiện tại
    const currentPath = window.location.pathname;
    const menuItems = document.querySelectorAll('.sidebar-menu li');
    
    menuItems.forEach(item => {
        const link = item.getAttribute('data-href');
        if (link && currentPath.toLowerCase().includes(link.toLowerCase())) {
            item.classList.add('active');
        } else {
            item.classList.remove('active');
        }
    });

    // Gắn SPA navigation vào các sidebar menu item
    menuItems.forEach(item => {
        item.addEventListener('click', function() {
            const href = this.getAttribute('data-href');
            if (!href) return;

            // Logout vẫn dùng full navigation
            if (href.includes('Logout')) {
                window.location.href = href;
                return;
            }

            // Dùng SPA Router nếu có, fallback về location.href
            if (window.spaRouter) {
                window.spaRouter.navigate(href);
            } else {
                window.location.href = href;
            }
        });
    });

    // Xử lý browser back/forward bằng cách update active state
    window.addEventListener('spa:page-ready', function(e) {
        const url = e.detail && e.detail.url ? e.detail.url : window.location.pathname;
        const menuItems = document.querySelectorAll('.sidebar-menu li');
        menuItems.forEach(item => {
            const link = item.getAttribute('data-href');
            if (link && url.toLowerCase().includes(link.toLowerCase())) {
                item.classList.add('active');
            } else {
                item.classList.remove('active');
            }
        });
    });
});
