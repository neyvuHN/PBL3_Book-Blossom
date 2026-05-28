(function (window, document, $) {
    if (!$) {
        console.error('book-preview.js requires jQuery.');
        return;
    }

    let currentSheetIndex = 0;
    const maxSheets = 3;
    let currentZoomScale = 1.0;
    let pdfDoc = null;
    let pdfBlobUrl = null;

    /* 
     * UPDATED AREA: Integrated PDF.js digital secure streaming.
     * Fetches secure signed URL session token, reads dynamically based on book cover match,
     * and streams file bytes securely using PDF.js context rendering.
     */
    function openPreview(options) {
        options = options || {};

        const title = options.title || 'Selected Book';

        const $modal = $('#preview-modal-container');

        if (!$modal.length) {
            showToast('Preview modal DOM is missing.');
            return;
        }

        showToast(`Opening preview for "${title}"...`);

        $('body').css('overflow', 'hidden');

        $('.flipbook-sheet').removeClass('flipped');
        currentSheetIndex = 0;

        setZoomScale(1.0);
        updateSheetZIndices();
        updatePreviewControls();

        // Extract PDF filename from current main cover image dynamically (e.g. book1.jpg -> Book1)
        let bookId = 'Book1'; // fallback default
        const match = (options.coverSrc || '').match(/book(\d+)/i);
        if (match) {
            bookId = 'Book' + match[1];
        }

        $modal.fadeIn(300);

        // Load secure digital PDF pages from backend
        loadSecurePreview(bookId);
    }

    // Load and render a specific page using PDF.js onto the target canvas
    async function renderPreviewPage(pdf, pageNum, canvasId, loaderId) {
        try {
            const page = await pdf.getPage(pageNum);
            
            // Render page at high quality (1.5x scale)
            const viewport = page.getViewport({ scale: 1.5 });
            const canvas = document.getElementById(canvasId);
            if (!canvas) return;
            
            const context = canvas.getContext('2d');
            canvas.height = viewport.height;
            canvas.width = viewport.width;

            const renderContext = {
                canvasContext: context,
                viewport: viewport
            };
            
            await page.render(renderContext).promise;
            
            // Hide loader once page is successfully rendered
            $(`#${loaderId}`).fadeOut(200);
        } catch (error) {
            console.error(`Error rendering page ${pageNum}:`, error);
            $(`#${loaderId}`).html(`<span style="color: #ff4444;"><i class="fas fa-exclamation-triangle"></i> Failed to load page ${pageNum}</span>`);
        }
    }

    // Load PDF document from secure signed URL
    async function loadSecurePreview(bookId) {
        // Show loading states for all sheets
        $('.page-loader').show();
        $('.page-loader').siblings('canvas').each(function() {
            const ctx = this.getContext('2d');
            if (ctx) {
                ctx.clearRect(0, 0, this.width, this.height);
            }
        });

        // Generate watermark overlays
        renderWatermarks();

        try {
            // Step 1: Call secure backend session to get signed URL
            const sessionResponse = await $.getJSON(`/api/books/${bookId}/preview-session`);
            const signedUrl = sessionResponse.signedUrl;

            // Step 2: Fetch PDF securely using the signed URL (60 seconds limit)
            const response = await fetch(signedUrl);
            if (!response.ok) {
                if (response.status === 403) {
                    throw new Error('Access Denied: The preview token is invalid or has expired.');
                }
                throw new Error('Failed to download secure preview.');
            }

            const blob = await response.blob();
            pdfBlobUrl = URL.createObjectURL(blob);

            // Step 3: Initialize PDF.js rendering
            pdfjsLib.GlobalWorkerOptions.workerSrc = 'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/2.16.105/pdf.worker.min.js';
            const loadingTask = pdfjsLib.getDocument(pdfBlobUrl);
            
            pdfDoc = await loadingTask.promise;
            
            // Revoke URL immediately after document is loaded in memory for absolute security!
            URL.revokeObjectURL(pdfBlobUrl);
            pdfBlobUrl = null;

            // Render each of the first 5 pages concurrently
            const totalPages = Math.min(pdfDoc.numPages, 5);
            const renderPromises = [];
            
            for (let p = 1; p <= totalPages; p++) {
                renderPromises.push(renderPreviewPage(pdfDoc, p, `canvas-page-${p}`, `loader-${p}`));
            }
            
            await Promise.all(renderPromises);
        } catch (error) {
            console.error('Secure preview error:', error);
            showToast(error.message || 'An error occurred while opening the preview.');
            closePreview();
        }
    }

    function renderWatermarks() {
        $('.watermark-overlay').each(function() {
            const $overlay = $(this);
            $overlay.empty();
            for (let i = 0; i < 6; i++) {
                $overlay.append('<div class="watermark-text">BookBlossom Preview</div>');
            }
        });
    }

    function updateSheetZIndices() {
        const sheets = [
            { el: $('#p-sheet-1'), index: 1 },
            { el: $('#p-sheet-2'), index: 2 },
            { el: $('#p-sheet-3'), index: 3 }
        ];

        sheets.forEach(function (sheet) {
            if (!sheet.el.length) return;

            if (sheet.el.hasClass('flipped')) {
                sheet.el.css('z-index', sheet.index);
            } else {
                sheet.el.css('z-index', maxSheets - sheet.index + 1);
            }
        });
    }

    function updatePreviewControls() {
        $('#btn-prev-page').prop('disabled', currentSheetIndex === 0);
        $('#btn-next-page').prop('disabled', currentSheetIndex === maxSheets);

        if (currentSheetIndex === 0) {
            $('#preview-page-indicator').text('Cover');
        } else if (currentSheetIndex === 1) {
            $('#preview-page-indicator').text('Pages 2 - 3');
        } else if (currentSheetIndex === 2) {
            $('#preview-page-indicator').text('Pages 4 - 5');
        } else {
            $('#preview-page-indicator').text('End');
        }
    }

    function flipNext() {
        if (currentSheetIndex >= maxSheets) return;

        const nextSheet = currentSheetIndex + 1;

        $(`#p-sheet-${nextSheet}`).addClass('flipped');

        setTimeout(updateSheetZIndices, 400);

        currentSheetIndex = nextSheet;
        updatePreviewControls();
    }

    function flipPrev() {
        if (currentSheetIndex <= 0) return;

        const prevSheet = currentSheetIndex;

        $(`#p-sheet-${prevSheet}`).removeClass('flipped');

        updateSheetZIndices();

        currentSheetIndex = prevSheet - 1;
        updatePreviewControls();
    }

    /* 
     * UPDATED AREA: Reset secure PDF document state upon modal closure.
     */
    function closePreview() {
        $('#preview-modal-container').fadeOut(300, function () {
            pdfDoc = null;
            currentSheetIndex = 0;

            $('.flipbook-sheet').removeClass('flipped');

            updateSheetZIndices();
            updatePreviewControls();

            $('body').css('overflow', '');

            setZoomScale(1.0);
        });
    }

    /* 
     * UPDATED AREA: Toggles .high-zoom class on controls depending on zoom scale to trigger solid highlights.
     */
    function setZoomScale(scale) {
        currentZoomScale = Math.min(Math.max(scale, 0.8), 3.0);

        $('#flipbook-container').css('transform', `scale(${currentZoomScale})`);
        $('#zoom-indicator').text(`${Math.round(currentZoomScale * 100)}%`);

        const baseWidth = 840;
        const baseHeight = 560;

        $('#flipbook-zoom-wrapper').css({
            width: (baseWidth * currentZoomScale) + 'px',
            height: (baseHeight * currentZoomScale) + 'px'
        });

        // Toggle elegant high-contrast class on control bar to avoid background blending
        if (currentZoomScale >= 1.0) {
            $('.preview-controls').addClass('high-zoom');
        } else {
            $('.preview-controls').removeClass('high-zoom');
        }

        $('#btn-zoom-in').prop('disabled', currentZoomScale >= 3.0);
        $('#btn-zoom-out').prop('disabled', currentZoomScale <= 0.8);
    }

    function bindEvents() {
        $(document)
            .off('click.bookPreviewNext')
            .on('click.bookPreviewNext', '#btn-next-page', function (e) {
                e.preventDefault();
                flipNext();
            });

        $(document)
            .off('click.bookPreviewPrev')
            .on('click.bookPreviewPrev', '#btn-prev-page', function (e) {
                e.preventDefault();
                flipPrev();
            });

        $(document)
            .off('click.bookPreviewZoomIn')
            .on('click.bookPreviewZoomIn', '#btn-zoom-in', function (e) {
                e.preventDefault();
                setZoomScale(currentZoomScale + 0.15);
            });

        $(document)
            .off('click.bookPreviewZoomOut')
            .on('click.bookPreviewZoomOut', '#btn-zoom-out', function (e) {
                e.preventDefault();
                setZoomScale(currentZoomScale - 0.15);
            });

        $(document)
            .off('click.bookPreviewClose')
            .on('click.bookPreviewClose', '#btn-close-preview, .preview-modal-backdrop', function (e) {
                e.preventDefault();
                closePreview();
            });

        $(document)
            .off('click.bookPreviewSheet')
            .on('click.bookPreviewSheet', '.flipbook-sheet', function (e) {
                if ($(e.target).closest('button').length > 0) return;

                const sheetId = $(this).attr('id') || '';
                const sheetNumber = parseInt(sheetId.replace('p-sheet-', ''), 10);

                if (sheetNumber === currentSheetIndex + 1) {
                    flipNext();
                } else if (sheetNumber === currentSheetIndex) {
                    flipPrev();
                }
            });

        $(document)
            .off('click.bookPreviewBuy')
            .on('click.bookPreviewBuy', '#btn-buy-from-preview', function (e) {
                e.preventDefault();
                closePreview();
                $('#btn-detail-buy').trigger('click');
            });

        $(document)
            .off('keydown.bookPreview')
            .on('keydown.bookPreview', function (e) {
                if (!$('#preview-modal-container').is(':visible')) return;

                if (e.key === 'Escape') {
                    closePreview();
                } else if (e.key === 'ArrowLeft') {
                    flipPrev();
                } else if (e.key === 'ArrowRight') {
                    flipNext();
                }
            });

        $(document)
            .off('contextmenu.bookPreview')
            .on('contextmenu.bookPreview', '#preview-modal-container', function (e) {
                e.preventDefault();
                showToast('Security Action: Right-click is restricted to protect content.');
                return false;
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

    $(document).ready(function () {
        bindEvents();
    });

    window.BookBlossomBookPreview = {
        open: openPreview,
        close: closePreview
    };
})(window, document, window.jQuery);