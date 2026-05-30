(function (window, document, $) {
    if (!$) {
        console.error('blind-date.js requires jQuery.');
        return;
    }

    $(document).ready(function () {
        initBlindDatePage();
        initMatchmakerSidebar();
        initBlindPriceSlider();
        initBlindFilters();
        initHowItWorks();
        initBlindCards();
    });

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
        $('.vibe-tag').off('click.blindDate').on('click.blindDate', function () {
            $(this).toggleClass('active');
            applyBlindFilters();
        });

        $('#blind-sidebar input[type="checkbox"]')
            .off('change.blindDate')
            .on('change.blindDate', applyBlindFilters);

        $('.clear-filters').off('click.blindDate').on('click.blindDate', function (e) {
            e.preventDefault();

            $('#blind-sidebar input[type="checkbox"]').prop('checked', false);
            $('#blind-sidebar .vibe-tag').removeClass('active');

            $('#blind-price-min').val(0).trigger('input');
            $('#blind-price-max').val(1000).trigger('input');

            $('.blind-card').show();
        });
    }

    function applyBlindFilters() {
        const activeTags = $('#blind-sidebar .vibe-tag.active').map(function () {
            return normalizeText($(this).text());
        }).get();

        const checkedKeywords = $('#blind-sidebar input[type="checkbox"]:checked').map(function () {
            return normalizeText($(this).closest('label').text());
        }).get();

        $('.blind-card').each(function () {
            const cardText = normalizeText($(this).text());

            const tagMatch =
                activeTags.length === 0 ||
                activeTags.some(tag => cardText.includes(tag.replace('#', '')));

            const keywordMatch =
                checkedKeywords.length === 0 ||
                checkedKeywords.some(keyword => {
                    return cardText.includes(keyword.replace(/[^\w\s#-]/g, '').trim());
                });

            if (tagMatch && keywordMatch) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
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
            .on('click.blindCard', '.blind-card', function (e) {
                e.preventDefault();

                const blindBookData = getBlindBookDataFromCard($(this));

                if (window.BookBlossomBlindDateDetails) {
                    window.BookBlossomBlindDateDetails.show(blindBookData);
                }
            });
    }

    function getBlindBookDataFromCard($card) {
        const imgSrc = $card.find('.blind-card-image img').attr('src') || '/images/BlindDateBook/BlindBook.jpg';
        const hashtags = $card.find('.card-hashtags').text().trim();
        const desc = $card.find('.card-desc').text().trim();
        const price = $card.find('.card-price').text().trim();
        const condition = $card.find('.card-condition').text().replace('Condition:', '').trim();

        let firstLine = 'The clock struck thirteen, and I knew I was in trouble...';
        let rating = '4.2';
        let ratingRange = '4.0 - 4.2';
        let year = '1994';
        let category = 'Mystery & Thriller';
        let keywords = 'Intriguing, suspenseful, ancient puzzles, dark secrets';

        if (hashtags.includes('Paris') || hashtags.includes('SlowBurn')) {
            firstLine = 'The dough was cold, but their glances were fiery.';
            rating = '4.3';
            ratingRange = '4.1 - 4.4';
            year = '2018';
            category = 'Romantic Fiction';
            keywords = 'Sweet, slow-burn romance, culinary baking, enemies-to-lovers, Parisian vibe';
        } else if (hashtags.includes('SpaceOpera') || hashtags.includes('AI')) {
            firstLine = 'The stars did not welcome us; they watched us in silence.';
            rating = '4.6';
            ratingRange = '4.4 - 4.7';
            year = '2021';
            category = 'Science Fiction';
            keywords = 'Deep space expedition, alien technology, emotional AI companion, space opera epic';
        } else if (hashtags.includes('FamilySaga') || hashtags.includes('Literary')) {
            firstLine = "Grandmother's cedar chest smelled of lavender and unspoken truths.";
            rating = '4.1';
            ratingRange = '3.9 - 4.2';
            year = '1998';
            category = 'Literary Fiction & Drama';
            keywords = 'Emotional drama, multi-generational secrets, female-led narrative, beautiful prose';
        }

        return {
            imgSrc,
            hashtags,
            desc,
            price,
            condition,
            firstLine,
            rating,
            ratingRange,
            year,
            category,
            keywords
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