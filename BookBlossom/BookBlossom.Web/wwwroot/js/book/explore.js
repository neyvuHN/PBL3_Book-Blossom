(function (window, document, $) {
    if (!$) {
        console.error('explore.js requires jQuery.');
        return;
    }

    let currentSearchTerm = '';
    let currentCategory = '';
    let currentSortOrder = 'Ascending'; // Default

    $(document).ready(function () {
        initExplorePage();
        initExploreFilterToggle();
        loadCategories();
        initExploreSearch();
        initExplorePriceSlider();
        initExploreCustomDropdowns();
        initExploreBookCards();
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
        
        $container.append('<span class="tag active" data-name="All">All</span>');
        
        if (categories && categories.length > 0) {
            categories.forEach(c => {
                $container.append(`<span class="tag" data-name="${c.categoryName}">${c.categoryName}</span>`);
            });
        }

        $container.find('.tag').off('click.explore').on('click.explore', function () {
            $container.find('.tag').removeClass('active');
            $(this).addClass('active');

            const categoryName = $(this).attr('data-name');
            currentCategory = categoryName === 'All' ? '' : categoryName;
            
            loadBooks();
        });
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
            renderBooks(books);
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

            $dropdown.find('li')
                .off('click.explore')
                .on('click.explore', function () {
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