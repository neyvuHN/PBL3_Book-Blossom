/**
 * apiClient.js - Helper Functions for API calls in Book Blossom
 * This script handles authentication tokens, guest sessions, and common HTTP methods.
 */

const apiClient = (function () {
    // Standard Headers
    function getHeaders(isFormData = false) {
        const headers = {};
        
        if (!isFormData) {
            headers['Content-Type'] = 'application/json';
        }

        // Attach JWT Token if available
        const token = localStorage.getItem('accessToken');
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        // Attach Guest ID if available
        const guestId = localStorage.getItem('guestId');
        if (guestId) {
            headers['X-Guest-Id'] = guestId;
        }

        return headers;
    }

    // Initialize Guest Session if no user is logged in and no guest session exists
    async function initGuestSession() {
        const token = localStorage.getItem('accessToken');
        const guestId = localStorage.getItem('guestId');

        if (!token && !guestId) {
            try {
                const response = await fetch('/api/Guest/session', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' }
                });

                if (response.ok) {
                    const data = await response.json();
                    if (data && data.guestID) {
                        localStorage.setItem('guestId', data.guestID);
                        console.log('Guest session initialized:', data.guestID);
                    }
                }
            } catch (error) {
                console.error('Failed to initialize guest session:', error);
            }
        }
    }

    // Global Error Handler
    function handleError(response, errorData) {
        if (response.status === 401) {
            // Unauthorized - clear token and redirect to login
            localStorage.removeItem('accessToken');
            localStorage.removeItem('refreshToken');
            localStorage.removeItem('userId');
            localStorage.removeItem('userName');
            localStorage.removeItem('roleId');
            
            showToast('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.', 'error', 'Unauthorized');
            setTimeout(() => {
                window.location.href = '/Auth/Login';
            }, 1500);
            
            throw new Error('Unauthorized');
        } else if (response.status === 403) {
            showToast('Bạn không có quyền thực hiện chức năng này.', 'error', 'Forbidden');
            throw new Error('Forbidden');
        } else {
            const message = errorData?.message || 'Đã có lỗi xảy ra. Vui lòng thử lại.';
            throw new Error(message);
        }
    }

    // Core Request wrapper
    async function request(endpoint, options = {}) {
        const url = endpoint;
        const config = {
            ...options,
            headers: {
                ...getHeaders(options.body instanceof FormData),
                ...options.headers,
            },
        };

        const response = await fetch(url, config);
        
        // Handle empty responses
        let data = null;
        const contentType = response.headers.get("content-type");
        if (contentType && contentType.indexOf("application/json") !== -1) {
            data = await response.json();
        } else {
            data = await response.text();
        }

        if (!response.ok) {
            handleError(response, data);
        }

        return data;
    }

    // Generic showToast function (matches the one in existing views)
    function showToast(message, type = 'success', title = '') {
        let container = document.getElementById('toastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toastContainer';
            container.className = 'toast-container';
            document.body.appendChild(container);
        }

        const toast = document.createElement('div');
        toast.className = `custom-toast ${type}`;

        let iconSvg = '';
        if (type === 'success') {
            iconSvg = `
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="#EBBAB9" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>`;
            if (!title) title = 'Success';
        } else if (type === 'error') {
            iconSvg = `
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="#EA4335" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                </svg>`;
            if (!title) title = 'Error';
        } else {
            iconSvg = `
                <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="#4285F4" stroke-width="2">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>`;
            if (!title) title = 'Info';
        }

        toast.innerHTML = `
            <div class="toast-icon">${iconSvg}</div>
            <div class="toast-content">
                <div class="toast-title">${title}</div>
                <div class="toast-message">${message}</div>
            </div>
            <button class="toast-close" type="button">&times;</button>
            <div class="toast-timer-bar">
                <div class="toast-timer-fill"></div>
            </div>
        `;

        container.appendChild(toast);

        setTimeout(() => {
            toast.classList.add('active');
            const fill = toast.querySelector('.toast-timer-fill');
            if (fill) fill.style.width = '0%';
        }, 10);

        const closeBtn = toast.querySelector('.toast-close');
        const dismissToast = () => {
            toast.classList.remove('active');
            setTimeout(() => toast.remove(), 400);
        };

        closeBtn.addEventListener('click', dismissToast);
        setTimeout(dismissToast, 4000);
    }

    // Public API
    return {
        init: initGuestSession,
        
        apiGet: (endpoint) => {
            return request(endpoint, { method: 'GET' });
        },
        
        apiPost: (endpoint, body) => {
            return request(endpoint, {
                method: 'POST',
                body: JSON.stringify(body)
            });
        },
        
        apiPut: (endpoint, body) => {
            return request(endpoint, {
                method: 'PUT',
                body: JSON.stringify(body)
            });
        },
        
        apiDelete: (endpoint) => {
            return request(endpoint, { method: 'DELETE' });
        },
        
        apiUpload: (endpoint, formData) => {
            // Do not JSON.stringify formData, and allow browser to set boundary in Content-Type
            return request(endpoint, {
                method: 'POST',
                body: formData
            });
        },
        
        showToast: showToast
    };
})();

// Auto-initialize when the script is loaded
document.addEventListener('DOMContentLoaded', () => {
    apiClient.init();
});
