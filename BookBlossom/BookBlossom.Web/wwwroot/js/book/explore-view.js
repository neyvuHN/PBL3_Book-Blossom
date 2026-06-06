(function (window, $) {
    class ExploreView {
        constructor() {
            this.selectors = {
                section: '#explore-section',
                productSection: '#product-details-section',
                toggleFilterBtn: '#btn-toggle-filter',
                layout: '#explore-layout',
                categoryTags: '.category-tags',
                searchInput: '#search-input',
                searchBtn: '#search-submit-btn',
                bookGrid: '.book-grid',
                authorList: '#explore-author-checkbox-list',
                genreList: '#explore-genre-checkbox-list',
                publisherList: '#explore-publisher-checkbox-list',
                priceMin: '#explore-price-min',
                priceMax: '#explore-price-max',
                priceMinInput: '#explore-price-min-input',
                priceMaxInput: '#explore-price-max-input',
                priceTrack: '#explore-price-track',
                tooltipMin: '#explore-tooltip-min',
                tooltipMax: '#explore-tooltip-max',
                yearFrom: '#publish-year-from',
                yearTo: '#publish-year-to',
                yearFromList: '#year-from-dropdown .dropdown-list',
                yearToList: '#year-to-dropdown .dropdown-list',
                applyBtn: '#btn-apply-explore-filters',
                clearBtn: '#clear-explore-filters',
                sidebar: '#filter-sidebar'
            };
        }

        init() {
            $(this.selectors.section).show();
            $(this.selectors.productSection).hide();

            if (window.BookBlossomLayout) {
                window.BookBlossomLayout.refreshNavbar();
            }

            this.initFilterToggle();
            this.initPriceSlider();
            this.initCustomDropdowns();
            this.initShowMoreToggles();
        }

        initFilterToggle() {
            $(this.selectors.toggleFilterBtn).off('click.explore').on('click.explore', (e) => {
                e.preventDefault();
                const $layout = $(this.selectors.layout);
                $layout.toggleClass('filters-hidden');
                const isHidden = $layout.hasClass('filters-hidden');
                $(e.currentTarget).html(
                    isHidden
                        ? '<i class="fas fa-sliders-h"></i> Show filters'
                        : '<i class="fas fa-sliders-h"></i> Hide filters'
                );
            });
        }

        renderCategoryTags(categories) {
            const $container = $(this.selectors.categoryTags);
            $container.empty();

            const allCategories = [{ categoryName: 'All' }].concat(categories || []);

            allCategories.forEach((c) => {
                const isActive = (c.categoryName === 'All') ? 'active' : '';
                $container.append(`<span class="tag ${isActive}" data-name="${c.categoryName}" style="display: inline-block !important; visibility: visible !important;">${c.categoryName}</span>`);
            });

            $(document).trigger('categoriesRendered.carousel');
        }

        bindCategoryClick(handler) {
            $(this.selectors.categoryTags).off('click.explore', '.tag').on('click.explore', '.tag', function () {
                $(this).siblings().removeClass('active');
                $(this).addClass('active');
                handler($(this).attr('data-name'));
            });
        }

        bindSearchInput(handler) {
            $(this.selectors.searchInput).off('input.explore').on('input.explore', function () {
                handler($(this).val());
            });

            $(this.selectors.searchBtn).off('click.explore').on('click.explore', (e) => {
                e.preventDefault();
                handler($(this.selectors.searchInput).val());
            });
        }

        bindApplyFilters(handler) {
            $(this.selectors.applyBtn).off('click.exploreApply').on('click.exploreApply', (e) => {
                e.preventDefault();
                handler();
            });
        }

        bindClearFilters(handler) {
            $(this.selectors.clearBtn).off('click.exploreClear').on('click.exploreClear', (e) => {
                e.preventDefault();
                
                $(`${this.selectors.authorList} input[type="checkbox"]`).prop('checked', false);
                $(`${this.selectors.genreList} input[type="checkbox"]`).prop('checked', false);
                $(`${this.selectors.publisherList} input[type="checkbox"]`).prop('checked', false);

                $(this.selectors.priceMin).val(0);
                $(this.selectors.priceMax).val(1000);
                $(this.selectors.priceMin).trigger('input');

                $(this.selectors.yearFrom).val('');
                $(this.selectors.yearTo).val('');

                handler();
            });
        }

        bindBookClick(handler) {
            $(document).off('click.exploreBookCard').on('click.exploreBookCard', `${this.selectors.section} .book-grid .book-card`, function (e) {
                e.preventDefault();
                handler($(this).attr('data-id'));
            });
        }

        getFilterValues() {
            return {
                authors: $(`${this.selectors.authorList} input[type="checkbox"]:checked`).map(function () { return $(this).val(); }).get(),
                genres: $(`${this.selectors.genreList} input[type="checkbox"]:checked`).map(function () { return $(this).val(); }).get(),
                publishers: $(`${this.selectors.publisherList} input[type="checkbox"]:checked`).map(function () { return $(this).val(); }).get(),
                minPrice: (parseInt($(this.selectors.priceMin).val(), 10) || 0) * 1000,
                maxPrice: (parseInt($(this.selectors.priceMax).val(), 10) || 1000) * 1000,
                fromYear: parseInt($(this.selectors.yearFrom).val(), 10) || null,
                toYear: parseInt($(this.selectors.yearTo).val(), 10) || null
            };
        }

        renderBooks(books, append = false) {
            const $grid = $(this.selectors.bookGrid);
            
            if (!append) {
                this.filteredBooks = books || [];
                this.currentPage = 1;
                this.pageSize = 20;
                $grid.empty();
            } else {
                this.currentPage++;
            }

            const startIndex = (this.currentPage - 1) * this.pageSize;
            const endIndex = this.currentPage * this.pageSize;
            const booksToRender = this.filteredBooks.slice(startIndex, endIndex);

            if (!append && this.filteredBooks.length === 0) {
                $grid.append('<div style="grid-column: 1/-1; text-align: center; padding: 40px; color: #777;">No books found matching your criteria.</div>');
                $('#load-more-explore-container').remove();
                return;
            }

            booksToRender.forEach((book) => {
                let mainImg = `/images/Book/book${(book.bookID % 6) + 1}.jpg`;
                if (book.imageUrls && book.imageUrls.length > 0) {
                    mainImg = book.imageUrls[0];
                }
                const priceStr = book.price.toLocaleString('vi-VN');
                const publisherDisplay = book.publisher || 'Unknown Publisher';
                const rating = (4.0 + (book.bookID % 10) / 10).toFixed(1);

                const html = `
                    <div class="book-card" data-id="${book.bookID}">
                        <img src="${mainImg}" alt="${book.title}">
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

            $('#load-more-explore-container').remove();
            if (endIndex < this.filteredBooks.length) {
                const loadMoreHtml = `
                    <div id="load-more-explore-container" style="grid-column: 1/-1; text-align: center; margin-top: 30px;">
                        <button id="btn-load-more-explore" style="background-color: #C2185B; color: white; border: none; padding: 10px 24px; border-radius: 8px; font-weight: 600; cursor: pointer; transition: background 0.3s;">
                            Load More
                        </button>
                    </div>
                `;
                $grid.append(loadMoreHtml);

                $('#btn-load-more-explore').off('click.loadMore').on('click.loadMore', (e) => {
                    e.preventDefault();
                    this.renderBooks(this.filteredBooks, true);
                });
            }
        }

        normalizeText(text) {
            return (text || '').toString().toLowerCase().replace(/\s+/g, ' ').trim();
        }

        generateDynamicFilters(books) {
            if (!books) return;

            const authorSet = new Set();
            const genreSet = new Set();
            const publisherSet = new Set();
            const years = new Set();

            books.forEach(b => {
                const authorsStr = b.authors || b.Authors || 'Unknown Author';
                authorsStr.split(/,+/).forEach(a => {
                    const clean = a.trim();
                    if (clean) authorSet.add(clean);
                });

                if (b.categoryName) genreSet.add(b.categoryName);

                const pub = b.publisher || b.Publisher || 'Unknown Publisher';
                publisherSet.add(pub.trim());

                const yr = b.publishYear || b.PublishYear;
                if (yr) years.add(yr);
            });

            const authors = [...authorSet].sort();
            const genres = [...genreSet].sort();
            const publishers = [...publisherSet].sort();
            const sortedYears = [...years].sort((a, b) => b - a);

            const visibleCount = 5;

            const renderFilterList = ($list, items, prefix) => {
                $list.empty();
                items.forEach((item, idx) => {
                    const isHiddenStyle = idx >= visibleCount ? 'style="display: none;"' : '';
                    const itemClass = idx >= visibleCount ? `filter-checkbox-item hidden-${prefix}-item` : 'filter-checkbox-item';
                    const itemId = `${prefix}-${this.normalizeText(item).replace(/\s+/g, '-')}`;
                    $list.append(`
                        <label class="${itemClass}" ${isHiddenStyle} style="display: block; margin-bottom: 8px; cursor: pointer; font-size: 0.95rem; color: #555;">
                            <input type="checkbox" id="${itemId}" value="${item}" style="margin-right: 8px;"> ${item}
                        </label>
                    `);
                });
                if (items.length > visibleCount) {
                    $list.append(`
                        <a href="javascript:void(0)" class="view-more-link filter-show-more" data-target="${prefix}" style="color: #C2185B; font-weight: 600; font-size: 0.9rem; text-decoration: none; display: inline-block; margin-top: 8px; transition: color 0.2s;">
                            + View More (${items.length - visibleCount})
                        </a>
                    `);
                }
            };

            renderFilterList($(this.selectors.authorList), authors, 'author');
            renderFilterList($(this.selectors.genreList), genres, 'genre');
            renderFilterList($(this.selectors.publisherList), publishers, 'publisher');

            const $fromList = $(this.selectors.yearFromList);
            const $toList = $(this.selectors.yearToList);
            $fromList.empty();
            $toList.empty();
            sortedYears.forEach(yr => {
                $fromList.append(`<li>${yr}</li>`);
                $toList.append(`<li>${yr}</li>`);
            });
        }

        initPriceSlider() {
            const accentColor = '#EEC7C9';
            const $minRange = $(this.selectors.priceMin);
            const $maxRange = $(this.selectors.priceMax);
            const $minInput = $(this.selectors.priceMinInput);
            const $maxInput = $(this.selectors.priceMaxInput);
            const $tooltipMin = $(this.selectors.tooltipMin);
            const $tooltipMax = $(this.selectors.tooltipMax);
            const $track = $(this.selectors.priceTrack);
            const maxRangeValue = 1000;

            if (!$minRange.length || !$maxRange.length) return;

            const updateSlider = (e) => {
                let minVal = parseInt($minRange.val()) || 0;
                let maxVal = parseInt($maxRange.val()) || 0;
                const movingId = e && e.target ? $(e.target).attr('id') : '';

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
            };

            const handleManualInput = (e) => {
                const target = $(e.target);
                let val = parseInt(target.val()) || 0;
                const clampedVal = Math.min(Math.max(val, 0), maxRangeValue);
                const movingId = target.attr('id') || '';

                if (movingId.includes('min')) {
                    const currentMax = parseInt($maxRange.val()) || maxRangeValue;
                    $minRange.val(Math.min(clampedVal, currentMax));
                } else {
                    const currentMin = parseInt($minRange.val()) || 0;
                    $maxRange.val(Math.max(clampedVal, currentMin));
                }

                updateSlider();
                target.val(clampedVal);
            };

            $minRange.off('input.explore').on('input.explore', updateSlider);
            $maxRange.off('input.explore').on('input.explore', updateSlider);
            $minInput.off('input.explore').on('input.explore', handleManualInput);
            $maxInput.off('input.explore').on('input.explore', handleManualInput);

            updateSlider();
        }

        initCustomDropdowns() {
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

        initShowMoreToggles() {
            $(this.selectors.sidebar).off('click.showMore', '.view-more-link').on('click.showMore', '.view-more-link', function (e) {
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
    }

    window.ExploreView = ExploreView;
})(window, window.jQuery);
