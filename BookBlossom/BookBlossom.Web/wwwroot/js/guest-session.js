// guest-session.js
document.addEventListener("DOMContentLoaded", async function () {
    // Hàm thiết lập cookie
    function setCookie(name, value, days) {
        let expires = "";
        if (days) {
            let date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = "; expires=" + date.toUTCString();
        }
        document.cookie = name + "=" + (value || "") + expires + "; path=/";
    }

    // Hàm lấy cookie
    function getCookie(name) {
        let nameEQ = name + "=";
        let ca = document.cookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i];
            while (c.charAt(0) == ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
        }
        return null;
    }

    let guestId = localStorage.getItem("X-Guest-Id") || getCookie("X-Guest-Id");

    // Nếu chưa có GuestId thì gọi API tạo mới
    if (!guestId) {
        try {
            const response = await fetch('/api/Guest/session', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const data = await response.json();
                guestId = data.guestId; // Tùy thuộc vào JSON trả về có chữ hoa hay thường
                if (guestId) {
                    localStorage.setItem("X-Guest-Id", guestId);
                    setCookie("X-Guest-Id", guestId, 365); // Lưu cookie 1 năm
                    if (data.token) {
                        localStorage.setItem("X-Guest-Token", data.token);
                        setCookie("X-Guest-Token", data.token, 365);
                    }
                    console.log("Đã tạo Guest Session thành công:", guestId);
                }
            } else {
                console.error("Lỗi khi tạo Guest Session:", await response.text());
            }
        } catch (error) {
            console.error("Lỗi mạng khi tạo Guest Session:", error);
        }
    } else {
        // Đảm bảo Cookie luôn được đồng bộ nếu bị xóa
        if (!getCookie("X-Guest-Id")) {
            setCookie("X-Guest-Id", guestId, 365);
        }
        // Đảm bảo LocalStorage luôn đồng bộ
        if (!localStorage.getItem("X-Guest-Id")) {
            localStorage.setItem("X-Guest-Id", guestId);
        }
    }
});
