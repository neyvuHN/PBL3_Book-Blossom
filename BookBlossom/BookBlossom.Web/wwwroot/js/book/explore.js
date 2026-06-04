(function (window, document, $) {
    if (!$) {
        console.error('explore.js requires jQuery.');
        return;
    }

    let currentSearchTerm = '';
    let currentCategory = '';
    let currentSortOrder = 'Ascending'; // Default
    let allBooks = [];

    $(document).ready(function () {
        initExplorePage();
        initExploreFilterToggle();
        loadCategories();
        initExploreSearch();
        initExplorePriceSlider();
        initExploreCustomDropdowns();
        initExploreShowMoreToggles();
        initExploreBookCards();
        initFilterActions();
        loadBooks();
    });

    function initExplorePage() {
        $('#explore-section').show();
        $('#product-details-section').hide();

        if (window.BookBlossomLayout) {
            window.BookBlossomLayout.refreshNavbar();
        }
    }

    function initExploreFilterToggle() {
        $('#btn-toggle-filter').off('click.explore').on('click.explore', function (e) {
            e.preventDefault();

            const $layout = $('#explore-layout');
            $layout.toggleClass('filters-hidden');
            const isHidden = $layout.hasClass('filters-hidden');

            $(this).html(
                isHidden
                    ? '<i class="fas fa-sliders-h"></i> Show filters'
                    : '<i class="fas fa-sliders-h"></i> Hide filters'
            );
        });
    }

    async function loadCategories() {
        try {
            const categories = await apiClient.apiGet('/api/category?status=Active');
            renderCategoryTags(categories);
        } catch (error) {
            console.error('Failed to load categories', error);
        }
    }

    function renderCategoryTags(categories) {
        const $container = $('.category-tags');
        $container.empty();
        
        console.log("Rendering category tags. Categories from API:", categories);
        
        // Ensure "All" is always the first item
        const allCategories = [{ categoryName: 'All' }].concat(categories || []);
        
        allCategories.forEach((c, index) => {
            const isActive = (c.categoryName === 'All') ? 'active' : '';
            $container.append(`<span class="tag ${isActive}" data-name="${c.categoryName}" style="display: inline-block !important; visibility: visible !important;">${c.categoryName}</span>`);
        });

        $container.find('.tag').off('click.explore').on('click.explore', function () {
            $container.find('.tag').removeClass('active');
            $(this).addClass('active');

            const categoryName = $(this).attr('data-name');
            currentCategory = categoryName === 'All' ? '' : categoryName;
            
            loadBooks();
        });

        $(document).trigger('categoriesRendered.carousel');
    }

    function initExploreSearch() {
        $('#search-submit-btn').off('click.explore').on('click.explore', function (e) {
            e.preventDefault();
            currentSearchTerm = ($('#search-input').val() || '').trim();
            loadBooks();
        });

        $('#search-input').off('keydown.explore').on('keydown.explore', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                currentSearchTerm = ($(this).val() || '').trim();
                loadBooks();
            }
        });
    }

    async function loadBooks() {
        const url = `/api/realbook?searchTerm=${encodeURIComponent(currentSearchTerm)}&category=${encodeURIComponent(currentCategory)}&sortOrder=${currentSortOrder}`;
        try {
            const books = await apiClient.apiGet(url);
            allBooks = books || [];
            generateDynamicFilters(allBooks);
            renderBooks(allBooks);
        } catch (error) {
            console.error('Failed to load books', error);
        }
    }

    function renderBooks(books) {
        const $grid = $('.book-grid');
        $grid.empty();

        if (!books || books.length === 0) {
            $grid.append('<div style="grid-column: 1/-1; text-align: center; padding: 40px; color: #777;">No books found matching your criteria.</div>');
            return;
        }

        books.forEach((book) => {
            if (!book.isContinued) return; // Hide discontinued books
            
            // Random image based on ID so it's consistent
            const randomImg = `/images/Book/book${(book.bookID % 6) + 1}.jpg`;
            const priceStr = book.price.toLocaleString('vi-VN');
            const publisherDisplay = book.publisher || 'Unknown Publisher';
            const rating = (4.0 + (book.bookID % 10) / 10).toFixed(1); // Fake rating for UI

            const html = `
                <div class="book-card" data-id="${book.bookID}">
                    <img src="${randomImg}" alt="${book.title}">
                    <div class="book-info">
                        <h3>${book.title}</h3>
                        <p>${publisherDisplay}</p>
                        <div class="book-meta">
                            <span class="rating">⭐ ${rating}</span>
                            <span class="price">${priceStr} VND</span>
                        </div>
                    </div>
                </div>
            `;
            $grid.append(html);
        });
    }

    function initExplorePriceSlider() {
        initPriceSlider('explore', '#EEC7C9');
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
            let minVal = parseInt($minRange.val()) || 0;
            let maxVal = parseInt($maxRange.val()) || 0;

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
        }

        function handleManualInput() {
            let val = parseInt($(this).val()) || 0;
            const clampedVal = Math.min(Math.max(val, 0), maxRangeValue);

            const movingId = $(this).attr('id') || '';

            if (movingId.includes('min')) {
                const currentMax = parseInt($maxRange.val()) || maxRangeValue;
                $minRange.val(Math.min(clampedVal, currentMax));
            } else {
                const currentMin = parseInt($minRange.val()) || 0;
                $maxRange.val(Math.max(clampedVal, currentMin));
            }

            updateSlider.call(this);
            $(this).val(clampedVal);
        }

        $minRange.off('input.explore').on('input.explore', updateSlider);
        $maxRange.off('input.explore').on('input.explore', updateSlider);
        $minInput.off('input.explore').on('input.explore', handleManualInput);
        $maxInput.off('input.explore').on('input.explore', handleManualInput);

        updateSlider.call($minRange[0]);
    }

    function initExploreCustomDropdowns() {
        $('.custom-dropdown').each(function () {
            const $dropdown = $(this);
            const $input = $dropdown.find('.dropdown-input');

            $dropdown.find('.dropdown-header')
                .off('click.explore')
                .on('click.explore', function (e) {
                    if (e.target !== $input[0]) {
                        $dropdown.toggleClass('open');
                    }
                });

            $input.off('focus.explore').on('focus.explore', function () {
                $dropdown.addClass('open');
            });

            $dropdown.find('.dropdown-list')
                .off('click.explore', 'li')
                .on('click.explore', 'li', function () {
                    $input.val($(this).text());
                    $dropdown.removeClass('open');
                });

            $(document)
                .off(`click.exploreDropdown-${$dropdown.attr('id')}`)
                .on(`click.exploreDropdown-${$dropdown.attr('id')}`, function (e) {
                    if (!$dropdown.is(e.target) && $dropdown.has(e.target).length === 0) {
                        $dropdown.removeClass('open');
                    }
                });
        });
    }

    function initExploreShowMoreToggles() {
        $('#filter-sidebar').off('click.showMore', '.view-more-link').on('click.showMore', '.view-more-link', function (e) {
            e.preventDefault();
            const $this = $(this);
            const target = $this.data('target');
            
            const hiddenItems = $(`.hidden-${target}-item`);
            const isExpanded = $this.hasClass('expanded');
            if (isExpanded) {
                hiddenItems.slideUp(200);
                $this.removeClass('expanded').text(`+ View More (${hiddenItems.length})`);
            } else {
                hiddenItems.slideDown(200);
                $this.addClass('expanded').text('- View Less');
            }
        });
    }

    function generateDynamicFilters(books) {
        if (!books) return;

        // 1. Gather unique authors
        const authorSet = new Set();
        books.forEach(b => {
            const authorsStr = b.authors || b.Authors || 'Unknown Author';
            authorsStr.split(/,+/).forEach(a => {
                const clean = a.trim();
                if (clean) authorSet.add(clean);
            });
        });
        const authors = [...authorSet].sort();

        // 2. Gather unique genres (categories)
        const genreSet = new Set();
        books.forEach(b => {
            if (b.categoryName) genreSet.add(b.categoryName);
        });
        const genres = [...genreSet].sort();

        // 3. Gather unique publishers
        const publisherSet = new Set();
        books.forEach(b => {
            const pub = b.publisher || b.Publisher || 'Unknown Publisher';
            publisherSet.add(pub.trim());
        });
        const publishers = [...publisherSet].sort();

        // 4. Gather unique publish years
        const years = [...new Set(books.map(b => b.publishYear || b.PublishYear).filter(Boolean))].sort((a, b) => b - a);

        const visibleCount = 5;

        // Render Authors
        const $authorList = $('#explore-author-checkbox-list');
        $authorList.empty();
        authors.forEach((auth, idx) => {
            const isHiddenStyle = idx >= visibleCount ? 'style="display: none;"' : '';
            const itemClass = idx >= visibleCount ? 'filter-checkbox-item hidden-author-item' : 'filter-checkbox-item';
            const itemId = `author-${normalizeText(auth).replace(/\s+/g, '-')}`;
            $authorList.append(`
                <label class="${itemClass}" ${isHiddenStyle} style="display: block; margin-bottom: 8px; cursor: pointer; font-size: 0.95rem; color: #555;">
                    <input type="checkbox" id="${itemId}" value="${auth}" style="margin-right: 8px;"> ${auth}
                </label>
            `);
        });
        if (authors.length > visibleCount) {
            $authorList.append(`
                <a href="javascript:void(0)" class="view-more-link filter-show-more" data-target="author" style="color: #C2185B; font-weight: 600; font-size: 0.9rem; text-decoration: none; display: inline-block; margin-top: 8px; transition: color 0.2s;">
                    + View More (${authors.length - visibleCount})
                </a>
            `);
        }

        // Render Genres
        const $genreList = $('#explore-genre-checkbox-list');
        $genreList.empty();
        genres.forEach((genre, idx) => {
            const isHiddenStyle = idx >= visibleCount ? 'style="display: none;"' : '';
            const itemClass = idx >= visibleCount ? 'filter-checkbox-item hidden-genre-item' : 'filter-checkbox-item';
            const itemId = `genre-${normalizeText(genre).replace(/\s+/g, '-')}`;
            $genreList.append(`
                <label class="${itemClass}" ${isHiddenStyle} style="display: block; margin-bottom: 8px; cursor: pointer; font-size: 0.95rem; color: #555;">
                    <input type="checkbox" id="${itemId}" value="${genre}" style="margin-right: 8px;"> ${genre}
                </label>
            `);
        });
        if (genres.length > visibleCount) {
            $genreList.append(`
                <a href="javascript:void(0)" class="view-more-link filter-show-more" data-target="genre" style="color: #C2185B; font-weight: 600; font-size: 0.9rem; text-decoration: none; display: inline-block; margin-top: 8px; transition: color 0.2s;">
                    + View More (${genres.length - visibleCount})
                </a>
            `);
        }

        // Render Publishers
        const $publisherList = $('#explore-publisher-checkbox-list');
        $publisherList.empty();
        publishers.forEach((pub, idx) => {
            const isHiddenStyle = idx >= visibleCount ? 'style="display: none;"' : '';
            const itemClass = idx >= visibleCount ? 'filter-checkbox-item hidden-publisher-item' : 'filter-checkbox-item';
            const itemId = `publisher-${normalizeText(pub).replace(/\s+/g, '-')}`;
            $publisherList.append(`
                <label class="${itemClass}" ${isHiddenStyle} style="display: block; margin-bottom: 8px; cursor: pointer; font-size: 0.95rem; color: #555;">
                    <input type="checkbox" id="${itemId}" value="${pub}" style="margin-right: 8px;"> ${pub}
                </label>
            `);
        });
        if (publishers.length > visibleCount) {
            $publisherList.append(`
                <a href="javascript:void(0)" class="view-more-link filter-show-more" data-target="publisher" style="color: #C2185B; font-weight: 600; font-size: 0.9rem; text-decoration: none; display: inline-block; margin-top: 8px; transition: color 0.2s;">
                    + View More (${publishers.length - visibleCount})
                </a>
            `);
        }

        // Render Years
        const $fromList = $('#year-from-dropdown .dropdown-list');
        const $toList = $('#year-to-dropdown .dropdown-list');
        $fromList.empty();
        $toList.empty();
        years.forEach(yr => {
            $fromList.append(`<li>${yr}</li>`);
            $toList.append(`<li>${yr}</li>`);
        });
    }

    function initFilterActions() {
        // Apply Filters
        $('#btn-apply-explore-filters').off('click.exploreApply').on('click.exploreApply', function (e) {
            e.preventDefault();
            applyExploreFilters();
        });

        // Clear Filters
        $('#clear-explore-filters').off('click.exploreClear').on('click.exploreClear', function (e) {
            e.preventDefault();

            // Uncheck checkboxes
            $('#explore-author-checkbox-list input[type="checkbox"]').prop('checked', false);
            $('#explore-genre-checkbox-list input[type="checkbox"]').prop('checked', false);
            $('#explore-publisher-checkbox-list input[type="checkbox"]').prop('checked', false);

            // Reset price sliders
            $('#explore-price-min').val(0);
            $('#explore-price-max').val(1000);
            // Re-trigger visual slider updates
            $('#explore-price-min').trigger('input');

            // Clear years
            $('#publish-year-from').val('');
            $('#publish-year-to').val('');

            // Re-render full list
            renderBooks(allBooks);
        });
    }

    function applyExploreFilters() {
        const checkedAuthors = $('#explore-author-checkbox-list input[type="checkbox"]:checked').map(function () {
            return $(this).val();
        }).get();

        const checkedGenres = $('#explore-genre-checkbox-list input[type="checkbox"]:checked').map(function () {
            return $(this).val();
        }).get();

        const checkedPublishers = $('#explore-publisher-checkbox-list input[type="checkbox"]:checked').map(function () {
            return $(this).val();
        }).get();

        const minPrice = (parseInt($('#explore-price-min').val(), 10) || 0) * 1000;
        const maxPrice = (parseInt($('#explore-price-max').val(), 10) || 1000) * 1000;

        const fromYear = parseInt($('#publish-year-from').val(), 10) || null;
        const toYear = parseInt($('#publish-year-to').val(), 10) || null;

        const filtered = allBooks.filter(book => {
            // 1. Author Filter (Skip if no author is checked)
            if (checkedAuthors.length > 0) {
                const bookAuthors = (book.authors || book.Authors || 'Unknown Author').split(/,+/).map(a => a.trim().toLowerCase());
                const hasAuthorMatch = checkedAuthors.some(auth => bookAuthors.includes(auth.toLowerCase()));
                if (!hasAuthorMatch) return false;
            }

            // 2. Genre Filter (Skip if no genre is checked)
            if (checkedGenres.length > 0) {
                const bookGenre = (book.categoryName || '').trim().toLowerCase();
                const hasGenreMatch = checkedGenres.some(g => bookGenre === g.toLowerCase());
                if (!hasGenreMatch) return false;
            }

            // 3. Publisher Filter (Skip if no publisher is checked)
            if (checkedPublishers.length > 0) {
                const bookPublisher = (book.publisher || book.Publisher || 'Unknown Publisher').trim().toLowerCase();
                const hasPublisherMatch = checkedPublishers.some(p => bookPublisher === p.toLowerCase());
                if (!hasPublisherMatch) return false;
            }

            // 4. Price Filter
            const bookPrice = Number(book.price || book.Price || 0);
            if (bookPrice < minPrice || bookPrice > maxPrice) return false;

            // 5. Publish Year Filter
            const bookYear = parseInt(book.publishYear || book.PublishYear, 10);
            if (bookYear) {
                if (fromYear && bookYear < fromYear) return false;
                if (toYear && bookYear > toYear) return false;
            } else {
                if (fromYear || toYear) return false;
            }

            return true;
        });

        renderBooks(filtered);
    }

    function normalizeText(text) {
        return (text || '')
            .toString()
            .toLowerCase()
            .replace(/\s+/g, ' ')
            .trim();
    }

    function initExploreBookCards() {
        $(document)
            .off('click.exploreBookCard')
            .on('click.exploreBookCard', '#explore-section .book-grid .book-card', function (e) {
                e.preventDefault();

                const $card = $(this);
                const bookId = $card.attr('data-id');

                if (window.BookBlossomProductDetails && bookId) {
                    window.BookBlossomProductDetails.showProductById(bookId);
                }
            });
    }
})(window, document, window.jQuery);