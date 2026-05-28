(function (window, document, $) {
    if (!$) {
        console.error('explore.js requires jQuery.');
        return;
    }

    $(document).ready(function () {
        initExplorePage();
        initExploreFilterToggle();
        initExploreCategoryTags();
        initExploreSearch();
        initExplorePriceSlider();
        initExploreCustomDropdowns();
        initExploreBookCards();
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

    function initExploreCategoryTags() {
        $('.category-tags .tag').off('click.explore').on('click.explore', function () {
            $('.category-tags .tag').removeClass('active');
            $(this).addClass('active');

            const category = $(this).text().trim().toLowerCase();

            filterBooksByCategory(category);
        });
    }

    function initExploreSearch() {
        $('#search-submit-btn').off('click.explore').on('click.explore', function (e) {
            e.preventDefault();
            filterBooksBySearch();
        });

        $('#search-input').off('keydown.explore').on('keydown.explore', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                filterBooksBySearch();
            }
        });
    }

    function filterBooksBySearch() {
        const keyword = ($('#search-input').val() || '').trim().toLowerCase();

        $('#explore-section .book-grid .book-card').each(function () {
            const title = $(this).find('h3').text().trim().toLowerCase();
            const author = $(this).find('p').text().trim().toLowerCase();

            if (!keyword || title.includes(keyword) || author.includes(keyword)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    }

    function filterBooksByCategory(category) {
        if (!category || category === 'all') {
            $('#explore-section .book-grid .book-card').show();
            return;
        }

        /*
            Hiện tại _ExploreSection.cshtml chưa có data-category cho từng book.
            Nên đoạn này chỉ là mock filter nhẹ theo title/author.
            Sau này khi render từ database, thêm data-category="Fiction" vào .book-card là chuẩn nhất.
        */
        $('#explore-section .book-grid .book-card').each(function () {
            const text = $(this).text().trim().toLowerCase();

            if (text.includes(category)) {
                $(this).show();
            } else {
                $(this).hide();
            }
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
                const title = $card.find('h3').text().trim();
                const author = $card.find('p').first().text().trim() || 'Unknown Author';
                const imgSrc = $card.find('img').attr('src') || '/images/Book/book1.jpg';

                if (window.BookBlossomProductDetails) {
                    window.BookBlossomProductDetails.show({
                        title: title,
                        author: author,
                        imgSrc: imgSrc
                    });
                }
            });
    }
})(window, document, window.jQuery);