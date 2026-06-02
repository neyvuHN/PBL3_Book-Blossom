(function (window, document, $) {
    if (!$) {
        console.error('product-details.js requires jQuery.');
        return;
    }

    let detailImagesArray = [];
    let currentDetailImageIndex = 0;
    let selectedVouchers = new Set();

    const VOUCHERS_DATA = [
        {
            code: 'BLOSSOM70',
            title: 'Save 70k VND',
            desc: 'Min order 500k',
            discount: 70000,
            type: 'fixed',
            minOrder: 500000,
            textColor: '#f07c7c',
            tagBg: '#ffebee',
            borderColor: '#f07c7c',
            bg: '#fffdfb'
        },
        {
            code: 'READMORE30',
            title: 'Save 30k VND',
            desc: 'First book purchase',
            discount: 30000,
            type: 'fixed',
            minOrder: 0,
            textColor: '#f07c7c',
            tagBg: '#ffebee',
            borderColor: '#f07c7c',
            bg: '#fffdfb'
        },
        {
            code: 'ZALOPAY50',
            title: 'ZaloPay Extra 50k',
            desc: 'Pay via ZaloPay app',
            discount: 50000,
            type: 'fixed',
            minOrder: 0,
            textColor: '#2b6cb0',
            tagBg: '#eef7fc',
            borderColor: '#2b6cb0',
            bg: '#fdfcff'
        },
        {
            code: 'MOMO12',
            title: 'Momo: Get 12k',
            desc: 'Cashback on Momo',
            discount: 12000,
            type: 'fixed',
            minOrder: 0,
            textColor: '#d53f8c',
            tagBg: '#fdf2f8',
            borderColor: '#d53f8c',
            bg: '#fffafc'
        },
        {
            code: 'SUMMER20',
            title: '20% OFF Summer',
            desc: 'Max discount 50k',
            discount: 0.2,
            maxDiscount: 50000,
            type: 'percent',
            minOrder: 0,
            textColor: '#dd6b20',
            tagBg: '#feebc8',
            borderColor: '#dd6b20',
            bg: '#fffaf0'
        },
        {
            code: 'FREESHIP',
            title: 'Free Shipping',
            desc: 'Min order 150k',
            discount: 0,
            type: 'freeship',
            minOrder: 150000,
            textColor: '#319795',
            tagBg: '#e6fffa',
            borderColor: '#319795',
            bg: '#f0fdf4'
        }
    ];

    $(document).ready(function () {
        initFromHash();
        initBackButton();
        initImageGallery();
        initLightbox();
        initDetailTabs();
        initReviewInteractions();
        initWishlist();
        initQuantity();
        initAddToCart();
        initBuyNow();
        initVoucherSystem();
        initPreviewButton();
    });

    async function showProductById(bookId, push = true) {
        try {
            const bookData = await apiClient.apiGet(`/api/realbook/${bookId}`);
            if (bookData) {
                showProductDetails(bookData, push, true);
            }
        } catch (error) {
            console.error('Failed to load book details', error);
            showToast('Failed to load book details.');
        }
    }

    function showProductDetails(bookData, push = true, isRealData = false) {
        if (!bookData) return;

        $('#explore-section').hide();
        $('#product-details-section').show();

        populateProductDetails(bookData, isRealData);
        resetProductDetailsUi(bookData, isRealData);

        window.scrollTo(0, 0);

        if (push) {
            const idToPush = isRealData ? bookData.bookID : encodeURIComponent(bookData.title);
            history.pushState(
                {
                    view: 'product-details',
                    bookData: bookData,
                    isRealData: isRealData
                },
                '',
                `/Explore#book-details-${idToPush}`
            );
        }
    }

    function populateProductDetails(bookData, isRealData) {
        $('#detail-title').text(bookData.title);
        $('#breadcrumb-title').text(bookData.title);
        
        const authorDisplay = isRealData ? (bookData.publisher || 'Unknown') : (bookData.author || 'Unknown Author');
        $('#detail-author').text(authorDisplay);

        currentDetailImageIndex = 0;

        const demoImages = [
            '/images/Book/book1.jpg',
            '/images/Book/book2.webp',
            '/images/Book/book3.avif',
            '/images/Book/book4.jpg',
            '/images/Book/book5.jpg',
            '/images/Book/book6.webp'
        ];

        const mainImage = isRealData ? `/images/Book/book${(bookData.bookID % 6) + 1}.jpg` : (bookData.imgSrc || '/images/Book/book1.jpg');
        const otherImages = demoImages.filter(img => img !== mainImage);

        detailImagesArray = [mainImage, ...otherImages.slice(0, 3)];

        $('.thumb-item').each(function (index) {
            if (index < detailImagesArray.length) {
                $(this).show();
                $(this).find('img').attr('src', detailImagesArray[index]);
            } else {
                $(this).hide();
            }
        });

        $('#detail-main-img').attr('src', mainImage);

        $('.thumb-item').removeClass('active');
        $('.thumb-item').first().addClass('active');

        const hash = getStableHash(bookData.title);

        const baseRating = 4.2 + ((hash % 8) / 10);
        const ratingString = baseRating.toFixed(1);
        renderRatingStars(baseRating, ratingString);

        $('#detail-reviews-count').text(`... Reviews`);
        $('#detail-tab-rev-count').text(`...`);
        // Load real reviews in the background
        const bookId = bookData.bookID || bookData.id;
        if (bookId) {
            fetchProductReviews(bookId);
        }

        const soldCount = (hash % 1800) + 140;
        $('#detail-sold-count').html(`<i class="fas fa-shopping-bag"></i> ${soldCount.toLocaleString()} Sold`);

        if (isRealData) {
            $('#detail-price').text(bookData.price.toLocaleString('vi-VN') + ' VND');
            $('#detail-original-price').text(Math.round(bookData.price * 1.2).toLocaleString('vi-VN') + ' VND');
            $('#detail-discount').text(`-20%`);
            $('#breadcrumb-genre').text(bookData.categoryName || 'General');
            $('#spec-publisher').text(bookData.publisher || 'Unknown');
            $('#meta-publisher').text(bookData.publisher || 'Unknown');
            $('#meta-supplier').text(bookData.publisher || 'Unknown');
            $('#meta-author').text(authorDisplay);
            $('#spec-isbn').text(bookData.isbn || 'N/A');
            $('#detail-desc-text').text(bookData.description || 'No description available.');
            $('#btn-detail-add-cart').data('book-id', bookData.bookID);
            $('#btn-detail-add-cart').data('book-price', bookData.price);
            $('#btn-toggle-wishlist').data('book-id', bookData.bookID);
            if (bookData.sampleFilePath) {
                $('.btn-read-preview').show();
                $('.btn-read-preview').data('sample-url', bookData.sampleFilePath);
            } else {
                $('.btn-read-preview').hide();
            }
        } else {
            let discountPercent = (hash % 4) * 10;
            if (discountPercent === 0) discountPercent = 20;

            const numericPrice = 120000 + ((hash % 15) * 15000);
            const discountedPrice = Math.round(numericPrice * (100 - discountPercent) / 100);

            $('#detail-price').text(discountedPrice.toLocaleString('vi-VN') + ' VND');
            $('#detail-original-price').text(numericPrice.toLocaleString('vi-VN') + ' VND');
            $('#detail-discount').text(`-${discountPercent}%`);

            const categories = ['Fiction', 'Psychology', 'Self-Help', 'Economics', 'History'];
            const selectedGenre = categories[hash % categories.length];
            $('#breadcrumb-genre').text(selectedGenre);

            const publishers = ['Ace Books', 'NXB Trẻ', 'NXB Kim Đồng', 'Penguin Books', 'HarperCollins'];
            const selectedPublisher = publishers[hash % publishers.length];

            $('#spec-publisher').text(selectedPublisher);
            $('#meta-publisher').text(selectedPublisher);
            $('#meta-supplier').text(selectedPublisher);
            $('#meta-author').text(bookData.author || 'Unknown Author');

            const coverFormats = ['Paperback', 'Hardcover', 'Deluxe Edition', 'Leatherbound'];
            $('#meta-format').text(coverFormats[hash % coverFormats.length]);

            const isbnSeed = 9780000000000 + (hash * 1337);
            $('#spec-isbn').text(
                isbnSeed.toString().replace(/(\d{3})(\d{1})(\d{6})(\d{3})/, '$1-$2-$3-$4')
            );

            const sampleTexts = [
                'A timeless masterpiece exploring power, legacy, and human struggle. Richly characterized and highly praised by the BookBlossom community for its depth and emotional resonance.',
                'An insightful study of human nature, habits, and resilience. This volume offers highly practical guidance and inspiring case studies that will stay with you long after the final chapter.',
                "A gorgeous narrative full of wonder and emotional depth. It captures the essence of self-discovery and the beauty of life's unpredictable journeys. Excellent reading choice.",
                'A brilliant analytical overview of society, economics, and human behavior. Highly informative and detailed, it challenges conventional wisdom and provides fresh, modern perspectives.'
            ];
            $('#detail-desc-text').text(sampleTexts[hash % sampleTexts.length]);
        }

        const estDate = new Date();
        estDate.setDate(estDate.getDate() + 3);
        const dateFormatted = estDate.toLocaleDateString('en-US', {
            weekday: 'long',
            day: '2-digit',
            month: '2-digit'
        });
        $('#shipping-est-date').text(`Estimated delivery inside 2-3 business days (by ${dateFormatted})`);

        const activeBookHash = `/Explore#book-details-${isRealData ? bookData.bookID : encodeURIComponent(bookData.title)}`;
        const fullBookLink = window.location.origin + activeBookHash;
        const discountPriceText = $('#detail-price').text();

        const bookChatHref =
            `/Messages?title=${encodeURIComponent(bookData.title)}` +
            `&price=${encodeURIComponent(discountPriceText)}` +
            `&img=${encodeURIComponent(mainImage)}` +
            `&link=${encodeURIComponent(fullBookLink)}`;

        $('#btn-detail-chat').attr('href', bookChatHref);
    }

    async function resetProductDetailsUi(bookData, isRealData) {
        $('#input-qty').val(1);

        $('.detail-tab-header').removeClass('active');
        $('[data-tab="tab-desc"]').addClass('active');

        $('.detail-tab-pane').hide();
        $('#tab-desc').show();

        const $btn = $('#btn-toggle-wishlist');
        
        try {
            let items = [];
            if (window.BookBlossomWishlist) {
                items = await window.BookBlossomWishlist.getWishlistItems();
            }

            let isInWishlist = false;
            if (isRealData) {
                isInWishlist = items.some(item => item.bookID === bookData.bookID);
            } else {
                const title = $('#detail-title').text().trim();
                const id = 'book-' + getStableHash(title);
                isInWishlist = items.some(item => String(item.id) === String(id) || String(item.bookID) === String(id));
            }

            if (isInWishlist) {
                setWishlistActive($btn);
            } else {
                setWishlistInactive($btn);
            }
        } catch (err) {
            console.error("Failed to sync wishlist icon", err);
            setWishlistInactive($btn);
        }
    }

    function getStableHash(text) {
        let hash = 0;
        text = text || 'Book';

        for (let i = 0; i < text.length; i++) {
            hash = text.charCodeAt(i) + ((hash << 5) - hash);
        }

        return Math.abs(hash);
    }

    function renderRatingStars(baseRating, ratingString) {
        let starsHtml = '';
        const fullStars = Math.floor(baseRating);
        const hasHalf = (baseRating % 1) >= 0.4;

        for (let s = 1; s <= 5; s++) {
            if (s <= fullStars) {
                starsHtml += '<i class="fas fa-star"></i>';
            } else if (s === fullStars + 1 && hasHalf) {
                starsHtml += '<i class="fas fa-star-half-alt"></i>';
            } else {
                starsHtml += '<i class="far fa-star"></i>';
            }
        }

        starsHtml += `<strong id="detail-rating-num" style="color: #111; margin-left: 6px;">${ratingString}</strong>`;

        $('.rating-stars').html(starsHtml);
    }

    function initFromHash() {
        const initialHash = window.location.hash;
        let targetTab = null;
        let bookTitleDecoded = '';

        if (initialHash && initialHash.startsWith('#book-details-')) {
            let hashStr = initialHash.substring('#book-details-'.length);
            if (hashStr.includes('?tab=reviews')) {
                targetTab = 'tab-rev';
                hashStr = hashStr.replace('?tab=reviews', '');
            }
            let idStr = hashStr;
            let isNumericId = !isNaN(idStr) && idStr.trim() !== '';

            if (isNumericId) {
                showProductById(idStr, false);
            } else {
                bookTitleDecoded = decodeURIComponent(hashStr);
                const mockBook = {
                    title: bookTitleDecoded,
                    author: getMockAuthor(bookTitleDecoded),
                    imgSrc: '/images/Book/book1.jpg'
                };
                showProductDetails(mockBook, false);
            }
            
            if (targetTab === 'tab-rev') {
                setTimeout(() => {
                    $('[data-tab="tab-rev"]').trigger('click');
                }, 100);
            }

            if (!isNumericId) {
                history.replaceState(
                    {
                        view: 'product-details',
                        bookData: { title: bookTitleDecoded, author: getMockAuthor(bookTitleDecoded), imgSrc: '/images/Book/book1.jpg' },
                        isRealData: false
                    },
                    '',
                    `/Explore#book-details-${encodeURIComponent(bookTitleDecoded)}`
                );
            }
        } else {
            $('#explore-section').show();
            $('#product-details-section').hide();

            history.replaceState({ view: 'explore' }, '', '/Explore');
        }
    }

    function getMockAuthor(title) {
        const map = {
            'How to Win Friends': 'Dale Carnegie',
            'The Alchemist': 'Paulo Coelho',
            'Think and Grow Rich': 'Napoleon Hill',
            'The 7 Habits': 'Stephen R. Covey',
            'Atomic Habits': 'James Clear',
            'The Power of Now': 'Eckhart Tolle',
            'Zero to One': 'Peter Thiel',
            'Good to Great': 'Jim Collins'
        };

        return map[title] || 'BookBlossom Curator';
    }

    function initBackButton() {
        $('#btn-back-to-list').off('click.productDetails').on('click.productDetails', function (e) {
            e.preventDefault();

            $('#product-details-section').hide();
            $('#explore-section').show();

            history.replaceState({ view: 'explore' }, '', '/Explore');
            window.scrollTo(0, 0);
        });

        window.addEventListener('popstate', function () {
            const hash = window.location.hash;

            if (hash && hash.startsWith('#book-details-')) {
                const idOrTitle = decodeURIComponent(hash.substring('#book-details-'.length));

                if (!isNaN(idOrTitle) && idOrTitle.trim() !== '') {
                    showProductById(idOrTitle, false);
                } else {
                    showProductDetails({
                        title: idOrTitle,
                        author: getMockAuthor(idOrTitle),
                        imgSrc: '/images/Book/book1.jpg'
                    }, false, false);
                }
            } else {
                $('#product-details-section').hide();
                $('#explore-section').show();
            }
        });
    }

    function initImageGallery() {
        $(document)
            .off('click.productThumb')
            .on('click.productThumb', '.thumb-item', function () {
                const clickedIndex = $('.thumb-item').index(this);
                setActiveDetailImage(clickedIndex);
            });

        $(document)
            .off('click.productPrev')
            .on('click.productPrev', '.prev-arrow', function (e) {
                e.stopPropagation();

                let targetIndex = currentDetailImageIndex - 1;
                if (targetIndex < 0) targetIndex = detailImagesArray.length - 1;

                setActiveDetailImage(targetIndex);
            });

        $(document)
            .off('click.productNext')
            .on('click.productNext', '.next-arrow', function (e) {
                e.stopPropagation();

                let targetIndex = currentDetailImageIndex + 1;
                if (targetIndex >= detailImagesArray.length) targetIndex = 0;

                setActiveDetailImage(targetIndex);
            });
    }

    function setActiveDetailImage(index) {
        if (index < 0 || index >= detailImagesArray.length) return;

        currentDetailImageIndex = index;

        $('.thumb-item').removeClass('active');
        $('.thumb-item').eq(index).addClass('active');

        const clickedImgSrc = detailImagesArray[index];
        const $mainImg = $('#detail-main-img');

        $mainImg.css({
            transform: 'scale(0.95)',
            opacity: '0.7'
        });

        setTimeout(function () {
            $mainImg.attr('src', clickedImgSrc);
            $mainImg.css({
                transform: 'scale(1)',
                opacity: '1'
            });
        }, 150);
    }

    function initLightbox() {
        $(document)
            .off('click.openBookLightbox')
            .on('click.openBookLightbox', '#detail-main-img', function () {
                openLightbox(currentDetailImageIndex);
            });

        $(document)
            .off('click.closeBookLightbox')
            .on('click.closeBookLightbox', '.lightbox-close-btn, #book-lightbox-modal .modal-backdrop', function () {
                closeLightbox();
            });

        $(document)
            .off('click.bookLightboxPrev')
            .on('click.bookLightboxPrev', '.lightbox-prev', function (e) {
                e.stopPropagation();

                let targetIndex = currentDetailImageIndex - 1;
                if (targetIndex < 0) targetIndex = detailImagesArray.length - 1;

                updateLightboxImage(targetIndex);
            });

        $(document)
            .off('click.bookLightboxNext')
            .on('click.bookLightboxNext', '.lightbox-next', function (e) {
                e.stopPropagation();

                let targetIndex = currentDetailImageIndex + 1;
                if (targetIndex >= detailImagesArray.length) targetIndex = 0;

                updateLightboxImage(targetIndex);
            });

        $(document).off('keydown.bookLightbox').on('keydown.bookLightbox', function (e) {
            const $modal = $('#book-lightbox-modal');

            if (!$modal.is(':visible')) return;

            if (e.key === 'Escape') {
                closeLightbox();
            } else if (e.key === 'ArrowLeft') {
                $('.lightbox-prev').trigger('click');
            } else if (e.key === 'ArrowRight') {
                $('.lightbox-next').trigger('click');
            }
        });
    }

    function openLightbox(index) {
        if (index < 0 || index >= detailImagesArray.length) return;

        $('#book-lightbox-modal').fadeIn(300);
        updateLightboxImage(index);
        $('body').css('overflow', 'hidden');
    }

    function closeLightbox() {
        $('#book-lightbox-modal').fadeOut(250, function () {
            $('body').css('overflow', '');
        });
    }

    function updateLightboxImage(index) {
        if (index < 0 || index >= detailImagesArray.length) return;

        currentDetailImageIndex = index;

        $('#lightbox-current-idx').text(index + 1);
        $('#lightbox-total-idx').text(detailImagesArray.length);

        const imgSrc = detailImagesArray[index];

        $('#lightbox-main-img').attr('src', imgSrc);
        $('#detail-main-img').attr('src', imgSrc);

        $('.thumb-item').removeClass('active');
        $('.thumb-item').eq(index).addClass('active');
    }

    function initDetailTabs() {
        $('.detail-tab-header').off('click.productDetails').on('click.productDetails', function () {
            $('.detail-tab-header').removeClass('active');
            $(this).addClass('active');

            const targetTab = $(this).data('tab');

            $('.detail-tab-pane').hide();
            $('#' + targetTab).fadeIn(200);

            if (targetTab === 'tab-rev') {
                const bookId = $('#btn-detail-add-cart').data('book-id');
                if (bookId) {
                    fetchProductReviews(bookId);
                }
            }
        });

        $(document).off('click.detailReviewCount').on('click.detailReviewCount', '#detail-reviews-count', function (e) {
            e.preventDefault();

            $('[data-tab="tab-rev"]').trigger('click');

            const $tabBar = $('.detail-tab-header').parent();

            if ($tabBar.length > 0) {
                $tabBar[0].scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    }

    function initReviewInteractions() {
        $(document).off('click.likeDetailReview').on('click.likeDetailReview', '.btn-like-detail-review', async function () {
            const $btn = $(this);
            const $icon = $btn.find('i');
            const $count = $btn.find('.like-count');
            const $reviewItem = $btn.closest('.prod-review-item');
            const reviewId = $reviewItem.data('id');

            if (!reviewId) return;

            let count = parseInt($count.text()) || 0;

            try {
                const token = localStorage.getItem('jwtToken'); // Assuming standard token storage
                const headers = { 'Content-Type': 'application/json' };
                if (token) headers['Authorization'] = `Bearer ${token}`;
                else {
                    let guestId = localStorage.getItem('guestId');
                    if (!guestId) {
                        guestId = 'guest_' + Math.random().toString(36).substr(2, 9);
                        localStorage.setItem('guestId', guestId);
                    }
                    headers['X-Guest-Id'] = guestId;
                }

                const response = await fetch(`/api/Review/${reviewId}/like`, {
                    method: 'POST',
                    headers: headers
                });

                if (response.ok) {
                    if ($icon.hasClass('far')) {
                        $icon.removeClass('far').addClass('fas').css('color', '#C2185B');
                        count++;
                        $btn.css('color', '#C2185B');
                    } else {
                        $icon.removeClass('fas').addClass('far').css('color', '');
                        count--;
                        $btn.css('color', '');
                    }

                    $count.text(count);
                    $reviewItem.attr('data-likes', count);
                }
            } catch (err) {
                console.error("Like error", err);
            }
        });

        $(document).off('click.toggleDetailReply').on('click.toggleDetailReply', '.btn-toggle-detail-reply', function () {
            const $reviewItem = $(this).closest('.prod-review-item');
            $reviewItem.find('.detail-reply-section').slideToggle(300);
        });

        $(document).off('click.reportDetailReview').on('click.reportDetailReview', '.btn-report-detail-review', function (e) {
            e.preventDefault();
            $('#report-review-modal').fadeIn(200);
        });

        $('#btn-cancel-review-report, #report-review-modal .modal-backdrop')
            .off('click.reportDetailReview')
            .on('click.reportDetailReview', function () {
                $('#report-review-modal').fadeOut(200);
            });

        $(document)
            .off('change.reportReviewReason')
            .on('change.reportReviewReason', 'input[name="report-review-reason"]', function () {
                if ($(this).val() === 'other') {
                    $('.other-review-reason-container').slideDown(200);
                } else {
                    $('.other-review-reason-container').slideUp(200);
                }
            });

        $('#btn-submit-review-report')
            .off('click.reportDetailReview')
            .on('click.reportDetailReview', function () {
                const reason = $('input[name="report-review-reason"]:checked').val();

                if (!reason) {
                    alert('Please select a reason for reporting.');
                    return;
                }

                if (reason === 'other') {
                    const otherText = $('#other-review-reason-text').val().trim();

                    if (otherText === '') {
                        alert('Please enter your reason.');
                        return;
                    }
                }

                $('#report-review-modal').fadeOut(200);
                showToast('Thank you for reporting. We will review this review.');

                setTimeout(function () {
                    $('input[name="report-review-reason"]').prop('checked', false);
                    $('#other-review-reason-text').val('');
                    $('.other-review-reason-container').hide();
                }, 200);
            });
    }

    function initWishlist() {
        $('#btn-toggle-wishlist').off('click.productDetails').on('click.productDetails', async function () {
            const $btn = $(this);
            const $icon = $btn.find('i');
            const bookId = $btn.data('book-id');

            if (!bookId) {
                const title = $('#detail-title').text().trim();
                const author = $('#detail-author').text().trim() || 'Unknown Author';
                const priceText = $('#detail-price').text() || '0 VND';
                const unitPriceVnd = parseInt(priceText.replace(/[^0-9]/g, ''), 10) || 0;
                const price = unitPriceVnd;
                const img = $('#detail-main-img').attr('src') || '/images/Book/book1.jpg';
                const id = 'book-' + getStableHash(title);

                if ($icon.hasClass('far')) {
                    if (window.BookBlossomWishlist) {
                        try {
                            await window.BookBlossomWishlist.addToWishlist({
                                id: id,
                                title: title,
                                author: author,
                                price: price,
                                imageUrl: img,
                                isBlindDate: false
                            });
                            setWishlistActive($btn);
                            showToast('Book added to your wishlist!');
                        } catch (e) {
                            showToast('Failed to add book to wishlist.', 'error');
                        }
                    }
                } else {
                    if (window.BookBlossomWishlist) {
                        try {
                            await window.BookBlossomWishlist.removeItem(id);
                            setWishlistInactive($btn);
                            showToast('Book removed from your wishlist.');
                        } catch (e) {
                            showToast('Failed to remove book from wishlist.', 'error');
                        }
                    }
                }
            } else {
                if ($icon.hasClass('far')) {
                    if (window.BookBlossomWishlist) {
                        try {
                            await window.BookBlossomWishlist.addToWishlist({
                                id: bookId,
                                bookID: bookId,
                                isBlindDate: false
                            });
                            setWishlistActive($btn);
                            showToast('Book added to your wishlist!');
                        } catch (error) {
                            console.error('Failed to add to wishlist', error);
                            showToast('Failed to add book to wishlist.', 'error');
                        }
                    }
                } else {
                    if (window.BookBlossomWishlist) {
                        try {
                            await window.BookBlossomWishlist.removeItem(bookId);
                            setWishlistInactive($btn);
                            showToast('Book removed from your wishlist.');
                        } catch (error) {
                            console.error('Failed to remove from wishlist', error);
                            showToast('Failed to remove book from wishlist.', 'error');
                        }
                    }
                }
            }
        });
    }

    function setWishlistActive($btn) {
        $btn.find('i').removeClass('far').addClass('fas').css('color', '#ff4444');
        $btn.css({
            borderColor: '#ff4444',
            background: '#fff5f5'
        });
    }

    function setWishlistInactive($btn) {
        $btn.find('i').removeClass('fas').addClass('far').css('color', '');
        $btn.css({
            borderColor: '#ddd',
            background: '#fff'
        });
    }

    function initQuantity() {
        $('#btn-qty-plus').off('click.productDetails').on('click.productDetails', function () {
            const $input = $('#input-qty');
            let val = parseInt($input.val()) || 1;

            if (val < 99) {
                $input.val(val + 1);
            }
        });

        $('#btn-qty-minus').off('click.productDetails').on('click.productDetails', function () {
            const $input = $('#input-qty');
            let val = parseInt($input.val()) || 1;

            if (val > 1) {
                $input.val(val - 1);
            }
        });

        $('#input-qty').off('input.productDetails').on('input.productDetails', function () {
            let val = parseInt($(this).val());

            if (isNaN(val) || val < 1) {
                $(this).val(1);
            } else if (val > 99) {
                $(this).val(99);
            }
        });
    }

    function initAddToCart() {
        $('#btn-detail-add-cart').off('click.productDetails').on('click.productDetails', async function () {
            const $btn = $(this);
            const qty = parseInt($('#input-qty').val()) || 1;
            const bookId = $btn.data('book-id');

            if (!bookId) {
                const title = $('#detail-title').text().trim();
                const priceText = $('#detail-price').text() || '0 VND';
                const unitPriceVnd = parseInt(priceText.replace(/[^0-9]/g, ''), 10) || 0;
                const img = $('#detail-main-img').attr('src') || '/images/Book/book1.jpg';

                if (!window.BookBlossomCart) {
                    showToast('Cart is not ready.');
                    return;
                }

                // For mock books without a real ID, try to add to backend using a fallback ID
                const mockId = (getStableHash(title) % 10) + 1; // 1-10

                try {
                    await window.BookBlossomCart.addToCart({ bookID: mockId, qty: qty });
                    animateAddToCart($(this), qty);
                    showToast(`Added ${qty}x "${title}" to your cart!`);
                } catch(error) {
                    showToast('Failed to add item to cart.', 'error');
                }
                return;
            }

            try {
                // Optimistic UI updates to make it feel instant
                animateAddToCart($btn, qty);
                showToast(`Added ${qty}x item(s) to your cart!`);

                if (window.BookBlossomCart) {
                    window.BookBlossomCart.addToCart({ bookID: parseInt(bookId), qty: qty }).catch(err => {
                        console.error('Failed background add to cart', err);
                    });
                } else {
                    apiClient.apiPost('/api/Cart', { bookID: parseInt(bookId), quantity: qty }).catch(err => {
                        console.error('Failed background add to cart', err);
                    });
                }
            } catch (error) {
                console.error('Failed to add to cart', error);
                if (error.status === 401) {
                    showToast('Please log in to add items to cart.', 'error');
                } else {
                    showToast('Failed to add item to cart.', 'error');
                }
            }
        });
    }

    function animateAddToCart($sourceBtn, qty) {
        const $cartIcon = $('#nav-cart-btn');

        if (!$sourceBtn.length || !$cartIcon.length) {
            if (window.BookBlossomLayout) {
                window.BookBlossomLayout.refreshCartBadge();
            }
            return;
        }

        const rectBtn = $sourceBtn[0].getBoundingClientRect();
        const rectCart = $cartIcon[0].getBoundingClientRect();

        const startX = rectBtn.left + rectBtn.width / 2 - 13;
        const startY = rectBtn.top + rectBtn.height / 2 - 13;
        const endX = rectCart.left + rectCart.width / 2 - 13;
        const endY = rectCart.top + rectCart.height / 2 - 13;

        const deltaX = endX - startX;
        const deltaY = endY - startY;

        const $flyer = $('<div class="cart-flyer"></div>').text(qty);

        $flyer.css({
            top: startY,
            left: startX
        });

        $('body').append($flyer);

        void $flyer[0].offsetHeight;

        $flyer.css({
            transform: `translate(${deltaX}px, ${deltaY}px) scale(0.4)`,
            opacity: '0.2'
        });

        setTimeout(function () {
            $flyer.remove();

            if (window.BookBlossomLayout) {
                window.BookBlossomLayout.refreshCartBadge();
            } else if (window.BookBlossomCart) {
                window.BookBlossomCart.updateBadge();
            }
        }, 1500);
    }

    function initBuyNow() {
        $(document).off('click.productBuyNow').on('click.productBuyNow', '#btn-detail-buy', function (e) {
            e.preventDefault();

            const title = $('#detail-title').text().trim() || 'Selected Book';
            const author = $('#detail-author').text().trim() || 'Unknown Author';
            const img = $('#detail-main-img').attr('src') || '/images/Book/book1.jpg';
            const qty = parseInt($('#input-qty').val()) || 1;

            const priceText = $('#detail-price').text() || '0 VND';
            const unitPriceVnd = parseInt(priceText.replace(/[^0-9]/g, ''), 10) || 0;

            if (unitPriceVnd <= 0) {
                showToast('Cannot checkout this book because price is invalid.');
                return;
            }

            const subtotalVnd = unitPriceVnd * qty;
            const voucherResult = calculateBuyNowVoucherDiscount(subtotalVnd);

            const checkoutItem = {
                id: 'buy-now-' + Date.now(),
                title: title,
                shop: 'Normal Books',
                price: unitPriceVnd / 20000,
                priceVnd: unitPriceVnd,
                qty: qty,
                condition: 'Like New',
                img: img,
                selected: true,
                isBlind: false,
                author: author
            };

            // [UPDATED] checkoutState now includes orderNote (empty by default);
            // the Order Note textarea is reset automatically by window.openCheckout()
            window.checkoutState = {
                isCart: false,
                isBuyNow: true,
                isBlind: false,
                subtotal: subtotalVnd,
                shippingFee: voucherResult.shippingFeeVnd,
                discount: voucherResult.discountVnd,
                orderNote: '',
                buyNowItem: {
                    bookID: $('#btn-detail-add-cart').data('book-id'), // Can be undefined for mock
                    blindBookID: null,
                    qty: qty,
                    isBlind: false
                }
            };

            renderCheckoutVoucherBadges(voucherResult.appliedVoucherLines);

            if (typeof window.populateCheckoutBookInfo === 'function') {
                window.populateCheckoutBookInfo([checkoutItem]);
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

    function calculateBuyNowVoucherDiscount(subtotalVnd) {
        let discountVnd = 0;
        let shippingFeeVnd = subtotalVnd > 0 ? 60000 : 0;
        const appliedVoucherLines = [];

        selectedVouchers.forEach(function (code) {
            const voucher = VOUCHERS_DATA.find(v => v.code === code);

            if (!voucher) return;

            if (subtotalVnd < voucher.minOrder) {
                showToast(`${voucher.code} requires minimum order ${voucher.minOrder.toLocaleString('vi-VN')} VND.`);
                return;
            }

            if (voucher.type === 'fixed') {
                discountVnd += voucher.discount;
                appliedVoucherLines.push({
                    code: voucher.code,
                    text: `${voucher.code} (-${voucher.discount.toLocaleString('vi-VN')} VND)`
                });
            }

            if (voucher.type === 'percent') {
                const percentDiscount = Math.min(Math.round(subtotalVnd * voucher.discount), voucher.maxDiscount);
                discountVnd += percentDiscount;

                appliedVoucherLines.push({
                    code: voucher.code,
                    text: `${voucher.code} (-${percentDiscount.toLocaleString('vi-VN')} VND)`
                });
            }

            if (voucher.type === 'freeship') {
                shippingFeeVnd = 0;

                appliedVoucherLines.push({
                    code: voucher.code,
                    text: `${voucher.code} (Free Shipping)`
                });
            }
        });

        discountVnd = Math.min(discountVnd, subtotalVnd);

        return {
            discountVnd: discountVnd,
            shippingFeeVnd: shippingFeeVnd,
            appliedVoucherLines: appliedVoucherLines
        };
    }

    function renderCheckoutVoucherBadges(appliedVoucherLines) {
        const voucherContainer = document.getElementById('checkout-applied-vouchers-container');
        const voucherList = document.getElementById('checkout-vouchers-list');

        if (voucherList) {
            voucherList.innerHTML = '';
        }

        if (!voucherContainer || !voucherList || appliedVoucherLines.length === 0) {
            if (voucherContainer) voucherContainer.style.display = 'none';
            return;
        }

        voucherContainer.style.display = 'block';

        appliedVoucherLines.forEach(function (voucher) {
            const badge = document.createElement('div');

            badge.style.cssText = `
                background: #ffebee;
                border: 1px solid #f07c7c;
                color: #C2185B;
                font-weight: 700;
                font-size: 0.8rem;
                padding: 4px 10px;
                border-radius: 6px;
                display: flex;
                align-items: center;
                gap: 5px;
            `;

            badge.innerHTML = `<i class="fas fa-ticket"></i> ${voucher.text}`;
            voucherList.appendChild(badge);
        });
    }

    function initVoucherSystem() {
        renderMainPageVouchers();

        $(document).off('click.productCoupon').on('click.productCoupon', '.coupon-card', function () {
            const code = $(this).attr('data-code') || $(this).find('div').first().text().trim();

            if (selectedVouchers.has(code)) {
                selectedVouchers.delete(code);
                updateVoucherSync(code, false);
                showToast(`Voucher "${code}" removed.`);
            } else {
                selectedVouchers.add(code);
                updateVoucherSync(code, true);
                showToast(`Voucher "${code}" applied successfully!`);
            }
        });

        $(document)
            .off('click.productSeeAllVouchers')
            .on('click.productSeeAllVouchers', '.btn-see-all-vouchers', function (e) {
                e.preventDefault();

                $('.modal-voucher-item').each(function () {
                    const code = $(this).attr('data-code');
                    const isSelected = selectedVouchers.has(code);

                    if (isSelected) {
                        $(this).addClass('selected');
                        $(this).find('.btn-modal-apply-voucher').text('Applied');
                    } else {
                        $(this).removeClass('selected');
                        $(this).find('.btn-modal-apply-voucher').text('Apply');
                    }
                });

                $('#modal-selected-count').text(selectedVouchers.size);
                $('#vouchers-modal').fadeIn(250);
            });

        $(document)
            .off('click.productCloseVouchers')
            .on('click.productCloseVouchers', '#btn-close-vouchers, #btn-confirm-vouchers, #vouchers-modal .modal-backdrop', function () {
                $('#vouchers-modal').fadeOut(250);

                if ($(this).attr('id') === 'btn-confirm-vouchers' && selectedVouchers.size > 0) {
                    showToast(`Successfully applied ${selectedVouchers.size} voucher(s) to order!`);
                }
            });

        $(document)
            .off('click.productModalVoucher')
            .on('click.productModalVoucher', '.modal-voucher-item', function () {
                const code = $(this).attr('data-code');

                if (selectedVouchers.has(code)) {
                    selectedVouchers.delete(code);
                    updateVoucherSync(code, false);
                    showToast(`Voucher "${code}" removed.`);
                } else {
                    selectedVouchers.add(code);
                    updateVoucherSync(code, true);
                    showToast(`Voucher "${code}" applied!`);
                }
            });

        $(document)
            .off('click.productModalVoucherButton')
            .on('click.productModalVoucherButton', '.btn-modal-apply-voucher', function (e) {
                e.stopPropagation();
                $(this).closest('.modal-voucher-item').trigger('click');
            });
    }

    function renderMainPageVouchers() {
        const $slider = $('.coupon-slider');

        if (!$slider.length) return;

        $slider.empty();

        const sortedVouchers = [...VOUCHERS_DATA].sort((a, b) => {
            const aSelected = selectedVouchers.has(a.code) ? 1 : 0;
            const bSelected = selectedVouchers.has(b.code) ? 1 : 0;

            return bSelected - aSelected;
        });

        sortedVouchers.forEach(v => {
            const isSelected = selectedVouchers.has(v.code);
            const selectedClass = isSelected ? 'selected' : '';

            const borderStyle = isSelected
                ? `2px solid #C2185B`
                : `1.5px dashed ${v.borderColor}`;

            const cardHtml = `
                <div class="coupon-card ${selectedClass}" data-code="${v.code}"
                    style="flex: 0 0 190px; background: ${v.bg}; border: ${borderStyle}; border-radius: 10px; padding: 12px; position: relative; box-shadow: 0 2px 6px rgba(0,0,0,0.02); transition: all 0.2s; cursor: pointer; text-align: left;">
                    <div class="checkmark-badge" style="${isSelected ? 'display:flex;' : 'display:none;'} position: absolute; top: -6px; right: -6px; background: #C2185B; color: #fff; border-radius: 50%; width: 18px; height: 18px; font-size: 0.65rem; align-items: center; justify-content: center; z-index: 5; box-shadow: 0 2px 4px rgba(0,0,0,0.1);">
                        <i class="fas fa-check"></i>
                    </div>
                    <div style="font-size: 0.7rem; font-weight: 700; color: ${v.textColor}; background: ${v.tagBg}; padding: 2px 6px; border-radius: 4px; width: fit-content; margin-bottom: 6px;">${v.code}</div>
                    <div style="font-size: 0.82rem; font-weight: 700; color: #333; margin-bottom: 4px;">${v.title}</div>
                    <div style="font-size: 0.72rem; color: #888;">${v.desc}</div>
                </div>
            `;

            $slider.append(cardHtml);
        });
    }

    function updateVoucherSync(code, isSelected) {
        $('.modal-voucher-item').each(function () {
            const itemCode = $(this).attr('data-code');

            if (itemCode === code) {
                if (isSelected) {
                    $(this).addClass('selected');
                    $(this).find('.btn-modal-apply-voucher').text('Applied');
                } else {
                    $(this).removeClass('selected');
                    $(this).find('.btn-modal-apply-voucher').text('Apply');
                }
            }
        });

        renderMainPageVouchers();
        $('#modal-selected-count').text(selectedVouchers.size);
    }

    function initPreviewButton() {
        $('.btn-read-preview').off('click.productPreview').on('click.productPreview', function () {
            if (window.BookBlossomBookPreview) {
                const title = $('#detail-title').text().trim();
                const coverSrc = $('#detail-main-img').attr('src') || '/images/Book/book1.jpg';

                window.BookBlossomBookPreview.open({
                    title: title,
                    coverSrc: coverSrc
                });
            } else {
                showToast('Read Preview module is not ready.');
            }
        });
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

    window.BookBlossomProductDetails = {
        show: showProductDetails,
        showProductById: showProductById
    };

    async function fetchProductReviews(bookId) {
        try {
            const token = localStorage.getItem('jwtToken');
            const headers = { 'Content-Type': 'application/json' };
            if (token) headers['Authorization'] = `Bearer ${token}`;
            else {
                let guestId = localStorage.getItem('guestId');
                if (!guestId) {
                    guestId = 'guest_' + Math.random().toString(36).substr(2, 9);
                    localStorage.setItem('guestId', guestId);
                }
                headers['X-Guest-Id'] = guestId;
            }

            const response = await fetch(`/api/Review?bookId=${bookId}`, { headers: headers });
            if (!response.ok) return;

            const reviews = await response.json();
            window._currentBookReviews = reviews;
            window._currentReviewStarFilter = 'all';
            window._currentReviewSort = 'default';
            $('.btn-star-filter').removeClass('active');
            $('.btn-star-filter[data-star="all"]').addClass('active');
            $('#detail-review-sort').val('default');
            applyReviewFilters();
        } catch (err) {
            console.error('Failed to load reviews', err);
        }
    }

    function applyReviewFilters() {
        let reviews = window._currentBookReviews || [];
        
        // Filter by star
        const star = window._currentReviewStarFilter;
        if (star && star !== 'all') {
            const starInt = parseInt(star);
            reviews = reviews.filter(r => r.rating === starInt);
        }
        
        // Sort
        const sort = window._currentReviewSort;
        if (sort === 'most-hearts') {
            reviews = [...reviews].sort((a, b) => b.likeCount - a.likeCount);
        } else if (sort === 'least-hearts') {
            reviews = [...reviews].sort((a, b) => a.likeCount - b.likeCount);
        } else {
            reviews = [...reviews];
        }
        
        renderReviewsList(reviews, window._currentBookReviews.length);
    }

    function renderReviewsList(reviews, totalOriginalCount) {
        const $list = $('.product-reviews-list');
        $list.empty();
        
        const total = totalOriginalCount !== undefined ? totalOriginalCount : (reviews ? reviews.length : 0);

        if (!reviews || reviews.length === 0) {
            if (totalOriginalCount && totalOriginalCount > 0) {
                $list.html('<p style="text-align:center;color:#888;padding:20px;">No reviews match the selected filter.</p>');
            } else {
                $list.html('<p style="text-align:center;color:#888;padding:20px;">No reviews yet. Be the first to review this book after purchasing!</p>');
            }
            $('#detail-reviews-count').text(`${total} Reviews`);
            $('#detail-tab-rev-count').text(total);
            return;
        }

        $('#detail-reviews-count').text(`${total} Reviews`);
        $('#detail-tab-rev-count').text(total);

        window._reviewLightboxItems = [];

        let totalRating = 0;
        reviews.forEach((r, i) => {
            totalRating += r.rating;
            let starsHtml = '';
            for(let s=0; s<5; s++) {
                starsHtml += s < r.rating ? '<i class="fas fa-star" style="color: #ffc107;"></i>' : '<i class="far fa-star" style="color: #e2e8f0;"></i>';
            }

            const allMedia = [];
            if (r.mediaUrls && r.mediaUrls.length > 0) {
                r.mediaUrls.forEach(url => allMedia.push({ url, type: 'image' }));
            }
            if (r.videoUrls && r.videoUrls.length > 0) {
                r.videoUrls.forEach(url => allMedia.push({ url, type: 'video' }));
            }
            if (allMedia.length === 0 && r.imageVideoPath) {
                r.imageVideoPath.split(',').forEach(p => {
                    const trimmed = p.trim();
                    if (!trimmed) return;
                    const isVid = trimmed.match(/\.(mp4|mov|webm)$/i);
                    allMedia.push({ url: trimmed, type: isVid ? 'video' : 'image' });
                });
            }

            const reviewMediaStartIndex = window._reviewLightboxItems.length;
            allMedia.forEach(m => window._reviewLightboxItems.push(m));

            let mediaHtml = '';
            if (allMedia.length > 0) {
                mediaHtml = '<div class="review-media-gallery" style="display:flex;gap:8px;flex-wrap:wrap;margin-top:12px;">';
                allMedia.forEach((m, mi) => {
                    const globalIdx = reviewMediaStartIndex + mi;
                    if (mi >= 5) return;
                    if (m.type === 'video') {
                        mediaHtml += `
                            <div class="review-media-thumb" data-lightbox-index="${globalIdx}"
                                 style="position:relative;width:80px;height:80px;border-radius:10px;overflow:hidden;cursor:pointer;border:2px solid #e0e0e0;background:#000;flex-shrink:0;">
                                <video src="${m.url}" style="width:100%;height:100%;object-fit:cover;opacity:0.85;"></video>
                                <div style="position:absolute;inset:0;display:flex;align-items:center;justify-content:center;pointer-events:none;">
                                    <i class="fas fa-play-circle" style="font-size:1.8rem;color:rgba(255,255,255,0.9);text-shadow:0 1px 4px rgba(0,0,0,0.5);"></i>
                                </div>
                            </div>`;
                    } else {
                        mediaHtml += `
                            <div class="review-media-thumb" data-lightbox-index="${globalIdx}"
                                 style="width:80px;height:80px;border-radius:10px;overflow:hidden;cursor:pointer;border:2px solid #e0e0e0;flex-shrink:0;">
                                <img src="${m.url}" style="width:100%;height:100%;object-fit:cover;transition:transform 0.2s;" 
                                     onmouseover="this.style.transform='scale(1.08)'" onmouseout="this.style.transform='scale(1)'">
                            </div>`;
                    }
                });
                if (allMedia.length > 5) {
                    mediaHtml += `<div style="width:80px;height:80px;border-radius:10px;background:rgba(0,0,0,0.55);display:flex;align-items:center;justify-content:center;cursor:pointer;flex-shrink:0;font-weight:700;color:#fff;font-size:1.1rem;" data-lightbox-index="${reviewMediaStartIndex}">+${allMedia.length - 5}</div>`;
                }
                mediaHtml += '</div>';
            }

            const isLiked = r.isLikedByCurrentUser;
            const likeIconClass = isLiked ? 'fas' : 'far';
            const likeColorStyle = isLiked ? 'color: #C2185B;' : '';
            const dateStr = new Date(r.createdAt).toLocaleDateString();

            const html = `
                <div class="prod-review-item" style="border-bottom: 1px solid #f0f0f0; padding-bottom: 20px; background: #fff; border-radius: 8px; padding: 15px; margin-bottom: 15px;" data-id="${r.reviewID}" data-rating="${r.rating}" data-likes="${r.likeCount}">
                    <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 8px;">
                        <img src="${r.customerAvatar || '/images/Avatar/default.png'}" alt="Avatar" style="width: 40px; height: 40px; border-radius: 50%; object-fit:cover;">
                        <div>
                            <h5 style="margin: 0; font-size: 0.95rem; font-weight: 700; color: #333;">${r.customerName || 'Anonymous User'}</h5>
                            <span style="font-size: 0.8rem; color: #999;">${dateStr}</span>
                        </div>
                        <div style="margin-left: auto;">${starsHtml}</div>
                    </div>
                    <p style="font-size: 0.9rem; color: #555; line-height: 1.6; margin: 0 0 4px 0; padding-left: 52px;">"${r.content}"</p>
                    <div style="padding-left: 52px;">
                        ${mediaHtml}
                        <div style="display:flex; gap:15px; margin-top:12px; align-items:center;">
                            <button class="btn btn-link btn-like-detail-review p-0" style="text-decoration:none; font-size:0.85rem; color:#888;">
                                <i class="${likeIconClass} fa-heart" style="${likeColorStyle}"></i> <span class="like-count">${r.likeCount || 0}</span>
                            </button>
                            <button class="btn btn-link p-0 btn-report-detail-review" style="text-decoration:none; font-size:0.85rem; color:#888;">
                                <i class="fas fa-flag"></i> Report
                            </button>
                        </div>
                    </div>
                </div>
            `;
            $list.append(html);
        });

        const avg = reviews.length > 0 ? totalRating / reviews.length : 0;
        if (window._currentReviewStarFilter === 'all' || !window._currentReviewStarFilter) {
            // Only update the main product rating stars if we are looking at ALL reviews,
            // otherwise filtering by 1 star would drop the book's overall rating to 1 star!
            renderRatingStars(avg, avg.toFixed(1));
        }

        initReviewLightbox();
    }

    function initReviewFilters() {
        if (!window._reviewFiltersInitialized) {
            window._reviewFiltersInitialized = true;
            
            $(document).on('click', '.btn-star-filter', function() {
                $('.btn-star-filter').removeClass('active');
                $(this).addClass('active');
                window._currentReviewStarFilter = $(this).data('star');
                // The function applyReviewFilters is inside the outer IIFE but we need to call it.
                // Oh wait, applyReviewFilters is scoped inside this IIFE, so it can be called directly.
                if (typeof applyReviewFilters === 'function') {
                    applyReviewFilters();
                }
            });

            $(document).on('change', '#detail-review-sort', function() {
                window._currentReviewSort = $(this).val();
                if (typeof applyReviewFilters === 'function') {
                    applyReviewFilters();
                }
            });
        }
    }

    function initReviewLightbox() {
        initReviewFilters();
        if ($('#review-lightbox-overlay').length === 0) {
            const overlay = `
                <div id="review-lightbox-overlay" style="display:none;position:fixed;inset:0;z-index:99999;background:rgba(0,0,0,0.92);align-items:center;justify-content:center;">
                    <button id="lightbox-close" style="position:absolute;top:18px;right:24px;background:none;border:none;color:#fff;font-size:2rem;cursor:pointer;z-index:10;"><i class="fas fa-times"></i></button>
                    <button id="lightbox-prev" style="position:absolute;left:18px;top:50%;transform:translateY(-50%);background:rgba(255,255,255,0.15);border:none;color:#fff;font-size:1.8rem;width:48px;height:48px;border-radius:50%;cursor:pointer;z-index:10;"><i class="fas fa-chevron-left"></i></button>
                    <button id="lightbox-next" style="position:absolute;right:18px;top:50%;transform:translateY(-50%);background:rgba(255,255,255,0.15);border:none;color:#fff;font-size:1.8rem;width:48px;height:48px;border-radius:50%;cursor:pointer;z-index:10;"><i class="fas fa-chevron-right"></i></button>
                    <div id="lightbox-media-wrapper" style="max-width:90vw;max-height:88vh;display:flex;align-items:center;justify-content:center;"></div>
                    <div id="lightbox-counter" style="position:absolute;bottom:18px;left:50%;transform:translateX(-50%);color:rgba(255,255,255,0.7);font-size:0.9rem;"></div>
                </div>`;
            $('body').append(overlay);

            $('#lightbox-close').on('click', closeLightbox);
            $('#review-lightbox-overlay').on('click', function(e) {
                if (e.target === this) closeLightbox();
            });
            $('#lightbox-prev').on('click', () => navigateLightbox(-1));
            $('#lightbox-next').on('click', () => navigateLightbox(1));

            $(document).on('keydown.lightbox', function(e) {
                if ($('#review-lightbox-overlay').is(':visible')) {
                    if (e.key === 'ArrowLeft') navigateLightbox(-1);
                    if (e.key === 'ArrowRight') navigateLightbox(1);
                    if (e.key === 'Escape') closeLightbox();
                }
            });
        }

        $(document).off('click.reviewThumb').on('click.reviewThumb', '.review-media-thumb', function() {
            const idx = parseInt($(this).data('lightbox-index'));
            openLightbox(idx);
        });
    }

    function openLightbox(index) {
        window._lightboxCurrentIndex = index;
        renderLightboxMedia(index);
        $('#review-lightbox-overlay').css('display', 'flex').hide().fadeIn(200);
    }

    function closeLightbox() {
        $('#review-lightbox-overlay').fadeOut(200);
        $('#lightbox-media-wrapper').empty();
    }

    function navigateLightbox(dir) {
        const items = window._reviewLightboxItems || [];
        if (!items.length) return;
        let next = ((window._lightboxCurrentIndex || 0) + dir + items.length) % items.length;
        window._lightboxCurrentIndex = next;
        renderLightboxMedia(next);
    }

    function renderLightboxMedia(index) {
        const items = window._reviewLightboxItems || [];
        if (!items.length) return;
        const item = items[index];
        const $wrapper = $('#lightbox-media-wrapper');
        $wrapper.empty();

        if (item.type === 'video') {
            $wrapper.html(`<video src="${item.url}" controls autoplay style="max-width:90vw;max-height:85vh;border-radius:8px;"></video>`);
        } else {
            $wrapper.html(`<img src="${item.url}" style="max-width:90vw;max-height:85vh;border-radius:8px;object-fit:contain;user-select:none;">`);
        }

        $('#lightbox-counter').text(`${index + 1} / ${items.length}`);
        $('#lightbox-prev').toggle(items.length > 1);
        $('#lightbox-next').toggle(items.length > 1);
    }
})(window, document, window.jQuery);