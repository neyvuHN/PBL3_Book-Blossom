/**
 * BOOKBLOSSOM ADMIN MESSAGES CONTROLLER (MVC PATTERN)
 * Binds UI events, triggers state updates in Model, and instructs View to render updates.
 */
class AdminMessagesController {
    constructor(model, view) {
        this.model = model;
        this.view = view;

        // Attachment files (stored locally in controller state)
        this.attachedFiles = [];
        this.selectedBook = null;

        // Search message state
        this.searchResults = [];
        this.currentSearchIndex = -1;
    }

    async init() {
        console.log("Admin Messages MVC Controller initialized.");
        await this.loadConversations();
        await this.loadSupportRequests();
        this.bindEvents();
        this.checkUrlParameters();
    }

    async loadConversations() {
        try {
            const convos = await this.model.fetchConversations();
            this.view.renderConversations(convos, this.model.activeConversationId);
        } catch (err) {
            console.error("Failed to load conversations:", err);
        }
    }

    async loadSupportRequests() {
        try {
            const reqs = await this.model.fetchSupportRequests();
            this.view.renderSupportRequests(reqs);
        } catch (err) {
            console.error("Failed to load support requests:", err);
        }
    }

    async selectConversation(convoId, buyerName, buyerAvatar) {
        this.model.activeConversationId = convoId;
        this.view.$chatMainArea.fadeIn(200);
        this.view.$reqDetailArea.hide();

        // 1. Instantly set basic header & profile info
        this.view.renderBuyerProfile({
            buyerName: buyerName,
            avatar: buyerAvatar,
            joinedDate: "Loading...",
            totalOrders: "...",
            totalSpent: 0,
            defaultAddress: "Loading address...",
            phoneNumber: "Loading...",
            email: "Loading..."
        });

        // 2. Fetch full real profile from DB
        try {
            const profile = await this.model.fetchBuyerProfile(convoId);
            this.view.renderBuyerProfile(profile);
        } catch (err) {
            console.error("Failed to load buyer profile details:", err);
        }

        // 3. Fetch chat messages
        try {
            const messages = await this.model.fetchMessages(convoId);
            this.view.renderMessages(messages);
            await this.loadConversations(); // Reload sidebar to update unread count indicators
        } catch (err) {
            console.error("Failed to load messages:", err);
        }
    }

    bindEvents() {
        // Tab switching
        this.view.$tabBtns.on('click', (e) => {
            const $btn = $(e.currentTarget);
            this.view.$tabBtns.removeClass('active');
            $btn.addClass('active');

            const targetId = $btn.data('target');
            this.view.$tabContents.hide().removeClass('active');
            $(targetId).fadeIn(200).addClass('active');
        });

        // Convo item click selection
        this.view.$convoList.on('click', '.convo-item', (e) => {
            const $item = $(e.currentTarget);
            this.view.$convoList.find('.convo-item').removeClass('active');
            $item.addClass('active');

            const convoId = $item.data('convo-id');
            const buyerName = $item.data('buyer-name');
            const buyerAvatar = $item.data('buyer-avatar');

            this.selectConversation(convoId, buyerName, buyerAvatar);
        });

        // Search conversation list
        this.view.$convoSearch.on('input', (e) => {
            const query = $(e.target).val().toLowerCase().trim();
            this.view.$convoList.find('.convo-item').each(function() {
                const name = $(this).find('.convo-name').text().toLowerCase();
                const preview = $(this).find('.convo-preview').text().toLowerCase();
                
                if (name.includes(query) || preview.includes(query)) {
                    $(this).show();
                } else {
                    $(this).hide();
                }
            });
        });

        // Support request list selection click
        this.view.$requestList.on('click', '.request-item', (e) => {
            const $item = $(e.currentTarget);
            const reqId = $item.data('req-id');
            this.model.activeSupportRequestId = reqId;

            const category = $item.find('.req-type').text().trim();
            const buyerName = $item.find('.req-buyer').text().trim();
            const note = $item.find('.req-preview').text().trim();
            const phone = $item.find('.req-phone').text().trim().replace(' ', '');
            const statusName = $item.find('.req-status').text().trim();
            const statusClass = $item.find('.req-status').attr('class');

            this.view.renderSupportRequestDetails({
                requestID: reqId,
                customerName: buyerName,
                categoryName: category,
                note: note,
                phoneNumber: phone,
                status: statusClass.includes('inprogress') ? 1 : (statusClass.includes('resolved') ? 2 : 0),
                statusName: statusName
            });
        });

        // Back button from request details to main chat
        this.view.$btnBackFromReq.on('click', () => {
            this.view.$reqDetailArea.hide();
            this.view.$chatMainArea.fadeIn(200);
        });

        // Message Buyer button click (from Support Request details)
        this.view.$btnSrChat.on('click', async () => {
            const reqId = this.model.activeSupportRequestId;
            const buyerName = this.view.$detailReqBuyer.text().trim();

            if (reqId) {
                try {
                    await this.model.setSupportRequestInProgress(reqId);
                    await this.loadSupportRequests();
                } catch (err) {
                    console.error("Failed to update status to In Progress:", err);
                }
            }

            // Switch back to chat panel
            this.view.$reqDetailArea.hide();
            this.view.$chatMainArea.fadeIn(200);

            // Switch tab to Conversations
            this.view.$tabBtns.filter('[data-target="#conversations-tab"]').click();

            // Find matching convo list item
            const $convoItem = this.view.$convoList.find('.convo-item').filter((idx, el) => {
                return $(el).data('buyer-name').toString().toLowerCase() === buyerName.toLowerCase();
            });

            if ($convoItem.length > 0) {
                $convoItem.first().click();
            } else {
                // Fallback sync placeholders if no conversation exists
                this.view.renderBuyerProfile({
                    buyerName: buyerName,
                    avatar: '/images/Avatar/default.png',
                    joinedDate: 'Joined: N/A',
                    totalOrders: '0',
                    totalSpent: 0,
                    defaultAddress: 'No address registered',
                    phoneNumber: 'N/A',
                    email: 'N/A'
                });
            }

            // Append System Info Message into the stream
            this.view.$chatStream.append(`
                <div class="chat-date-separator">
                    <span>System</span>
                </div>
                <div style="text-align: center; color: #888; font-size: 0.8rem; margin: 10px 0;">
                    Chat initiated from Support Request: ${this.view.$detailReqCategory.text()}
                </div>
            `);
            this.view.scrollToBottom();
        });

        // Support request resolve modal actions
        this.view.$btnConfirmResolve.on('click', async () => {
            const reqId = this.model.activeSupportRequestId;
            if (reqId) {
                try {
                    await this.model.resolveSupportRequest(reqId);
                    this.view.$detailReqStatus.removeClass().addClass('status-resolved').text('Resolved');
                    await this.loadSupportRequests();
                    this.view.$confirmResolveModal.fadeOut(200).removeClass('active');
                } catch (err) {
                    alert("Failed to resolve call request: " + (err.message || err));
                    this.view.$confirmResolveModal.fadeOut(200).removeClass('active');
                }
            }
        });

        // Modal triggers
        $('.btn-sr-resolve').on('click', (e) => {
            e.preventDefault();
            if (this.model.activeSupportRequestId) {
                this.view.$confirmResolveModal.fadeIn(200).addClass('active').css('display', 'flex');
            }
        });

        this.view.$btnCancelResolve.on('click', () => {
            this.view.$confirmResolveModal.fadeOut(200).removeClass('active');
        });

        // --- Attachment / Tagging Actions ---

        // Media Input Trigger click
        this.view.$mediaTrigger.on('click', () => {
            this.view.$mediaInput.click();
        });

        // Media files attachment selection
        this.view.$mediaInput.on('change', (e) => {
            const files = Array.from(e.target.files || []);
            files.forEach(file => {
                const reader = new FileReader();
                reader.onload = (event) => {
                    this.attachedFiles.push({
                        name: file.name,
                        type: file.type,
                        data: event.target.result
                    });
                    this.view.renderMediaPreviews(this.attachedFiles);
                };
                reader.readAsDataURL(file);
            });
            // Clear input so same file can be selected again
            this.view.$mediaInput.val('');
        });

        // Remove media attachment preview click
        this.view.$mediaPreviewBar.on('click', '.btn-remove-media-preview', (e) => {
            const idx = parseInt($(e.currentTarget).data('index'), 10);
            if (!isNaN(idx)) {
                this.attachedFiles.splice(idx, 1);
                this.view.renderMediaPreviews(this.attachedFiles);
            }
        });

        // Tag Product Modal confirmation click
        $(document).on('click', '#btn-confirm-tag-book', () => {
            const $selectedRow = $('#admin-tag-book-modal .book-select-item.selected');
            if ($selectedRow.length > 0) {
                this.selectedBook = {
                    id: $selectedRow.data('id'),
                    title: $selectedRow.data('title'),
                    author: $selectedRow.data('author'),
                    img: $selectedRow.data('img'),
                    link: $selectedRow.data('link')
                };
                this.view.renderTaggedBookPreview(this.selectedBook);
            }
            $('#admin-tag-book-modal').fadeOut(200).removeClass('active');
        });

        // Clear tagged book selection click
        this.view.$taggedBookPreviewBar.on('click', '#btn-admin-clear-tag', () => {
            this.selectedBook = null;
            this.view.renderTaggedBookPreview(null);
        });

        // --- Message sending triggers ---

        const handleSendMessageSubmit = async () => {
            if (!this.model.activeConversationId) return;

            const textContent = this.view.$messageInput.val().trim();
            const mediaUrls = this.attachedFiles.map(f => f.data);
            const bookId = this.selectedBook ? this.selectedBook.id : null;

            if (!textContent && mediaUrls.length === 0 && !bookId) return;

            const dto = {
                conversationID: this.model.activeConversationId,
                content: textContent,
                attachmentUrls: mediaUrls,
                attachedBookID: bookId
            };

            try {
                const newMsg = await this.model.sendMessage(dto);
                if (newMsg) {
                    // Re-fetch all messages to update chat stream
                    const messages = await this.model.fetchMessages(this.model.activeConversationId);
                    this.view.renderMessages(messages);
                    
                    // Clear inputs & local attachments state
                    this.view.clearInput();
                    this.attachedFiles = [];
                    this.selectedBook = null;
                    
                    await this.loadConversations();
                }
            } catch (err) {
                alert("Failed to send message: " + (err.message || err));
            }
        };

        this.view.$btnSend.on('click', handleSendMessageSubmit);

        this.view.$messageInput.on('keypress', (e) => {
            if (e.which === 13) {
                e.preventDefault();
                handleSendMessageSubmit();
            }
        });

        // --- SPA Chat Message Text Search ---

        this.view.$btnToggleSearch.on('click', () => {
            this.view.$chatSearchBar.slideToggle(200, () => {
                if (this.view.$chatSearchBar.is(':visible')) {
                    this.view.$chatSearchInput.focus();
                } else {
                    this.clearSearchHighlights();
                }
            });
        });

        this.view.$chatSearchInput.on('input', () => {
            this.performChatSearch();
        });

        this.view.$btnSearchPrev.on('click', () => {
            this.scrollSearchMatch(-1);
        });

        this.view.$btnSearchNext.on('click', () => {
            this.scrollSearchMatch(1);
        });

        this.view.$btnCloseSearch.on('click', () => {
            this.view.$chatSearchBar.slideUp(200);
            this.clearSearchHighlights();
        });

        // --- Media attachments click / zoom triggers ---

        this.view.$chatStream.on('click', '.chat-media-click', (e) => {
            const src = $(e.currentTarget).attr('src');
            // Reuse the existing galleryModal in shared layout if present
            const $modal = $('#galleryModal');
            if ($modal.length > 0) {
                $modal.find('#galleryImg').attr('src', src);
                $modal.css('display', 'block');
            } else {
                alert("Gallery preview modal element was not found in layout.");
            }
        });

        // Shared materials navigation
        $('#btn-admin-view-shared-pictures').on('click', () => {
            const $modal = $('#admin-shared-pictures-modal');
            if ($modal.length > 0) {
                $modal.fadeIn(200).addClass('active').css('display', 'flex');
                if (typeof window.loadAdminSharedPictures === 'function') {
                    window.loadAdminSharedPictures(this.model.activeConversationId);
                }
            }
        });

        $('#btn-admin-view-shared-links').on('click', () => {
            const $modal = $('#admin-shared-links-modal');
            if ($modal.length > 0) {
                $modal.fadeIn(200).addClass('active').css('display', 'flex');
                if (typeof window.loadAdminSharedBookLinks === 'function') {
                    window.loadAdminSharedBookLinks(this.model.activeConversationId);
                }
            }
        });
    }

    checkUrlParameters() {
        const urlParams = new URLSearchParams(window.location.search);
        const buyerFromUrl = urlParams.get('buyer');
        if (buyerFromUrl) {
            // Switch left tab to Conversations
            this.view.$tabBtns.filter('[data-target="#conversations-tab"]').click();

            // Try to find convo-item
            setTimeout(() => {
                const $convoItem = this.view.$convoList.find('.convo-item').filter((idx, el) => {
                    return $(el).data('buyer-name').toString().toLowerCase() === buyerFromUrl.toLowerCase();
                });

                if ($convoItem.length > 0) {
                    $convoItem.first().click();
                } else {
                    // Placeholders sync
                    this.view.renderBuyerProfile({
                        buyerName: buyerFromUrl,
                        avatar: '/images/Avatar/default.png',
                        joinedDate: 'Joined: N/A',
                        totalOrders: '0',
                        totalSpent: 0,
                        defaultAddress: 'No address registered',
                        phoneNumber: 'N/A',
                        email: 'N/A'
                    });
                }
            }, 300);
        }
    }

    performChatSearch() {
        this.clearSearchHighlights();
        const query = this.view.$chatSearchInput.val().toLowerCase().trim();
        if (!query) {
            this.view.$chatSearchResultsCount.text('0 matches');
            return;
        }

        const self = this;
        const matches = [];
        this.view.$chatStream.find('.msg-text-bubble').each(function() {
            const $bubble = $(this);
            const rawText = $bubble.text();
            const lowerText = rawText.toLowerCase();

            if (lowerText.includes(query)) {
                matches.push($bubble);
                
                // Highlight matches using regex replace
                const regex = new RegExp(`(${query.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&')})`, 'gi');
                const newHtml = rawText.replace(regex, '<span class="chat-search-highlight" style="background-color: #ffeb3b; color: #000; font-weight: 700; padding: 1px 4px; border-radius: 4px; box-shadow: 0 1px 3px rgba(0,0,0,0.15);">$1</span>');
                $bubble.html(newHtml);
            }
        });

        this.searchResults = matches;
        this.currentSearchIndex = matches.length > 0 ? 0 : -1;
        this.view.$chatSearchResultsCount.text(`${matches.length} matches`);

        if (this.currentSearchIndex !== -1) {
            this.scrollSearchMatch(0);
        }
    }

    scrollSearchMatch(direction) {
        if (this.searchResults.length === 0) return;

        // Clear active borders
        this.view.$chatStream.find('.chat-search-highlight').css('border', 'none');

        this.currentSearchIndex = (this.currentSearchIndex + direction + this.searchResults.length) % this.searchResults.length;

        const $activeBubble = this.searchResults[this.currentSearchIndex];
        const stream = document.getElementById('admin-chat-stream');
        const $stream = $('#admin-chat-stream');

        if (stream && $activeBubble) {
            const targetScrollTop = $activeBubble.offset().top - $stream.offset().top + $stream.scrollTop() - 40;
            stream.scrollTo({
                top: targetScrollTop,
                behavior: 'smooth'
            });

            // Put a small outline on matching text
            $activeBubble.find('.chat-search-highlight').css('border', '2px solid #C2185B');
        }
    }

    clearSearchHighlights() {
        this.searchResults = [];
        this.currentSearchIndex = -1;
        this.view.$chatSearchResultsCount.text('0 matches');

        this.view.$chatStream.find('.msg-text-bubble').each(function() {
            const $bubble = $(this);
            // Replace highlighted tags back to clean text
            const rawText = $bubble.text();
            $bubble.text(rawText);
        });
    }
}

// Attach to window namespace for global access
window.AdminMessagesController = AdminMessagesController;
