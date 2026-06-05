(function (window, document, $) {
    if (!$) {
        console.error('blind-date.js requires jQuery.');
        return;
    }

    let allBlindBooks = [];

    $(document).ready(function () {
        initBlindDatePage();
        initMatchmakerSidebar();
        initBlindPriceSlider();
        initBlindFilters();
        initHowItWorks();
        initBlindCards();
        loadBlindBooks();
    });

    async function loadBlindBooks() {
        const grid = $('#blind-date-grid');
        grid.html(`
            <div class="loading-placeholder text-center" style="grid-column: 1 / -1; padding: 50px; color: #a291b5;">
                <i class="fas fa-spinner fa-spin fa-3x" style="margin-bottom: 15px;"></i>
                <p style="font-size: 1.1rem; font-weight: 600;">Loading book matches...</p>
            </div>
        `);

        try {
            const response = await fetch('/api/BlindBook');
            if (response.ok) {
                allBlindBooks = await response.json();
                generateDynamicFilters(allBlindBooks);
                renderBlindBooks(allBlindBooks);
                handleInitialHash();
            } else {
                grid.html('<p class="text-center w-100" style="grid-column: 1 / -1; color: #ff6b6b; padding: 30px; font-weight: 600;">Failed to load mystery books. Please try again later.</p>');
            }
        } catch (error) {
            console.error("Error loading blind books:", error);
            grid.html('<p class="text-center w-100" style="grid-column: 1 / -1; color: #ff6b6b; padding: 30px; font-weight: 600;">Error connecting to server.</p>');
        }
    }

    function renderBlindBooks(books) {
        const grid = $('#blind-date-grid');
        grid.empty();

        if (books.length === 0) {
            grid.html(`
                <div class="empty-state text-center" style="grid-column: 1 / -1; padding: 50px; color: #a291b5;">
                    <i class="fas fa-search fa-3x" style="margin-bottom: 15px; opacity: 0.5;"></i>
                    <p style="font-size: 1.1rem; font-weight: 600;">No mystery books match your current filters.</p>
                </div>
            `);
            return;
        }

        books.forEach((book, index) => {
            const imgId = (book.blindBookID % 5) + 1;
            const fallbackImg = `/images/BlindDateBook/BlindBook${imgId}.jpg`;
            // Ưu tiên ảnh thật từ DB (imagePaths đã sort theo SortOrder từ API)
            const imgSrc = (book.imagePaths && book.imagePaths.length > 0)
                ? book.imagePaths[0]
                : fallbackImg;

            const conditions = ['Pristine - Like new', 'Gift-ready', 'Well Loved - Has character', 'Gently read'];
            const condition = conditions[book.blindBookID % conditions.length];

            const priceFormatted = Number(book.Price || book.price || 0).toLocaleString('vi-VN') + ' VNĐ';
            const hashtags = book.Hashtags || book.hashtags || '#Mystery #Suspense #MustRead';
            const firstWords = book.Quotes ? book.Quotes.split(' ').slice(0, 12).join(' ') + '...' : 'A beautiful mystery book selection...';

            const cardHtml = `
                <div class="blind-card" data-id="${book.blindBookID}" data-index="${index}">
                    <div class="blind-card-image">
                        <img src="${imgSrc}" alt="Mystery Book">
                        <div class="view-overlay">
                            <i class="fas fa-eye"></i>
                        </div>
                    </div>
                    <div class="blind-card-body">
                        <div class="card-hashtags">${hashtags}</div>
                        <p class="card-desc">"${firstWords}"</p>
                        <div class="card-status">
                            <span class="verified-badge"><i class="fas fa-leaf"></i> Verified Shop</span>
                            <span class="card-price">${priceFormatted}</span>
                        </div>
                        <div class="card-condition">Condition: ${condition}</div>
                    </div>
                </div>
            `;
            grid.append(cardHtml);
        });
    }

    function handleInitialHash() {
        const hash = window.location.hash || '';
        if (hash.startsWith('#blind-details-')) {
            const idOrTitle = decodeURIComponent(hash.replace('#blind-details-', ''));
            let match = allBlindBooks.find(b => {
                const bid = b.blindBookID || b.blindBookId || b.id;
                return bid && bid.toString() === idOrTitle;
            });
            if (!match) {
                match = allBlindBooks.find(b => {
                    const cat = b.Category || b.category || '';
                    return cat.toLowerCase() === idOrTitle.toLowerCase();
                });
            }
            if (match) {
                const bookData = getBlindBookDataFromObject(match);
                if (window.BookBlossomBlindDateDetails) {
                    window.BookBlossomBlindDateDetails.show(bookData);
                }
            }
        }
    }

    function initBlindDatePage() {
        const hash = window.location.hash || '';

        if (!hash.startsWith('#blind-details-')) {
            $('#blind-date-section').show();
            $('#blind-date-details-section').hide();

            history.replaceState({ view: 'blind-date' }, '', '/BlindDate');
        }

        if (window.BookBlossomLayout) {
            window.BookBlossomLayout.refreshNavbar();
        }
    }

    function initMatchmakerSidebar() {
        const $blindSidebar = $('#blind-sidebar');
        const $btnToggleMatchmaker = $('#btn-toggle-matchmaker');
        const $btnHideSidebar = $('#btn-hide-sidebar');

        $btnToggleMatchmaker.off('click.blindDate').on('click.blindDate', function () {
            $blindSidebar.toggleClass('collapsed');

            const isCollapsed = $blindSidebar.hasClass('collapsed');

            $(this).find('i').attr('class', isCollapsed ? 'fas fa-filter' : 'fas fa-times');
        });

        $btnHideSidebar.off('click.blindDate').on('click.blindDate', function () {
            $blindSidebar.addClass('collapsed');
            $btnToggleMatchmaker.find('i').attr('class', 'fas fa-filter');
        });
    }

    function initBlindPriceSlider() {
        initPriceSlider('blind', '#a291b5');
    }

    function initPriceSlider(prefix, accentColor) {
        const $minRange = $(`#${prefix}-price-min`);
        const $maxRange = $(`#${prefix}-price-max`);
        const $minInput = $(`#${prefix}-price-min-input`);
        const $maxInput = $(`#${prefix}-price-max-input`);
        const $tooltipMin = $(`#${prefix}-tooltip-min`);
        const $tooltipMax = $(`#${prefix}-tooltip-max`);
        const $track = $(`#${prefix}-price-track`);
        const maxRangeValue = 1000;

        if (!$minRange.length || !$maxRange.length) return;

        function updateSlider() {
            let minVal = parseInt($minRange.val(), 10) || 0;
            let maxVal = parseInt($maxRange.val(), 10) || 0;

            const movingId = $(this).attr('id') || '';

            if (minVal > maxVal) {
                if (movingId.includes('min')) {
                    $minRange.val(maxVal);
                    minVal = maxVal;
                } else {
                    $maxRange.val(minVal);
                    maxVal = minVal;
                }
            }

            const percent1 = (minVal / maxRangeValue) * 100;
            const percent2 = (maxVal / maxRangeValue) * 100;

            $track.css(
                'background',
                `linear-gradient(to right, #eee ${percent1}%, ${accentColor} ${percent1}%, ${accentColor} ${percent2}%, #eee ${percent2}%)`
            );

            $tooltipMin.text(minVal).css('left', `${percent1}%`);
            $tooltipMax.text(maxVal).css('left', `${percent2}%`);

            $minInput.val(minVal);
            $maxInput.val(maxVal);

            if (typeof applyBlindFilters === 'function') {
                applyBlindFilters();
            }
        }

        function handleManualInput() {
            let val = parseInt($(this).val(), 10) || 0;
            val = Math.min(Math.max(val, 0), maxRangeValue);

            const movingId = $(this).attr('id') || '';

            if (movingId.includes('min')) {
                const currentMax = parseInt($maxRange.val(), 10) || maxRangeValue;
                $minRange.val(Math.min(val, currentMax));
            } else {
                const currentMin = parseInt($minRange.val(), 10) || 0;
                $maxRange.val(Math.max(val, currentMin));
            }

            updateSlider.call(this);
            $(this).val(val);
        }

        $minRange.off('input.blindDate').on('input.blindDate', updateSlider);
        $maxRange.off('input.blindDate').on('input.blindDate', updateSlider);
        $minInput.off('input.blindDate').on('input.blindDate', handleManualInput);
        $maxInput.off('input.blindDate').on('input.blindDate', handleManualInput);

        updateSlider.call($minRange[0]);
    }

    function initBlindFilters() {
        // Event delegation for dynamic vibe tags
        $('#blind-sidebar').off('click.blindDate', '.vibe-tag').on('click.blindDate', '.vibe-tag', function () {
            $(this).toggleClass('active');
            applyBlindFilters();
        });

        // Event delegation for dynamic checkboxes
        $('#blind-sidebar').off('change.blindDate', 'input[type="checkbox"]').on('change.blindDate', 'input[type="checkbox"]', applyBlindFilters);

        // Event delegation for show more links
        $('#blind-sidebar').off('click.showMore', '.view-more-link').on('click.showMore', '.view-more-link', function (e) {
            e.preventDefault();
            const $this = $(this);
            const target = $this.data('target');
            if (target === 'genre') {
                const hiddenItems = $('.hidden-genre-item');
                const isExpanded = $this.hasClass('expanded');
                if (isExpanded) {
                    hiddenItems.slideUp(200);
                    $this.removeClass('expanded').text(`+ View More (${hiddenItems.length})`);
                } else {
                    hiddenItems.slideDown(200);
                    $this.addClass('expanded').text('- View Less');
                }
            } else if (target === 'tag') {
                const hiddenItems = $('.hidden-tag-item');
                const isExpanded = $this.hasClass('expanded');
                if (isExpanded) {
                    hiddenItems.slideUp(200);
                    $this.removeClass('expanded').text(`+ View More (${hiddenItems.length})`);
                } else {
                    hiddenItems.css('display', 'inline-block').hide().slideDown(200);
                    $this.addClass('expanded').text('- View Less');
                }
            }
        });

        $('.clear-filters').off('click.blindDate').on('click.blindDate', function (e) {
            e.preventDefault();

            $('#blind-sidebar input[type="checkbox"]').prop('checked', true);
            $('#blind-sidebar .vibe-tag').addClass('active');

            $('#blind-price-min').val(0).trigger('input');
            $('#blind-price-max').val(1000).trigger('input');

            renderBlindBooks(allBlindBooks);
        });
    }

    function generateDynamicFilters(books) {
        // 1. Gather unique categories (genres)
        const categories = [...new Set(books.map(b => b.Category || b.category).filter(Boolean))];
        
        // 2. Gather unique hashtags
        const tagSet = new Set();
        books.forEach(b => {
            const hashtags = b.Hashtags || b.hashtags || '';
            hashtags.split(/[\s,;#]+/).forEach(t => {
                let clean = t.trim();
                if (clean) {
                    if (!clean.startsWith('#')) {
                        clean = '#' + clean;
                    }
                    tagSet.add(clean);
                }
            });
        });
        const tags = [...tagSet].sort();

        // 3. Render Genres (Categories)
        const genreContainer = $('#blind-genre-checkbox-list');
        genreContainer.empty();
        
        // Emoji map for genres
        const emojiMap = {
            'romance': '💖',
            'lang man': '💖',
            'lãng mạn': '💖',
            'thriller': '💀',
            'mystery': '🔍',
            'trinh thám': '🔍',
            'horror': '💀',
            'kinh dị': '💀',
            'sci-fi': '🚀',
            'fantasy': '🚀',
            'viễn tưởng': '🚀',
            'kỹ năng': '🌱',
            'ky nang': '🌱',
            'văn học': '🍃',
            'van hoc': '🍃',
            'kinh doanh': '💼',
            'kinh te': '💼',
            'phiêu lưu': '🗺️',
            'phieu luu': '🗺️',
            'đời sống': '🍳',
            'doi song': '🍳',
            'lich su': '📜',
            'lịch sử': '📜'
        };

        const getEmoji = (cat) => {
            const normalized = cat.toLowerCase();
            for (const key in emojiMap) {
                if (normalized.includes(key)) return emojiMap[key];
            }
            return '📖';
        };

        const visibleGenreCount = 5;
        categories.forEach((cat, idx) => {
            const emoji = getEmoji(cat);
            const isHiddenStyle = idx >= visibleGenreCount ? 'style="display: none;"' : '';
            const itemClass = idx >= visibleGenreCount ? 'filter-checkbox-item hidden-genre-item' : 'filter-checkbox-item';
            const itemId = `genre-${normalizeText(cat).replace(/\s+/g, '-')}`;
            genreContainer.append(`
                <label class="${itemClass}" ${isHiddenStyle}>
                    <input type="checkbox" id="${itemId}" value="${cat}" checked> ${emoji} ${cat}
                </label>
            `);
        });

        if (categories.length > visibleGenreCount) {
            genreContainer.append(`
                <a href="javascript:void(0)" class="view-more-link filter-show-more" data-target="genre" style="color: #a291b5; font-weight: 600; font-size: 0.9rem; text-decoration: none; display: inline-block; margin-top: 8px; transition: color 0.2s;">
                    + View More (${categories.length - visibleGenreCount})
                </a>
            `);
        }

        // 4. Render Vibe Tags
        const tagContainer = $('#blind-vibe-tag-cloud');
        tagContainer.empty();

        const visibleTagCount = 8;
        tags.forEach((tag, idx) => {
            const isHiddenStyle = idx >= visibleTagCount ? 'style="display: none;"' : '';
            const tagClass = idx >= visibleTagCount ? 'vibe-tag active hidden-tag-item' : 'vibe-tag active';
            tagContainer.append(`
                <span class="${tagClass}" ${isHiddenStyle}>${tag}</span>
            `);
        });

        if (tags.length > visibleTagCount) {
            tagContainer.append(`
                <a href="javascript:void(0)" class="view-more-link tag-show-more" data-target="tag" style="color: #a291b5; font-weight: 600; font-size: 0.9rem; text-decoration: none; display: inline-block; margin-top: 8px; transition: color 0.2s;">
                    + View More (${tags.length - visibleTagCount})
                </a>
            `);
        }
    }

    function applyBlindFilters() {
        const inactiveTags = $('#blind-sidebar .vibe-tag:not(.active)').map(function () {
            return normalizeText($(this).text().replace('#', ''));
        }).get();

        const checkedGenres = $('#blind-genre-checkbox-list input[type="checkbox"]:checked').map(function () {
            return normalizeText($(this).val());
        }).get();

        const checkedConditions = $('#blind-sidebar input[type="checkbox"][id^="cond-"]:checked').map(function () {
            return normalizeText($(this).val() || $(this).closest('label').text());
        }).get();

        const minPrice = (parseInt($('#blind-price-min').val(), 10) || 0) * 1000;
        const maxPrice = (parseInt($('#blind-price-max').val(), 10) || 1000) * 1000;

        const filtered = allBlindBooks.filter(book => {
            const catField = book.Category || book.category || '';
            const tagField = book.Hashtags || book.hashtags || '';
            const keyField = book.Keywords || book.keywords || '';
            
            const cardText = normalizeText(catField + ' ' + tagField + ' ' + keyField);
            const bookPrice = Number(book.Price || book.price || 0);

            if (bookPrice < minPrice || bookPrice > maxPrice) return false;

            // Genre validation (Inclusive checklist filter)
            const bookCategory = normalizeText(catField);
            const hasGenreMatch = checkedGenres.some(genre => {
                if (genre.includes('thriller') || genre.includes('mystery') || genre.includes('trinh thám') || genre.includes('kinh dị')) {
                    return bookCategory.includes('mystery') || bookCategory.includes('thriller') || bookCategory.includes('trinh thám') || bookCategory.includes('kinh dị');
                }
                if (genre.includes('romance') || genre.includes('lãng mạn') || genre.includes('lang man')) {
                    return bookCategory.includes('romance') || bookCategory.includes('romantic') || bookCategory.includes('lãng mạn') || bookCategory.includes('lang man');
                }
                if (genre.includes('sci-fi') || genre.includes('fantasy') || genre.includes('science') || genre.includes('viễn tưởng')) {
                    return bookCategory.includes('science') || bookCategory.includes('sci-fi') || bookCategory.includes('fantasy') || bookCategory.includes('viễn tưởng') || bookCategory.includes('vien tuong');
                }
                if (genre.includes('drama') || genre.includes('literary') || genre.includes('prose') || genre.includes('văn học') || genre.includes('van hoc')) {
                    return bookCategory.includes('literary') || bookCategory.includes('drama') || bookCategory.includes('prose') || bookCategory.includes('văn học') || bookCategory.includes('van hoc') || bookCategory.includes('general');
                }
                return bookCategory.includes(genre);
            });
            if (!hasGenreMatch) return false;

            // Vibe tag validation (Inclusive checklist: hide book if any of its hashtags is inactive)
            if (inactiveTags.length > 0) {
                const bookHashtags = tagField.split(/[\s,;#]+/).map(t => normalizeText(t.replace('#', ''))).filter(Boolean);
                const hasInactiveMatch = bookHashtags.some(tag => inactiveTags.includes(tag));
                if (hasInactiveMatch) return false;
            }

            // Condition validation (Inclusive checklist filter)
            const conditionsList = ['Pristine - Like new', 'Gift-ready', 'Well Loved - Has character', 'Gently read'];
            const bookId = book.blindBookID || book.blindBookId || book.id || 0;
            const bookCondition = normalizeText(conditionsList[bookId % conditionsList.length]);
            const hasCondMatch = checkedConditions.some(cond => {
                if (cond.includes('new') || cond.includes('pristine')) return bookCondition.includes('new') || bookCondition.includes('pristine');
                if (cond.includes('gift')) return bookCondition.includes('gift');
                if (cond.includes('loved') || cond.includes('character')) return bookCondition.includes('loved');
                if (cond.includes('gently') || cond.includes('read')) return bookCondition.includes('gently') || bookCondition.includes('read');
                return bookCondition.includes(cond);
            });
            if (!hasCondMatch) return false;

            return true;
        });

        renderBlindBooks(filtered);
    }

    function normalizeText(text) {
        return (text || '')
            .toString()
            .toLowerCase()
            .replace(/\s+/g, ' ')
            .trim();
    }

    function initHowItWorks() {
        $(document).off('click.blindHowItWorks').on('click.blindHowItWorks', '.btn-how-it-works', function (e) {
            e.preventDefault();

            if ($('#how-it-works-modal').length) {
                $('#how-it-works-modal').fadeIn(200);
            } else {
                showToast('Pick a mystery book based on clues, vibe, price, and condition. The exact title is revealed after purchase.');
            }
        });

        $(document)
            .off('click.closeHowItWorks')
            .on('click.closeHowItWorks', '#btn-close-how-it-works, #how-it-works-modal .modal-backdrop', function () {
                $('#how-it-works-modal').fadeOut(200);
            });
    }

    function initBlindCards() {
        $(document)
            .off('click.blindCard')
            .on('click.blindCard', '#blind-date-grid .blind-card', async function (e) {
                e.preventDefault();

                const id = $(this).attr('data-id');
                if (!id) return;

                // Fetch detail from API to get real images (imagePaths)
                try {
                    const response = await fetch(`/api/blindbook/${id}`);
                    if (response.ok) {
                        const realBookData = await response.json();
                        const mapFn = window.BookBlossomBlindDateDetails && window.BookBlossomBlindDateDetails.mapBookData;
                        const blindBookData = mapFn ? mapFn(realBookData) : getBlindBookDataFromObject(realBookData);
                        if (window.BookBlossomBlindDateDetails) {
                            window.BookBlossomBlindDateDetails.show(blindBookData);
                        }
                        return;
                    }
                } catch (err) {
                    console.error('Error fetching blind book detail for card:', err);
                }

                // Fallback to cached book list data (no real images)
                const book = allBlindBooks.find(b => {
                    const bid = b.blindBookID || b.blindBookId || b.id;
                    return bid && bid.toString() === id.toString();
                });
                if (!book) return;

                const blindBookData = getBlindBookDataFromObject(book);
                if (window.BookBlossomBlindDateDetails) {
                    window.BookBlossomBlindDateDetails.show(blindBookData);
                }
            });
    }

    function getBlindBookDataFromObject(book) {
        const bookId = book.blindBookID || book.blindBookId || book.id || 0;
        const imgId = (bookId % 5) + 1;
        
        // Ưu tiên ảnh thật từ DB
        const realImages = (book.imagePaths && book.imagePaths.length > 0) ? book.imagePaths : null;
        const imgSrc = realImages ? realImages[0] : `/images/BlindDateBook/BlindBook${imgId}.jpg`;
        const allImages = realImages || [`/images/BlindDateBook/BlindBook${imgId}.jpg`];

        const hashtags = book.hashtags || book.Hashtags || '#Mystery #BlindDate';
        const quotes = book.quotes || book.Quotes || 'An intriguing mystery waiting to be solved...';
        const priceNum = book.price !== undefined ? book.price : (book.Price || 0);
        const price = Number(priceNum).toLocaleString('vi-VN') + ' VNĐ';

        const conditionsList = ['Pristine - Like new', 'Gift-ready', 'Well Loved - Has character', 'Gently read'];
        const condition = conditionsList[bookId % conditionsList.length];

        const rating = '4.2';
        const ratingRange = '4.0 - 4.2';
        const year = '1994';
        
        // Cập nhật lấy categoryName từ API
        const category = book.categoryName || book.CategoryName || book.category || book.Category || 'Mystery & Thriller';
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
})(window, document, window.jQuery);