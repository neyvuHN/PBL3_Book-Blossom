(function (window, document, $) {
    if (!$) {
        console.error('blind-date-details.js requires jQuery.');
        return;
    }

    let currentBlindImageIndex = 0;
    let selectedBlindVouchers = new Set();

    $(document).ready(function () {
        initBlindDateDetailsFromHash();
        initBackButton();
        initGallery();
        initLightbox();
        initCollapsibleBox();
        initWishlist();
        initQuantity();
        initVouchers();
        initAddToCart();
        initBuyNow();
        resumePendingBlindAction();
    });

    // After login redirect, auto-resume the pending buy/cart action
    function resumePendingBlindAction() {
        const token = localStorage.getItem('accessToken');
        if (!token) return;

        const raw = sessionStorage.getItem('pendingBlindAction');
        if (!raw) return;

        let pending;
        try { pending = JSON.parse(raw); } catch { return; }
        sessionStorage.removeItem('pendingBlindAction');

        if (!pending || !pending.blindBookID) return;

        // Wait for the detail view to be fully rendered before triggering action
        const tryResume = (attempts) => {
            if (attempts <= 0) return;
            const detailsSection = $('#blind-date-details-section');

            if (detailsSection.is(':visible') && window.currentBlindBookData) {
                if (pending.action === 'addToCart') {
                    if (pending.qty) $('#input-blind-qty').val(pending.qty);
                    if (window.BookBlossomCart) {
                        window.BookBlossomCart.addToCart({ blindBookID: pending.blindBookID, qty: pending.qty || 1 })
                            .then(() => showToast('Sách Ẩn Danh đã được thêm vào giỏ hàng!', 'success'))
                            .catch(err => showToast(err.message || 'Thêm vào giỏ hàng thất bại.', 'error'));
                    }
                } else if (pending.action === 'buyNow') {
                    if (pending.qty) $('#input-blind-qty').val(pending.qty);
                    // Trigger buy now button click
                    setTimeout(() => { $('.btn-buy-blind').first().trigger('click'); }, 300);
                }
            } else {
                // Detail section not ready yet, wait and retry
                setTimeout(() => tryResume(attempts - 1), 400);
            }
        };

        // Start trying after a short delay for the page hash navigation to settle
        setTimeout(() => tryResume(10), 600);
    }

    function showBlindDateDetails(blindBookData, push = true) {
        if (!blindBookData) return;

        window.currentBlindBookData = blindBookData;

        $('#blind-date-section').hide();
        $('#blind-date-details-section').show();

        populateBlindDateDetails(blindBookData);
        resetBlindDateUi();
        
        renderBlindVouchers(); // Dynamic voucher rendering

        window.scrollTo(0, 0);

        if (push) {
            const hashKey = getBlindHashKey(blindBookData);

            history.pushState(
                {
                    view: 'blind-date-details',
                    blindBookData: blindBookData
                },
                '',
                `/BlindDate#blind-details-${encodeURIComponent(hashKey)}`
            );
        }
    }

    function populateBlindDateDetails(blindBookData) {
        $('#blind-detail-main-img').attr('src', blindBookData.imgSrc || '/images/BlindDateBook/BlindBook.jpg');

        $('#blind-clue-quote').text(blindBookData.quotes || 'The story begins with a secret...');
        $('#blind-detail-price').text(blindBookData.price || '120.000 VNĐ');

        const numPrice = parsePriceToVnd(blindBookData.price || '120.000 VNĐ');
        const numOriginal = Math.round(numPrice * 1.5);

        $('#blind-detail-original-price').text(formatVndDot(numOriginal));
        $('#blind-detail-discount').text('-33%');

        $('#blind-condition-desc').text('Condition: ' + (blindBookData.condition || 'Well Loved'));
        $('#blind-clue-goodreads').text('★ ' + (blindBookData.rating || '4.2') + ' / 5');
        $('#blind-clue-rating-range').text('Community Rating: ★ ' + (blindBookData.ratingRange || '4.0 - 4.2'));
        $('#blind-clue-year').text('Publication Year: ' + (blindBookData.year || '1994'));
        $('#blind-clue-category').text(blindBookData.category || 'Mystery & Thriller');
        $('#blind-clue-keywords').text(blindBookData.keywords || 'Intriguing, suspenseful, cozy mystery');

        renderHashtags(blindBookData.hashtags || '#Mystery #BlindDate');
        renderGalleryImages(blindBookData.allImages || blindBookData.imgSrc || '/images/BlindDateBook/BlindBook.jpg');

        const title = getBlindTitle();
        const activeHash = `/BlindDate#blind-details-${encodeURIComponent(getBlindHashKey(blindBookData))}`;
        const fullLink = window.location.origin + activeHash;

        const chatHref =
            `/Messages?title=${encodeURIComponent(title)}` +
            `&price=${encodeURIComponent(blindBookData.price || '120.000 VNĐ')}` +
            `&img=${encodeURIComponent(blindBookData.imgSrc || '/images/BlindDateBook/BlindBook.jpg')}` +
            `&link=${encodeURIComponent(fullLink)}`;

        $('.btn-chat-owner').attr('href', chatHref);
    }

    async function resetBlindDateUi() {
        $('#input-blind-qty').val(1);

        const $wishlist = $('#btn-toggle-blind-wishlist');

        const title = getBlindTitle();
        const category = $('#blind-clue-category').text().trim() || 'Mystery';
        const id = 'blind-' + getBlindHashKey({ title: title, category: category });
        
        let isInWishlist = false;
        if (window.BookBlossomWishlist) {
            const items = await window.BookBlossomWishlist.getWishlistItems();
            isInWishlist = items.some(item => String(item.id) === String(id) || String(item.blindBookID) === String(id));
        }

        if (isInWishlist) {
            $wishlist.find('i').removeClass('far').addClass('fas').css('color', '#ff4444');
            $wishlist.css({
                borderColor: '#ff4444',
                background: '#fff5f5'
            });
        } else {
            $wishlist.find('i').attr('class', 'far fa-heart').css('color', '');
            $wishlist.css({
                borderColor: '#ddd',
                background: '#fff'
            });
        }

        $('.blind-thumb-wrapper').removeClass('active').css('border-color', 'transparent');
        $('.blind-thumb-wrapper').first().addClass('active').css('border-color', '#a291b5');

        currentBlindImageIndex = 0;
    }

    function renderHashtags(rawHashtags) {
        const $container = $('#blind-clue-hashtags');

        $container.empty();

        const tags = rawHashtags
            .split(/\s+/)
            .map(t => t.trim())
            .filter(Boolean);

        tags.forEach(function (tag) {
            const badge = $('<span></span>')
                .text(tag)
                .css({
                    background: '#fff',
                    color: '#6a4f8c',
                    border: '1px solid #a291b5',
                    padding: '4px 12px',
                    borderRadius: '20px',
                    fontSize: '0.8rem',
                    fontWeight: '600',
                    display: 'inline-block'
                });

            $container.append(badge);
        });
    }

    function renderGalleryImages(imagesInput) {
        let blindImagesArray;

        if (Array.isArray(imagesInput) && imagesInput.length > 0) {
            // Real images from database — use directly (up to 3 thumbnails)
            blindImagesArray = imagesInput.slice(0, 3);
        } else {
            // Fallback: single string passed → pad with demo images
            const mainImage = typeof imagesInput === 'string' ? imagesInput : '/images/BlindDateBook/BlindBook.jpg';
            const demoBlindImages = [
                '/images/BlindDateBook/BlindBook1.jpg',
                '/images/BlindDateBook/BlindBook2.jpg',
                '/images/BlindDateBook/BlindBook3.jpg',
                '/images/BlindDateBook/BlindBook4.jpg',
                '/images/BlindDateBook/BlindBook5.jpg',
                '/images/BlindDateBook/BlindBook.jpg'
            ];
            const otherImages = demoBlindImages.filter(img => img !== mainImage);
            blindImagesArray = [mainImage, ...otherImages.slice(0, 2)];
        }

        $('.blind-thumb-wrapper img').each(function (index) {
            if (index < blindImagesArray.length) {
                $(this).closest('.blind-thumb-wrapper').show();
                $(this).attr('src', blindImagesArray[index]);
            } else {
                $(this).closest('.blind-thumb-wrapper').hide();
            }
        });

        $('#blind-detail-main-img').attr('src', blindImagesArray[0]);
    }

    function getMappedBlindBookData(book) {
        const bookId = book.blindBookID || book.blindBookId || book.id || 0;
        const imgId = (bookId % 5) + 1;
        const fallbackImg = `/images/BlindDateBook/BlindBook${imgId}.jpg`;

        // Prefer real images from DB (imagePaths field from API)
        const realImages = (book.imagePaths && book.imagePaths.length > 0) ? book.imagePaths : null;
        const imgSrc = realImages ? realImages[0] : fallbackImg;
        const allImages = realImages || [fallbackImg];

        const hashtags = book.hashtags || book.Hashtags || '#Mystery #BlindDate';
        const quotes = book.quotes || book.Quotes || 'An intriguing mystery waiting to be solved...';
        const priceNum = book.price !== undefined ? book.price : (book.Price || 0);
        const price = Number(priceNum).toLocaleString('vi-VN') + ' VNĐ';

        const conditionsList = ['Pristine - Like new', 'Gift-ready', 'Well Loved - Has character', 'Gently read'];
        const condition = conditionsList[bookId % conditionsList.length];

        const rating = '4.2';
        const ratingRange = '4.0 - 4.2';
        const year = '1994';
        const category = book.category || book.Category || 'Mystery & Thriller';
        const keywords = book.keywords || book.Keywords || 'Suspenseful, intriguing, dark secrets';

        return {
            blindBookID: bookId,
            imgSrc: imgSrc,
            allImages: allImages,
            hashtags: hashtags,
            quotes: quotes,
            price: price,
            condition: condition,
            rating: rating,
            ratingRange: ratingRange,
            year: year,
            category: category,
            keywords: keywords
        };
    }

    async function initBlindDateDetailsFromHash() {
        const hash = window.location.hash || '';

        if (!hash.startsWith('#blind-details-')) return;

        const tagKey = decodeURIComponent(hash.substring('#blind-details-'.length));
        const parsedId = parseInt(tagKey, 10);

        if (!isNaN(parsedId) && parsedId > 0) {
            try {
                const response = await fetch(`/api/blindbook/${parsedId}`);
                if (response.ok) {
                    const realBookData = await response.json();
                    const mappedData = getMappedBlindBookData(realBookData);
                    showBlindDateDetails(mappedData, false);
                    history.replaceState(
                        {
                            view: 'blind-date-details',
                            blindBookData: mappedData
                        },
                        '',
                        `/BlindDate#blind-details-${parsedId}`
                    );
                    return;
                }
            } catch (err) {
                console.error("Error fetching real blind book details from hash:", err);
            }
        }

        const mockData = buildMockBlindBookFromHash(tagKey);

        showBlindDateDetails(mockData, false);

        history.replaceState(
            {
                view: 'blind-date-details',
                blindBookData: mockData
            },
            '',
            `/BlindDate#blind-details-${encodeURIComponent(tagKey)}`
        );
    }

    function buildMockBlindBookFromHash(tagKey) {
        let hashtags = '#SlowBurn #EnemiesToLovers #Paris';
        let desc = 'A baker and a grumpy food critic clash in the City of Light...';
        let imgSrc = '/images/BlindDateBook/BlindBook1.jpg';
        let price = '120.000 VNĐ';
        let condition = 'Well Loved - Has character';

        const parsedId = parseInt(tagKey, 10);
        const blindBookID = isNaN(parsedId) ? null : parsedId;

        if (tagKey.toLowerCase().includes('space') || tagKey.toLowerCase().includes('ai')) {
            hashtags = '#SpaceOpera #AI #FirstContact';
            desc = 'A pilot discovers an ancient alien artifact on a distant moon...';
            imgSrc = '/images/BlindDateBook/BlindBook3.jpg';
            price = '150.000 VNĐ';
            condition = 'Gift-ready';
        } else if (tagKey.toLowerCase().includes('literary') || tagKey.toLowerCase().includes('family')) {
            hashtags = '#Literary #FamilySaga #ComingOfAge';
            desc = 'Three generations of women navigate secrets in a small town...';
            imgSrc = '/images/BlindDateBook/BlindBook2.jpg';
            price = '100.000 VNĐ';
            condition = 'Well Loved';
        }

        return {
            blindBookID: blindBookID,
            imgSrc,
            hashtags,
            desc,
            price,
            condition,
            quotes: tagKey.toLowerCase().includes('space')
                ? 'The stars did not welcome us; they watched us in silence.'
                : tagKey.toLowerCase().includes('literary')
                    ? "Grandmother's cedar chest smelled of lavender and unspoken truths."
                    : 'The dough was cold, but their glances were fiery.',
            rating: tagKey.toLowerCase().includes('space') ? '4.6' : '4.3',
            ratingRange: tagKey.toLowerCase().includes('space') ? '4.4 - 4.7' : '4.1 - 4.4',
            year: tagKey.toLowerCase().includes('space') ? '2021' : '2018',
            category: tagKey.toLowerCase().includes('space') ? 'Science Fiction' : 'Romantic Fiction',
            keywords: tagKey.toLowerCase().includes('space')
                ? 'Deep space expedition, alien technology, emotional AI companion, space opera epic'
                : 'Sweet, slow-burn romance, culinary baking, enemies-to-lovers, Parisian vibe'
        };
    }

    function initBackButton() {
        $('#btn-back-to-blind').off('click.blindDetails').on('click.blindDetails', function (e) {
            e.preventDefault();

            $('#blind-date-details-section').hide();
            $('#blind-date-section').show();

            history.replaceState({ view: 'blind-date' }, '', '/BlindDate');

            window.scrollTo(0, 0);
        });

        window.addEventListener('popstate', async function () {
            const hash = window.location.hash || '';

            if (hash.startsWith('#blind-details-')) {
                const tagKey = decodeURIComponent(hash.substring('#blind-details-'.length));
                const parsedId = parseInt(tagKey, 10);
                if (!isNaN(parsedId) && parsedId > 0) {
                    try {
                        const response = await fetch(`/api/blindbook/${parsedId}`);
                        if (response.ok) {
                            const realBookData = await response.json();
                            const mappedData = getMappedBlindBookData(realBookData);
                            showBlindDateDetails(mappedData, false);
                            return;
                        }
                    } catch (err) {
                        console.error("Error fetching real blind book in popstate:", err);
                    }
                }
                showBlindDateDetails(buildMockBlindBookFromHash(tagKey), false);
            } else {
                $('#blind-date-details-section').hide();
                $('#blind-date-section').show();
            }
        });
    }

    function initGallery() {
        $(document)
            .off('click.blindThumb')
            .on('click.blindThumb', '.blind-thumb-wrapper', function () {
                const imgSrc = $(this).find('img').attr('src');

                $('#blind-detail-main-img').attr('src', imgSrc);

                const $thumbs = $('.blind-thumb-wrapper:visible');

                currentBlindImageIndex = $thumbs.index(this);

                $thumbs.removeClass('active').css('border-color', 'transparent');
                $(this).addClass('active').css('border-color', '#a291b5');
            });

        $(document)
            .off('click.blindGalleryPrev')
            .on('click.blindGalleryPrev', '.blind-gallery-prev', function (e) {
                e.stopPropagation();
                moveGallery(-1);
            });

        $(document)
            .off('click.blindGalleryNext')
            .on('click.blindGalleryNext', '.blind-gallery-next', function (e) {
                e.stopPropagation();
                moveGallery(1);
            });
    }

    function moveGallery(direction) {
        const $thumbs = $('.blind-thumb-wrapper:visible');

        if (!$thumbs.length) return;

        let targetIndex = currentBlindImageIndex + direction;

        if (targetIndex < 0) targetIndex = $thumbs.length - 1;
        if (targetIndex >= $thumbs.length) targetIndex = 0;

        $thumbs.eq(targetIndex).trigger('click');
    }

    function initLightbox() {
        $(document)
            .off('click.openBlindLightbox')
            .on('click.openBlindLightbox', '#blind-detail-main-img', function () {
                openBlindLightbox();
            });

        $(document)
            .off('click.closeBlindLightbox')
            .on('click.closeBlindLightbox', '.blind-lightbox-close, #blind-lightbox-modal .modal-backdrop', function () {
                closeBlindLightbox();
            });

        $(document)
            .off('click.blindLightboxWrapper')
            .on('click.blindLightboxWrapper', '#blind-lightbox-wrapper', function (e) {
                if ($(e.target).closest('#blind-lightbox-main-img, .lightbox-arrow, .lightbox-toolbar, button').length === 0) {
                    closeBlindLightbox();
                }
            });

        $(document)
            .off('click.blindLightboxPrev')
            .on('click.blindLightboxPrev', '.blind-lightbox-prev', function (e) {
                e.stopPropagation();
                updateBlindLightboxImage(currentBlindImageIndex - 1);
            });

        $(document)
            .off('click.blindLightboxNext')
            .on('click.blindLightboxNext', '.blind-lightbox-next', function (e) {
                e.stopPropagation();
                updateBlindLightboxImage(currentBlindImageIndex + 1);
            });

        $(document)
            .off('keydown.blindLightbox')
            .on('keydown.blindLightbox', function (e) {
                if (!$('#blind-lightbox-modal').is(':visible')) return;

                if (e.key === 'Escape') {
                    closeBlindLightbox();
                } else if (e.key === 'ArrowLeft') {
                    updateBlindLightboxImage(currentBlindImageIndex - 1);
                } else if (e.key === 'ArrowRight') {
                    updateBlindLightboxImage(currentBlindImageIndex + 1);
                }
            });
    }

    function openBlindLightbox() {
        if (!$('#blind-lightbox-modal').length) return;

        $('#blind-lightbox-modal').fadeIn(300);
        updateBlindLightboxImage(currentBlindImageIndex);
        $('body').css('overflow', 'hidden');
    }

    function closeBlindLightbox() {
        $('#blind-lightbox-modal').fadeOut(250, function () {
            $('body').css('overflow', '');
        });
    }

    function updateBlindLightboxImage(index) {
        const $thumbs = $('.blind-thumb-wrapper:visible');

        if (!$thumbs.length) return;

        if (index < 0) index = $thumbs.length - 1;
        if (index >= $thumbs.length) index = 0;

        currentBlindImageIndex = index;

        const imgSrc = $thumbs.eq(index).find('img').attr('src');

        $('#blind-lightbox-current-idx').text(index + 1);
        $('#blind-lightbox-total-idx').text($thumbs.length);
        $('#blind-lightbox-main-img').attr('src', imgSrc);

        $thumbs.removeClass('active').css('border-color', 'transparent');
        $thumbs.eq(index).addClass('active').css('border-color', '#a291b5');

        $('#blind-detail-main-img').attr('src', imgSrc);
    }

    function initCollapsibleBox() {
        $(document)
            .off('click.blindCollapsible')
            .on('click.blindCollapsible', '.how-this-works-box .collapsible-header', function () {
                const $content = $(this).next('.collapsible-content');
                const $icon = $(this).find('i').last();

                if ($content.is(':visible')) {
                    $content.slideUp(200);
                    $icon.removeClass('fa-chevron-up').addClass('fa-chevron-down');
                } else {
                    $content.slideDown(200);
                    $icon.removeClass('fa-chevron-down').addClass('fa-chevron-up');
                }
            });
    }

    function initWishlist() {
        $('#btn-toggle-blind-wishlist').off('click.blindDetails').on('click.blindDetails', async function () {
            const $btn = $(this);
            const $icon = $btn.find('i');
            
            const title = getBlindTitle();
            const id = 'blind-' + getBlindHashKey({ title: title, category: $('#blind-clue-category').text().trim() });
            const author = 'Unknown';
            const priceText = $('#blind-detail-price').text() || '0 VNĐ';
            const priceVnd = parsePriceToVnd(priceText);
            const price = priceVnd;
            const img = $('#blind-detail-main-img').attr('src') || '/images/BlindDateBook/BlindBook.jpg';
            const hashtags = getCurrentHashtags();

            if ($icon.hasClass('far')) {
                if (window.BookBlossomWishlist) {
                    try {
                        await window.BookBlossomWishlist.addToWishlist({
                            id: id,
                            title: title,
                            author: author,
                            price: price,
                            imageUrl: img,
                            isBlindDate: true,
                            hashtags: hashtags
                        });
                        $icon.removeClass('far').addClass('fas').css('color', '#ff4444');
                        $btn.css({
                            borderColor: '#ff4444',
                            background: '#fff5f5'
                        });
                        showToast('Mystery Book added to your wishlist!');
                    } catch (e) {
                        showToast('Failed to add book to wishlist.', 'error');
                    }
                }
            } else {
                if (window.BookBlossomWishlist) {
                    try {
                        await window.BookBlossomWishlist.removeItem(id);
                        $icon.removeClass('fas').addClass('far').css('color', '');
                        $btn.css({
                            borderColor: '#ddd',
                            background: '#fff'
                        });
                        showToast('Mystery Book removed from your wishlist.');
                    } catch (e) {
                        showToast('Failed to remove book from wishlist.', 'error');
                    }
                }
            }
        });
    }

    function initQuantity() {
        $('#btn-blind-qty-plus').off('click.blindDetails').on('click.blindDetails', function () {
            const $input = $('#input-blind-qty');
            let val = parseInt($input.val(), 10) || 1;

            if (val < 99) {
                $input.val(val + 1);
            }
        });

        $('#btn-blind-qty-minus').off('click.blindDetails').on('click.blindDetails', function () {
            const $input = $('#input-blind-qty');
            let val = parseInt($input.val(), 10) || 1;

            if (val > 1) {
                $input.val(val - 1);
            }
        });

        $('#input-blind-qty').off('input.blindDetails').on('input.blindDetails', function () {
            let val = parseInt($(this).val(), 10);

            if (isNaN(val) || val < 1) {
                $(this).val(1);
            } else if (val > 99) {
                $(this).val(99);
            }
        });
    }

    async function renderBlindVouchers() {
        const $container = $('.blind-coupons-container');
        $container.empty();

        if (!window.VOUCHERS_DATA) {
            return; // Vouchers haven't loaded yet
        }

        let currentCategoryId = null;
        if (window.currentBlindBookData && window.currentBlindBookData.category) {
            const catName = window.currentBlindBookData.category.toLowerCase();
            try {
                if (!window.cachedCategories) {
                    const response = await fetch('/api/category?status=Active');
                    if (response.ok) {
                        window.cachedCategories = await response.json();
                    }
                }
                if (window.cachedCategories) {
                    const catObj = window.cachedCategories.find(c => c.categoryName.toLowerCase() === catName);
                    if (catObj) currentCategoryId = catObj.categoryId;
                }
            } catch (e) {
                console.error("Failed to fetch categories", e);
            }
        }

        let applicableVouchers = window.VOUCHERS_DATA.filter(v => {
            const isForAllBooks = !v.applicableBooks || v.applicableBooks.length === 0;
            if (!isForAllBooks) return false; 
            
            const isForAllCategories = !v.applicableCategories || v.applicableCategories.length === 0;
            const isForCategory = currentCategoryId && v.applicableCategories && v.applicableCategories.includes(currentCategoryId);

            return isForAllCategories || isForCategory;
        });

        if (applicableVouchers.length === 0) {
            $container.append('<p style="padding: 15px; color: #888;">No mystery vouchers available for this book.</p>');
        } else {
            applicableVouchers.forEach(v => {
                if (window.GlobalVouchers && window.GlobalVouchers.getBlindVoucherCardHtml) {
                    const html = window.GlobalVouchers.getBlindVoucherCardHtml(v);
                    $container.append(html);
                }
            });
        }
        
        renderBlindModalVouchers(applicableVouchers);
        syncBlindVoucherUI();
    }

    function renderBlindModalVouchers(vouchers) {
        const $modalContainer = $('#blind-vouchers-container');
        $modalContainer.empty();
        
        if (vouchers.length === 0) {
            $modalContainer.append('<p style="text-align:center; color:#888; margin-top:20px;">No mystery vouchers available for this book.</p>');
            return;
        }
        
        vouchers.forEach(v => {
            if (window.GlobalVouchers && window.GlobalVouchers.getBlindVoucherHtml) {
                const html = window.GlobalVouchers.getBlindVoucherHtml(v);
                $modalContainer.append(html);
            }
        });
    }

    function initVouchers() {
        ensureBlindVoucherCheckmarks();
        syncBlindVoucherUI();

        // Click voucher ở khung Available Vouchers
        $(document)
            .off('click.blindCoupon')
            .on('click.blindCoupon', '.blind-coupon-card', function (e) {
                e.preventDefault();

                const code = $(this).attr('data-code');
                if (!code) return;

                toggleBlindVoucher(code);
            });

        // Mở popup Mystery Book Vouchers
        $(document)
            .off('click.seeAllBlindVouchers')
            .on('click.seeAllBlindVouchers', '#btn-see-all-blind-vouchers', function (e) {
                e.preventDefault();

                syncBlindVoucherUI();
                $('#blind-vouchers-modal').fadeIn(250);
            });

        // Click voucher trong popup
        $(document)
            .off('click.blindModalVoucher')
            .on('click.blindModalVoucher', '#blind-vouchers-modal .modal-blind-voucher-item', function (e) {
                e.preventDefault();

                const code = $(this).attr('data-code');
                if (!code) return;

                toggleBlindVoucher(code);
            });

        // Click nút Apply trong popup
        $(document)
            .off('click.blindModalVoucherButton')
            .on('click.blindModalVoucherButton', '#blind-vouchers-modal .btn-modal-apply-blind-voucher', function (e) {
                e.preventDefault();
                e.stopPropagation();

                const code = $(this).closest('.modal-blind-voucher-item').attr('data-code');
                if (!code) return;

                toggleBlindVoucher(code);
            });

        // Đóng popup
        $(document)
            .off('click.closeBlindVouchers')
            .on('click.closeBlindVouchers', '#btn-close-blind-vouchers, #blind-vouchers-modal .modal-backdrop', function () {
                $('#blind-vouchers-modal').fadeOut(250);
            });

        // Apply Selected trong popup
        $(document)
            .off('click.confirmBlindVouchers')
            .on('click.confirmBlindVouchers', '#btn-confirm-blind-vouchers', function () {
                $('#blind-vouchers-modal').fadeOut(250);

                if (selectedBlindVouchers.size > 0) {
                    showToast(`Successfully applied ${selectedBlindVouchers.size} mystery voucher(s)!`);
                } else {
                    showToast('No mystery voucher selected.');
                }

                syncBlindVoucherUI();
            });
    }

    function toggleBlindVoucher(code) {
        if (selectedBlindVouchers.has(code)) {
            selectedBlindVouchers.delete(code);
            showToast(`Mystery Voucher "${code}" removed.`);
        } else {
            const voucher = window.VOUCHERS_DATA ? window.VOUCHERS_DATA.find(v => v.code === code) : null;
            if (voucher && voucher.isStackable === false && selectedBlindVouchers.size > 0) {
                window.showVoucherError(`Voucher <b>${code}</b> cannot be used in conjunction with other vouchers.`);
                return;
            }

            let hasNonStackable = false;
            let nonStackableCode = '';
            selectedBlindVouchers.forEach(sc => {
                const sv = window.VOUCHERS_DATA ? window.VOUCHERS_DATA.find(v => v.code === sc) : null;
                if (sv && sv.isStackable === false) {
                    hasNonStackable = true;
                    nonStackableCode = sc;
                }
            });

            if (hasNonStackable) {
                window.showVoucherError(`Voucher <b>${nonStackableCode}</b> cannot be used in conjunction with other vouchers.`);
                return;
            }

            // Add checks for other conditions if window.currentUser is defined
            if (voucher && window.currentUser) {
                if (voucher.minPlan > (window.currentUser.plan || 0)) {
                    window.showVoucherError(`Voucher <b>${code}</b> requires a minimum subscription plan of <b>${window.getPlanName(voucher.minPlan)}</b>.`);
                    return;
                }
                if (voucher.minReputation > (window.currentUser.reputation || 0)) {
                    window.showVoucherError(`Voucher <b>${code}</b> requires a minimum reputation score of <b>${voucher.minReputation}</b>.`);
                    return;
                }
                if (voucher.minRank > (window.currentUser.rank || 0)) {
                    window.showVoucherError(`Voucher <b>${code}</b> requires a minimum membership rank of <b>${window.getRankName(voucher.minRank)}</b>.`);
                    return;
                }
            }

            selectedBlindVouchers.add(code);
            showToast(`Mystery Voucher "${code}" applied!`);
        }

        syncBlindVoucherUI();
    }

    function syncBlindVoucherUI() {
        // 1. Đồng bộ khung Available Vouchers
        $('.blind-coupon-card').each(function () {
            const $card = $(this);
            const code = $card.attr('data-code');
            const isSelected = selectedBlindVouchers.has(code);

            $card.toggleClass('selected', isSelected);

            if (isSelected) {
                $card.css({
                    border: '2px solid #6a4f8c',
                    background: '#faf8fc',
                    boxShadow: '0 6px 18px rgba(106, 79, 140, 0.16)'
                });
            } else {
                // [FIXED] Restored the beautiful dashed border and light purple background styling for unselected voucher cards
                $card.css({
                    border: '1.5px dashed #a291b5',
                    background: '#faf8fc',
                    boxShadow: '0 2px 6px rgba(0,0,0,0.01)'
                });
            }

            // [FIXED] Target both classes to support static HTML checkmark-badge-blind and dynamic checkmark-badge
            $card.find('.checkmark-badge, .checkmark-badge-blind').css('display', isSelected ? 'flex' : 'none');
        });

        // 2. Đồng bộ popup Mystery Book Vouchers
        $('#blind-vouchers-modal .modal-blind-voucher-item').each(function () {
            const $item = $(this);
            const code = $item.attr('data-code');
            const isSelected = selectedBlindVouchers.has(code);

            $item.toggleClass('selected', isSelected);

            if (isSelected) {
                $item.css({
                    border: '2px solid #6a4f8c',
                    background: '#f4effa',
                    boxShadow: '0 6px 18px rgba(106, 79, 140, 0.14)'
                });
                $item.find('.btn-modal-apply-blind-voucher').text('Applied').css({
                    background: '#6a4f8c',
                    color: '#fff'
                });
            } else {
                $item.css({
                    border: '1.5px dashed #a291b5',
                    background: '#faf8fc',
                    boxShadow: ''
                });
                $item.find('.btn-modal-apply-blind-voucher').text('Apply').css({
                    background: '#fff',
                    color: '#6a4f8c'
                });
            }
        });

        // 3. Đồng bộ số lượng ở footer popup
        $('#blind-modal-selected-count').text(selectedBlindVouchers.size);
    }

    function ensureBlindVoucherCheckmarks() {
        $('.blind-coupon-card').each(function () {
            const $card = $(this);

            if ($card.find('.checkmark-badge').length === 0) {
                $card.css('position', 'relative');

                $card.append(`
                    <div class="checkmark-badge" style="
                        display: none;
                        position: absolute;
                        top: -7px;
                        right: -7px;
                        width: 20px;
                        height: 20px;
                        border-radius: 50%;
                        background: #6a4f8c;
                        color: #fff;
                        align-items: center;
                        justify-content: center;
                        font-size: 0.7rem;
                        z-index: 5;
                        box-shadow: 0 2px 6px rgba(0,0,0,0.15);
                    ">
                        <i class="fas fa-check"></i>
                    </div>
                `);
            }
        });
    }

    function initAddToCart() {
        $(document)
            .off('click.addBlindCart')
            .on('click.addBlindCart', '.btn-cart-blind', async function (e) {
                e.preventDefault();

                // Yêu cầu đăng nhập nếu là khách vãng lai
                const token = localStorage.getItem('accessToken');
                if (!token) {
                    const blindId = window.currentBlindBookData
                        ? (window.currentBlindBookData.blindBookID || window.currentBlindBookData.blindBookId || window.currentBlindBookData.id)
                        : null;
                    sessionStorage.setItem('pendingBlindAction', JSON.stringify({
                        action: 'addToCart',
                        blindBookID: blindId,
                        qty: parseInt($('#input-blind-qty').val(), 10) || 1
                    }));
                    showToast('Vui lòng đăng nhập để thêm Sách Ẩn Danh vào giỏ hàng!', 'error', 'Yêu cầu đăng nhập');
                    setTimeout(() => {
                        window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent('/BlindDate#blind-details-' + blindId);
                    }, 1500);
                    return;
                }

                if (!window.BookBlossomCart) {
                    showToast('Cart is not ready.');
                    return;
                }

                const qty = parseInt($('#input-blind-qty').val(), 10) || 1;
                const blindId = window.currentBlindBookData 
                    ? (window.currentBlindBookData.blindBookID || window.currentBlindBookData.blindBookId || window.currentBlindBookData.id) 
                     : null;

                if (!blindId) {
                    showToast('Invalid mystery book selection.', 'error');
                    return;
                }

                try {
                    animateAddToCart($(this), qty);
                    showToast(`Added ${qty}x Mystery Book to your cart!`);

                    window.BookBlossomCart.addToCart({ blindBookID: blindId, qty: qty }).catch(err => {
                        console.error('Failed background add to cart', err);
                    });
                } catch (error) {
                    console.error('Failed to add to cart', error);
                    showToast('Failed to add item to cart.', 'error');
                }
            });
    }

    function initBuyNow() {
        $(document)
            .off('click.buyBlindNow')
            .on('click.buyBlindNow', '.btn-buy-blind', function (e) {
                e.preventDefault();

                // Yêu cầu đăng nhập nếu là khách vãng lai
                const token = localStorage.getItem('accessToken');
                if (!token) {
                    const blindId = window.currentBlindBookData
                        ? (window.currentBlindBookData.blindBookID || window.currentBlindBookData.blindBookId || window.currentBlindBookData.id)
                        : null;
                    const qty = parseInt($('#input-blind-qty').val(), 10) || 1;
                    sessionStorage.setItem('pendingBlindAction', JSON.stringify({
                        action: 'buyNow',
                        blindBookID: blindId,
                        qty: qty
                    }));
                    showToast('Vui lòng đăng nhập để mua Sách Ẩn Danh!', 'error', 'Yêu cầu đăng nhập');
                    setTimeout(() => {
                        window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent('/BlindDate#blind-details-' + blindId);
                    }, 1500);
                    return;
                }

                const item = buildCartItem();
                const subtotal = item.priceVnd * item.qty;
                const voucherResult = calculateBlindVoucherDiscount(subtotal);

                const blindId = window.currentBlindBookData 
                    ? (window.currentBlindBookData.blindBookID || window.currentBlindBookData.blindBookId || window.currentBlindBookData.id) 
                    : null;

                window.checkoutState = {
                    isCart: false,
                    isBuyNow: true,
                    isBlind: true,
                    subtotal: subtotal,
                    shippingFee: voucherResult.shippingFeeVnd,
                    discount: voucherResult.discountVnd,
                    buyNowItem: {
                        isBlind: true,
                        blindBookID: blindId,
                        qty: item.qty
                    },
                    orderNote: '',
                    appliedVouchers: voucherResult.appliedVoucherLines
                };

                renderCheckoutVoucherBadges(voucherResult.appliedVoucherLines);

                if (typeof window.populateCheckoutBookInfo === 'function') {
                    window.populateCheckoutBookInfo([item]);
                }

                if (typeof window.updateCheckoutTotals === 'function') {
                    window.updateCheckoutTotals();
                }

                if (typeof window.openCheckout === 'function') {
                    window.openCheckout();
                } else {
                    showToast('Checkout interface is not ready.');
                }
            });
    }

    function buildCartItem() {
        const qty = parseInt($('#input-blind-qty').val(), 10) || 1;
        const priceText = $('#blind-detail-price').text() || '0 VNĐ';
        const priceVnd = parsePriceToVnd(priceText);
        const img = $('#blind-detail-main-img').attr('src') || '/images/BlindDateBook/BlindBook.jpg';
        const title = getBlindTitle();
        const hashtags = getCurrentHashtags();
        const blindId = window.currentBlindBookData 
            ? (window.currentBlindBookData.blindBookID || window.currentBlindBookData.blindBookId || window.currentBlindBookData.id) 
            : null;

        return {
            id: 'cart-' + Date.now(),
            title: title,
            shop: 'Blind Date Books',
            price: priceVnd / 20000,
            priceVnd: priceVnd,
            qty: qty,
            condition: 'New Curated',
            img: img,
            selected: true,
            isBlind: true,
            blindBookID: blindId,
            hashtags: hashtags
        };
    }

    function getBlindTitle() {
        const category = $('#blind-clue-category').text().trim() || 'Mystery';
        return 'Blind Date Mystery - Vibe: ' + category;
    }

    function getCurrentHashtags() {
        const tags = [];

        $('#blind-clue-hashtags span').each(function () {
            const clean = $(this).text().replace('#', '').trim();

            if (clean) {
                tags.push(clean);
            }
        });

        if (tags.length === 0) {
            tags.push('Mystery', 'BlindDate');
        }

        return tags;
    }

    function calculateBlindVoucherDiscount(subtotalVnd) {
        let discountVnd = 0;
        let shippingFeeVnd = subtotalVnd > 0 ? 30000 : 0;
        const appliedVoucherLines = [];

        selectedBlindVouchers.forEach(function (code) {
            const voucher = (window.VOUCHERS_DATA || []).find(v => v.code === code);
            
            if (voucher) {
                if (subtotalVnd >= voucher.minOrder) {
                    let d = voucher.type === 'percent' ? (subtotalVnd * voucher.discount) : voucher.discount;
                    if (voucher.type === 'percent' && voucher.maxDiscount) {
                        d = Math.min(d, voucher.maxDiscount);
                    }
                    
                    if (voucher.type === 'freeship') {
                        shippingFeeVnd = 0;
                        appliedVoucherLines.push({ code: code, text: `${voucher.code} (Free Shipping)` });
                    } else {
                        discountVnd += d;
                        appliedVoucherLines.push({ code: code, text: `${voucher.code} (-${new Intl.NumberFormat('vi-VN').format(d)} VND)` });
                    }
                } else {
                    showToast(`${voucher.code} requires minimum order ${new Intl.NumberFormat('vi-VN').format(voucher.minOrder)} VND.`);
                }
            }
        });

        discountVnd = Math.min(discountVnd, subtotalVnd);

        return {
            discountVnd,
            shippingFeeVnd,
            appliedVoucherLines
        };
    }

    function renderCheckoutVoucherBadges(appliedVoucherLines) {
        const container = document.getElementById('checkout-applied-vouchers-container');
        const list = document.getElementById('checkout-vouchers-list');

        if (list) {
            list.innerHTML = '';
        }

        if (!container || !list || appliedVoucherLines.length === 0) {
            if (container) container.style.display = 'none';
            return;
        }

        container.style.display = 'block';

        appliedVoucherLines.forEach(function (voucher) {
            const badge = document.createElement('div');

            badge.style.cssText = `
                background: #faf8fc;
                border: 1px solid #a291b5;
                color: #6a4f8c;
                font-weight: 700;
                font-size: 0.8rem;
                padding: 4px 10px;
                border-radius: 6px;
                display: flex;
                align-items: center;
                gap: 5px;
            `;

            badge.innerHTML = `<i class="fas fa-ticket"></i> ${voucher.text}`;
            list.appendChild(badge);
        });
    }

    function animateAddToCart($sourceBtn, qty) {
        const $cartIcon = $('#nav-cart-btn');

        if (!$sourceBtn.length || !$cartIcon.length) {
            refreshCartBadge();
            return;
        }

        const rectBtn = $sourceBtn[0].getBoundingClientRect();
        const rectCart = $cartIcon[0].getBoundingClientRect();

        const startX = rectBtn.left + rectBtn.width / 2 - 13;
        const startY = rectBtn.top + rectBtn.height / 2 - 13;
        const endX = rectCart.left + rectCart.width / 2 - 13;
        const endY = rectCart.top + rectCart.height / 2 - 13;

        const $flyer = $('<div class="cart-flyer"></div>').text(qty);

        $flyer.css({
            top: startY,
            left: startX
        });

        $('body').append($flyer);

        void $flyer[0].offsetHeight;

        $flyer.css({
            transform: `translate(${endX - startX}px, ${endY - startY}px) scale(0.4)`,
            opacity: '0.2'
        });

        setTimeout(function () {
            $flyer.remove();
            refreshCartBadge();
        }, 1500);
    }

    function refreshCartBadge() {
        if (window.BookBlossomLayout) {
            window.BookBlossomLayout.refreshCartBadge();
        } else if (window.BookBlossomCart) {
            window.BookBlossomCart.updateBadge();
        }
    }

    function parsePriceToVnd(priceText) {
        return parseInt((priceText || '').replace(/[^0-9]/g, ''), 10) || 0;
    }

    function formatVndDot(value) {
        return new Intl.NumberFormat('vi-VN').format(Number(value) || 0).replace(/,/g, '.') + ' VNĐ';
    }

    function getBlindHashKey(blindBookData) {
        return blindBookData.blindBookID ? blindBookData.blindBookID.toString() : (blindBookData.hashtags || 'Mystery')
            .replace(/\s+/g, '')
            .replace(/#/g, '');
    }

    function showToast(message) {
        const $toast = $(`
            <div class="toast-notification">
                <i class="fas fa-check-circle" style="color: #28a745;"></i>
                <span>${message}</span>
                <div class="toast-progress"></div>
            </div>
        `);

        $('body').append($toast);
        $toast.fadeIn(300);

        setTimeout(function () {
            $toast.fadeOut(300, function () {
                $(this).remove();
            });
        }, 3000);
    }

    window.BookBlossomBlindDateDetails = {
        show: showBlindDateDetails,
        mapBookData: getMappedBlindBookData
    };
})(window, document, window.jQuery);