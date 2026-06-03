/**
 * Admin SPA Router
 * ================
 * Intercepts sidebar navigation, fetches pages via fetch(), swaps <main> content,
 * and caches pages in-memory + sessionStorage to avoid repeated full-page reloads.
 *
 * Cache flow:
 *   1. Click sidebar link
 *   2. Check in-memory cache → hit? swap immediately (< 50ms) ✨
 *   3. Miss? fetch full HTML from server, extract <main> + scripts + styles
 *   4. Store in cache, swap content, re-execute page scripts
 *   5. Update browser URL via history.pushState()
 */
class AdminSpaRouter {
    constructor() {
        /** @type {Map<string, {html: string, scripts: string[], styleHrefs: string[], title: string, ts: number}>} */
        this._cache = new Map();

        /** Cache TTL in milliseconds (5 phút) */
        this._cacheTTL = 5 * 60 * 1000;

        /** Danh sách script src đã load vào trang (tránh load trùng) */
        this._loadedScripts = new Set();

        /** Đang fetch không? Tránh double-click */
        this._navigating = false;

        /** Hỗ trợ hủy kết nối fetch cũ khi click chuyển tab mới liên tục */
        this._abortController = null;
        this._navigationId = 0;

        /** Tên event phát khi SPA hoàn tất swap */
        this.PAGE_READY_EVENT = 'spa:page-ready';

        /** Lưu các script src đã có sẵn trong layout (không cần re-load) */
        this._layoutScripts = new Set();
    }

    /**
     * Khởi tạo router. Gọi 1 lần sau DOMContentLoaded.
     */
    init() {
        // Ghi nhận tất cả script đã có trong layout để tránh load lại
        document.querySelectorAll('script[src]').forEach(s => {
            const normalized = this._normalizeUrl(s.src);
            this._layoutScripts.add(normalized);
            this._loadedScripts.add(normalized);
        });

        // Cache trang hiện tại (không cần re-fetch nếu quay lại)
        this._cacheCurrentPage();

        // Xử lý back/forward của browser
        window.addEventListener('popstate', (e) => {
            if (e.state && e.state.url) {
                this._navigateInternal(e.state.url, false);
            }
        });

        console.log('[SPA Router] Initialized. Cached scripts:', this._layoutScripts.size);
    }

    /**
     * Cache nội dung trang hiện tại để không fetch lại nếu quay về.
     */
    _cacheCurrentPage() {
        const url = this._normalizeUrl(window.location.pathname);
        const main = document.querySelector('main[role="main"]');
        if (!main) return;

        const existing = this._cache.get(url);
        if (existing) {
            existing.html = main.innerHTML;
            existing.ts = Date.now();
            return;
        }

        // Lấy scripts của trang hiện tại (những scripts không phải layout) - Chỉ thu thập lần đầu khi chưa có cache
        const pageScripts = [];
        document.querySelectorAll('script').forEach(s => {
            if (s.src) {
                const normalized = this._normalizeUrl(s.src);
                if (!this._layoutScripts.has(normalized)) {
                    pageScripts.push({ type: 'src', value: normalized });
                }
            } else if (s.textContent && s.textContent.trim()) {
                const txt = s.textContent.trim();
                // Bỏ qua script quá ngắn hoặc có khả năng thuộc về layout setup
                if (txt.length > 10) {
                    pageScripts.push({ type: 'inline', value: txt });
                }
            }
        });

        // Lấy inline <style> block đặc thù của page hiện tại
        const normalizedUrl = this._normalizeUrl(window.location.pathname);
        const existingStyle = document.querySelector(`style[data-spa-page="${normalizedUrl}"]`);
        const inlineStyles = existingStyle ? [existingStyle.textContent] : [];

        this._cache.set(url, {
            html: main.innerHTML,
            scripts: pageScripts,
            styleHrefs: this._getCurrentPageStyles(),
            inlineStyles: inlineStyles,
            title: document.title,
            ts: Date.now()
        });
    }

    /**
     * Lấy danh sách href của các <link> stylesheet đặc thù của trang hiện tại.
     */
    _getCurrentPageStyles() {
        const layoutStyles = new Set([
            'bootstrap', 'site.css', 'admin-layout', 'dashboard'
        ]);
        const hrefs = [];
        document.querySelectorAll('link[rel="stylesheet"]').forEach(l => {
            const href = l.getAttribute('href') || '';
            const isLayout = [...layoutStyles].some(k => href.includes(k));
            if (!isLayout && href) {
                hrefs.push(href);
            }
        });
        return hrefs;
    }

    /**
     * Navigate tới một URL admin. Entry point chính.
     * @param {string} url - Ví dụ: /Admin/Orders
     */
    async navigate(url) {
        const normalized = this._normalizeUrl(url);
        const currentNormalized = this._normalizeUrl(window.location.pathname);

        // Không navigate lại trang hiện tại
        if (normalized === currentNormalized) return;

        // Chặn click trùng lặp khi đang tải trang này
        if (this._targetNormalizedUrl === normalized) return;
        this._targetNormalizedUrl = normalized;

        await this._navigateInternal(url, true);
    }

    async _navigateInternal(url, pushState) {
        this._navigationId++;
        const currentNavId = this._navigationId;

        // Dọn dẹp các tàn dư của bootstrap modal (tránh bị kẹt lớp phủ backdrop che màn hình)
        if (window.bootstrap) {
            document.querySelectorAll('.modal.show').forEach(el => {
                try {
                    const modal = window.bootstrap.Modal.getInstance(el) || window.bootstrap.Modal.getOrCreateInstance(el);
                    if (modal) {
                        modal.hide();
                    }
                } catch (e) {
                    console.warn('[SPA Router] Failed to hide modal:', e);
                }
            });
        }
        document.querySelectorAll('.modal-backdrop').forEach(el => el.remove());
        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';

        // Hủy bỏ lệnh tải trang cũ đang chạy (nếu có)
        if (this._abortController) {
            this._abortController.abort();
        }
        this._abortController = new AbortController();
        const signal = this._abortController.signal;

        const normalized = this._normalizeUrl(url);

        try {
            // Chụp lại trạng thái giao diện đã dựng (có dữ liệu) của trang hiện tại trước khi rời đi
            this._cacheCurrentPage();

            // --- 1. Hiện loading indicator ---
            this._showLoading();

            // --- 2. Kiểm tra cache ---
            const cached = this._getFromCache(normalized);

            if (cached) {
                // Cache hit → swap ngay lập tức
                console.log(`[SPA Router] Cache HIT: ${normalized}`);
                if (signal.aborted) return;
                await this._applyPage(cached, url, pushState);
            } else {
                // Cache miss → fetch từ server
                console.log(`[SPA Router] Cache MISS: ${normalized} → fetching...`);
                const pageData = await this._fetchPage(url, signal);
                if (signal.aborted) return;
                if (pageData) {
                    this._cache.set(normalized, { ...pageData, ts: Date.now() });
                    if (signal.aborted) return;
                    await this._applyPage(pageData, url, pushState);
                } else {
                    // Fetch thất bại → fallback full reload
                    window.location.href = url;
                    return;
                }
            }
        } catch (err) {
            if (err.name === 'AbortError') {
                console.log('[SPA Router] Navigation aborted.');
                return;
            }
            console.error('[SPA Router] Navigation error:', err);
            // Fallback: full reload
            window.location.href = url;
        } finally {
            if (this._navigationId === currentNavId) {
                this._abortController = null;
                this._navigating = false;
                this._targetNormalizedUrl = null;
                this._hideLoading();
            }
        }
    }

    /**
     * Lấy từ cache nếu còn hạn (TTL).
     */
    _getFromCache(normalizedUrl) {
        const cached = this._cache.get(normalizedUrl);
        if (!cached) return null;
        if (Date.now() - cached.ts > this._cacheTTL) {
            this._cache.delete(normalizedUrl);
            return null;
        }
        return cached;
    }

    /**
     * Fetch trang từ server và parse.
     * @param {string} url
     * @returns {Promise<{html, scripts, styleHrefs, title}|null>}
     */
    async _fetchPage(url, signal) {
        try {
            const resp = await fetch(url, {
                headers: {
                    'X-Requested-With': 'spa-fetch',
                    'X-SPA-Nav': '1'
                },
                credentials: 'same-origin',
                signal: signal
            });

            if (!resp.ok) return null;

            const fullHtml = await resp.text();
            return this._parsePage(fullHtml);
        } catch (e) {
            console.error('[SPA Router] Fetch failed:', e);
            return null;
        }
    }

    /**
     * Parse full HTML string → trích xuất main content, scripts, styles.
     * @param {string} htmlText
     */
    _parsePage(htmlText) {
        const parser = new DOMParser();
        const doc = parser.parseFromString(htmlText, 'text/html');

        // Lấy <main> content
        const mainEl = doc.querySelector('main[role="main"]');
        if (!mainEl) return null;
        const html = mainEl.innerHTML;

        // Lấy title trang
        const title = doc.title || document.title;

        // Lấy script src đặc thù của trang (không phải layout scripts)
        const scripts = [];
        const layoutScriptKeywords = [
            'jquery', 'bootstrap', 'apiClient', 'layout-model', 'layout-view',
            'layout-controller', 'admin-dashboard', 'chart.js', 'admin-spa-router'
        ];

        doc.querySelectorAll('script').forEach(s => {
            const src = s.getAttribute('src');
            if (src) {
                // Chỉ lấy scripts không phải layout
                const isLayout = layoutScriptKeywords.some(k => src.includes(k));
                if (!isLayout) {
                    scripts.push({ type: 'src', value: src });
                }
            } else if (s.textContent && s.textContent.trim()) {
                // Inline scripts (init code như new OrdersController...)
                const txt = s.textContent.trim();
                // Bỏ qua script rỗng hoặc script layout
                if (txt.length > 10) {
                    scripts.push({ type: 'inline', value: txt });
                }
            }
        });

        // Lấy các CSS đặc thù của trang (link stylesheet)
        const layoutStyleKeywords = ['bootstrap', 'site.css', 'admin-layout', 'dashboard'];
        const styleHrefs = [];
        doc.querySelectorAll('link[rel="stylesheet"]').forEach(l => {
            const href = l.getAttribute('href') || '';
            const isLayout = layoutStyleKeywords.some(k => href.includes(k));
            if (!isLayout && href) {
                styleHrefs.push(href);
            }
        });

        // Lấy inline <style> blocks từ <head> của trang mới (ví dụ Vouchers.cshtml)
        const inlineStyles = [];
        doc.querySelectorAll('head style').forEach(s => {
            const cssText = s.textContent.trim();
            // Bỏ qua các style quá ngắn (likely từ layout)
            if (cssText.length > 50) {
                inlineStyles.push(cssText);
            }
        });

        return { html, scripts, styleHrefs, inlineStyles, title };
    }

    /**
     * Áp dụng page data: swap HTML, load CSS, execute scripts, update URL.
     */
    async _applyPage(pageData, url, pushState) {
        const { html, scripts, styleHrefs, inlineStyles = [], title } = pageData;

        // --- Swap main content ---
        const main = document.querySelector('main[role="main"]');
        if (!main) return;

        // Transition out
        main.style.opacity = '0';
        main.style.transform = 'translateY(8px)';
        main.style.transition = 'opacity 0.15s ease, transform 0.15s ease';

        await this._delay(80);

        // Inject HTML
        main.innerHTML = html;

        // --- Load page CSS link nếu chưa có ---
        for (const href of styleHrefs) {
            this._ensureStylesheet(href);
        }

        // --- Inject inline <style> blocks đặc thù của page (e.g. Messages, Vouchers) ---
        // Dùng data-spa-page attribute để tránh duplicate
        // Gộp TẤT CẢ inline style blocks thành một <style> element duy nhất
        const normalizedUrl = this._normalizeUrl(url);
        document.querySelectorAll(`style[data-spa-page]`).forEach(s => {
            if (s.getAttribute('data-spa-page') !== normalizedUrl) {
                s.remove();
            }
        });
        if (inlineStyles.length > 0) {
            const existing = document.querySelector(`style[data-spa-page="${normalizedUrl}"]`);
            if (!existing) {
                const styleEl = document.createElement('style');
                styleEl.setAttribute('data-spa-page', normalizedUrl);
                // Gộp tất cả blocks lại — tránh bỏ sót style (lỗi cũ chỉ inject block đầu tiên)
                styleEl.textContent = inlineStyles.join('\n');
                document.head.appendChild(styleEl);
            }
        }


        // --- Update browser URL + title ---
        if (pushState) {
            history.pushState({ url }, title, url);
        }
        document.title = title;

        // --- Update sidebar active state ---
        this._updateSidebarActive(url);

        // --- Scroll to top ---
        window.scrollTo({ top: 0, behavior: 'instant' });

        // --- Transition in ---
        main.style.opacity = '1';
        main.style.transform = 'translateY(0)';

        // Clear transform after transition completes to restore viewport fixed positioning for modals
        this._transitionId = (this._transitionId || 0) + 1;
        const currentTransitionId = this._transitionId;
        setTimeout(() => {
            if (this._transitionId === currentTransitionId) {
                main.style.transform = '';
            }
        }, 160);

        // Đợi một chút để DOM ổn định trước khi execute scripts
        await this._delay(30);

        // --- Execute page scripts ---
        await this._executeScripts(scripts);

        // --- Phát event spa:page-ready ---
        document.dispatchEvent(new CustomEvent(this.PAGE_READY_EVENT, {
            detail: { url, title }
        }));

        console.log(`[SPA Router] Page ready: ${url}`);
    }

    /**
     * Load và execute các scripts của trang mới.
     * @param {Array<{type: 'src'|'inline', value: string}>} scripts
     */
    async _executeScripts(scripts) {
        for (const script of scripts) {
            if (script.type === 'src') {
                const normalized = this._normalizeUrl(script.value);
                if (this._loadedScripts.has(normalized)) {
                    // Script đã load → chỉ cần execute nếu là module init
                    // (script src được load 1 lần, class đã có sẵn trong memory)
                    continue;
                }
                // Load script lần đầu
                await this._loadScript(script.value);
                this._loadedScripts.add(normalized);
            } else if (script.type === 'inline') {
                // Inline script (init code) → luôn execute
                this._executeInlineScript(script.value);
            }
        }
    }

    /**
     * Load một script file từ src, trả về Promise khi load xong.
     */
    _loadScript(src) {
        return new Promise((resolve, reject) => {
            const el = document.createElement('script');
            el.src = src;
            el.onload = () => resolve();
            el.onerror = (e) => {
                console.warn('[SPA Router] Failed to load script:', src);
                resolve(); // Không reject để không block navigation
            };
            document.head.appendChild(el);
        });
    }

    /**
     * Execute inline script trong context hiện tại.
     * Xử lý đặc biệt: thay thế DOMContentLoaded event với việc gọi trực tiếp.
     */
    _executeInlineScript(code) {
        try {
            // Thay thế các dạng document.addEventListener('DOMContentLoaded', ...) 
            // thành IIFE vì DOM đã ready khi SPA swap (hỗ trợ cả async và arrow functions)
            let transformedCode = code
                .replace(
                    /document\.addEventListener\s*\(\s*['"]DOMContentLoaded['"]\s*,\s*(async\s+)?(?:function\s*)?\(\s*\)\s*(?:=>\s*)?\{/g,
                    '($1function() {'
                )
                .replace(
                    /\}\s*\)\s*;?\s*$/, // Đóng ngoặc cho IIFE
                    '})();'
                );

            // Execute inline
            const el = document.createElement('script');
            el.textContent = transformedCode;
            document.body.appendChild(el);
            document.body.removeChild(el);
        } catch (err) {
            console.warn('[SPA Router] Inline script transform error, trying raw:', err.message);
            // Thử execute nguyên bản qua DOM script injection
            try {
                const el = document.createElement('script');
                el.textContent = code;
                document.body.appendChild(el);
                document.body.removeChild(el);
            } catch (e2) {
                console.error('[SPA Router] Script execute fallback failed:', e2);
            }
        }
    }

    /**
     * Thêm stylesheet vào <head> nếu chưa có.
     */
    _ensureStylesheet(href) {
        const existing = document.querySelector(`link[href="${href}"]`);
        if (existing) return;

        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        document.head.appendChild(link);
    }

    /**
     * Cập nhật class active trên sidebar menu.
     */
    _updateSidebarActive(url) {
        const normalized = this._normalizeUrl(url);
        const menuItems = document.querySelectorAll('.sidebar-menu li');
        menuItems.forEach(item => {
            const dataHref = item.getAttribute('data-href');
            if (dataHref) {
                const itemNorm = this._normalizeUrl(dataHref);
                if (normalized.includes(itemNorm) || itemNorm.includes(normalized)) {
                    item.classList.add('active');
                } else {
                    item.classList.remove('active');
                }
            }
        });
    }

    /**
     * Hiện loading indicator.
     */
    _showLoading() {
        const indicator = document.getElementById('spa-loading-bar');
        if (indicator) {
            indicator.classList.add('loading');
        }
    }

    /**
     * Ẩn loading indicator.
     */
    _hideLoading() {
        const indicator = document.getElementById('spa-loading-bar');
        if (indicator) {
            indicator.classList.remove('loading');
        }
    }

    /**
     * Normalize URL để dùng làm cache key.
     * Loại bỏ protocol, host, trailing slash.
     */
    _normalizeUrl(url) {
        try {
            // Nếu là absolute URL
            if (url.startsWith('http')) {
                const u = new URL(url);
                return u.pathname.replace(/\/$/, '').toLowerCase();
            }
        } catch (e) {}
        return url.replace(/\/$/, '').toLowerCase().split('?')[0];
    }

    /**
     * Xóa cache của một URL cụ thể (gọi sau khi admin thực hiện action).
     * @param {string} url
     */
    invalidate(url) {
        const normalized = this._normalizeUrl(url);
        this._cache.delete(normalized);
        console.log(`[SPA Router] Cache invalidated: ${normalized}`);
    }

    /**
     * Xóa toàn bộ cache.
     */
    invalidateAll() {
        this._cache.clear();
        console.log('[SPA Router] All cache cleared.');
    }

    /**
     * Helper: delay ms milliseconds.
     */
    _delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}

// Khởi tạo singleton toàn cục
window.spaRouter = new AdminSpaRouter();
