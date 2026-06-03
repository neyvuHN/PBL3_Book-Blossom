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
        this.initSignalR();
    }

    initSignalR() {
        if (typeof signalR === 'undefined') {
            console.warn("SignalR is not loaded.");
            return;
        }

        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl("/chatHub")
            .withAutomaticReconnect()
            .build();
            
        this.hubConnection.on("ReceiveMessage", (message) => {
            console.log("SignalR ReceiveMessage:", message);
            const msgConvoId = message.conversationID || message.conversationId;
            // Check if message belongs to currently active conversation
            if (this.model.activeConversationId?.toString() === msgConvoId?.toString()) {
                // If it's my own message from another session or we already added it locally, this might duplicate,
                // but SendMessage re-fetches all anyway. Let's just append it.
                this.view.renderMessages([message], true);
            }
            
            // Reload conversations to update snippet and unread status
            this.loadConversations();
        });
        
        this.hubConnection.start().then(() => {
            console.log("SignalR Connected Successfully!");
            if (this.model.activeConversationId) {
                this.hubConnection.invoke("JoinConversation", this.model.activeConversationId)
                    .then(() => console.log("Joined conversation group:", this.model.activeConversationId))
                    .catch(console.error);
            }
        }).catch(err => console.error("SignalR connection error:", err));
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
        if (this.model.activeConversationId && this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
            this.hubConnection.invoke("LeaveConversation", this.model.activeConversationId).catch(console.error);
        }

        this.model.activeConversationId = convoId;
        this.view.$chatMainArea.fadeIn(200);
        this.view.$reqDetailArea.hide();

        if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
            this.hubConnection.invoke("JoinConversation", convoId)
                .then(() => console.log("Joined conversation group:", convoId))
                .catch(console.error);
        }

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

        // Tag Book Modal Trigger click
        this.view.$tagBookTrigger.on('click', (e) => {
            e.preventDefault();
            $('#tag-book-modal').fadeIn(200).addClass('active').css('display', 'flex');
            $('#tag-book-link').val('');
            $('#link-error').hide();
            $('#tag-book-modal .book-select-item').removeClass('selected');
            this.loadTagBooksFlow();
        });

        // Tag Book Modal confirmation click
        $(document).on('click', '#btn-confirm-tag-book', async () => {
            const activeTab = $('#tag-book-modal .btn-modal-tab.active').data('target');
            let bookTitle = "", bookAuthor = "", bookImg = "", bookLink = "", bookId = "";

            if (activeTab === 'tab-link') {
                const linkVal = $('#tag-book-link').val().trim();
                if (linkVal === '') {
                    $('#link-error').text("Please enter a valid link.").show();
                    return;
                }
                if (!linkVal.includes('bookblossom.com') && !linkVal.startsWith('/') && !linkVal.includes('#book-details') && !linkVal.includes('#blind-details')) {
                    $('#link-error').text("Link must contain 'bookblossom.com', start with '/', or use hash views like '#book-details-' / '#blind-details-'.").show();
                    return;
                }
                
                if (linkVal.includes('#blind-details-')) {
                    const hashPart = linkVal.split('#blind-details-')[1];
                    bookId = decodeURIComponent(hashPart);
                    bookTitle = "Mystery Blind Book (" + bookId + ")";
                    bookAuthor = "BookBlossom Curated";
                    bookImg = "/images/BlindDateBook/BlindBook.jpg";
                } else if (linkVal.includes('#book-details-') || linkVal.includes('/book/') || linkVal.includes('/Explore')) {
                    let searchTerm = '';
                    if (linkVal.includes('#book-details-')) {
                        searchTerm = decodeURIComponent(linkVal.split('#book-details-')[1]);
                    } else if (linkVal.includes('/book/')) {
                        searchTerm = decodeURIComponent(linkVal.split('/book/')[1]);
                    } else {
                        const lastSeg = linkVal.split('/').pop();
                        searchTerm = decodeURIComponent(lastSeg);
                    }

                    let numericId = parseInt(searchTerm);
                    if (!isNaN(numericId) && numericId.toString() === searchTerm.trim()) {
                        try {
                            const $originalBtn = $('#btn-confirm-tag-book');
                            $originalBtn.prop('disabled', true).text('Loading...');
                            const res = await this.model.fetchBookDetails(numericId);
                            $originalBtn.prop('disabled', false).text('Tag Book');
                            
                            if (res && res.title) {
                                bookId = res.bookID || res.bookId || numericId;
                                bookTitle = res.title;
                                bookAuthor = res.author || res.authors || "BookBlossom Curated";
                                bookImg = res.sampleFilePath && res.sampleFilePath.includes('.') ? res.sampleFilePath : "/images/Book/book1.jpg";
                            } else {
                                $('#link-error').text("Cannot find the book from this link. Please check again.").show();
                                return;
                            }
                        } catch(e) {
                            $('#btn-confirm-tag-book').prop('disabled', false).text('Tag Book');
                            $('#link-error').text("Cannot find the book from this link. Please check again.").show();
                            return;
                        }
                    } else {
                        try {
                            const $originalBtn = $('#btn-confirm-tag-book');
                            $originalBtn.prop('disabled', true).text('Loading...');
                            const books = await window.apiClient.apiGet('/api/RealBook?searchTerm=' + encodeURIComponent(searchTerm));
                            $originalBtn.prop('disabled', false).text('Tag Book');
                            
                            const matchedBook = (books && books.length > 0) ? books[0] : null;
                            if (!matchedBook) {
                                $('#link-error').text("Cannot find the book from this link. Please check again.").show();
                                return;
                            }
                            
                            bookId = matchedBook.bookID || matchedBook.bookId || "";
                            bookTitle = matchedBook.title;
                            bookAuthor = matchedBook.author || matchedBook.authors || "BookBlossom Curated";
                            bookImg = matchedBook.sampleFilePath && matchedBook.sampleFilePath.includes('.') ? matchedBook.sampleFilePath : "/images/Book/book1.jpg";
                        } catch (err) {
                            $('#btn-confirm-tag-book').prop('disabled', false).text('Tag Book');
                            console.error(err);
                            $('#link-error').text("Error searching book from link.").show();
                            return;
                        }
                    }
                } else {
                    bookTitle = "BookBlossom Shared Book";
                    bookAuthor = "Community Curator";
                    bookImg = "/images/Book/book1.jpg";
                }
                bookLink = linkVal;
            } else {
                const $selectedItem = $(`#tag-book-modal #${activeTab} .book-select-item.selected`);
                if ($selectedItem.length === 0) {
                    alert("Please select a book first!");
                    return;
                }
                bookTitle = $selectedItem.data('title');
                bookAuthor = $selectedItem.data('author');
                bookImg = $selectedItem.data('img');
                bookLink = $selectedItem.data('link');
                bookId = $selectedItem.data('id') || "";
            }

            this.selectedBook = {
                id: bookId,
                title: bookTitle,
                author: bookAuthor,
                img: bookImg,
                link: bookLink
            };
            this.view.renderTaggedBookPreview(this.selectedBook);
            $('#tag-book-modal').fadeOut(200).removeClass('active');
        });

        // Tabs toggle inside Tag Book Modal
        $(document).on('click', '#tag-book-modal .btn-modal-tab', function() {
            $('#tag-book-modal .btn-modal-tab').removeClass('active');
            $(this).addClass('active');
            
            const target = $(this).data('target');
            $('#tag-book-modal .modal-tab-content').hide().removeClass('active');
            $('#tag-book-modal #' + target).fadeIn(150).addClass('active');
        });

        // Dismiss Tag Book Modal
        $(document).on('click', '#btn-cancel-tag-book, #tag-book-modal .modal-backdrop', () => {
            $('#tag-book-modal').fadeOut(200).removeClass('active');
        });

        // Select Book inside lists (Delegated)
        $(document).on('click', '#tag-book-modal .book-select-item', function() {
            $('#tag-book-modal .book-select-item').removeClass('selected');
            $(this).addClass('selected');
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
            if (!$(e.currentTarget).attr('id')) {
                $(e.currentTarget).attr('id', 'admin-chat-img-' + Math.random().toString(36).substr(2, 9));
            }
            const targetId = $(e.currentTarget).attr('id');

            $('#lightbox-img').attr('src', src).show();
            $('#lightbox-video').hide();
            $('#btn-lightbox-scroll-to-msg').attr('data-target-id', targetId).show();
            
            $('#chat-image-lightbox').fadeIn(150).css('display', 'flex');
        });

        // Shared materials navigation - Pictures
        $('#btn-admin-view-shared-pictures').on('click', () => {
            const $modal = $('#shared-pictures-modal');
            if ($modal.length === 0) return;

            const $grid = $('#shared-pictures-grid');
            $grid.empty();

            // Assign unique IDs to any untagged images in admin-chat-stream
            $('#admin-chat-stream .chat-media-click').each(function(index) {
                if (!$(this).attr('id')) {
                    $(this).attr('id', 'admin-chat-img-' + index);
                }
            });

            // Assign unique IDs to any untagged videos in admin-chat-stream
            $('#admin-chat-stream video').each(function(index) {
                if (!$(this).attr('id')) {
                    $(this).attr('id', 'admin-chat-video-' + index);
                }
            });

            const $images = $('#admin-chat-stream .chat-media-click');
            const $videos = $('#admin-chat-stream video');

            if ($images.length === 0 && $videos.length === 0) {
                $('#shared-pictures-empty').show();
                $grid.hide();
            } else {
                $('#shared-pictures-empty').hide();
                $grid.show();

                $images.each(function() {
                    const src = $(this).attr('src');
                    const targetId = $(this).attr('id');

                    const itemHtml = `
                        <div class="shared-picture-card" style="position: relative; border-radius: 12px; overflow: hidden; border: 1.5px solid rgba(194, 24, 91, 0.1); background: #fff; box-shadow: 0 4px 15px rgba(0,0,0,0.04); height: 120px; cursor: pointer; transition: all 0.25s;">
                            <img src="${src}" class="shared-grid-img" data-src="${src}" data-target-id="${targetId}" style="width: 100%; height: 100%; object-fit: cover; transition: transform 0.3s ease;">
                            <div class="shared-card-overlay" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; background: linear-gradient(to top, rgba(194, 24, 91, 0.65) 0%, rgba(0,0,0,0) 65%); opacity: 0; transition: opacity 0.25s ease; display: flex; align-items: flex-end; justify-content: flex-end; padding: 10px; box-sizing: border-box;">
                                <button class="btn-scroll-to-msg btn-hover-eye-icon" data-target-id="${targetId}" title="View in Chat" style="background: #fff; color: #C2185B; border: none; width: 34px; height: 34px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 1rem; cursor: pointer; box-shadow: 0 4px 12px rgba(0,0,0,0.2); transform: translateY(10px); transition: all 0.25s; outline: none; padding: 0;">
                                    <i class="fas fa-eye"></i>
                                </button>
                            </div>
                        </div>
                    `;
                    $grid.append(itemHtml);
                });

                $videos.each(function() {
                    const src = $(this).attr('src');
                    const targetId = $(this).attr('id');

                    const itemHtml = `
                        <div class="shared-picture-card" style="position: relative; border-radius: 12px; overflow: hidden; border: 1.5px solid rgba(194, 24, 91, 0.1); background: #000; box-shadow: 0 4px 15px rgba(0,0,0,0.04); height: 120px; cursor: pointer; transition: all 0.25s;">
                            <video src="${src}" class="shared-grid-video" data-src="${src}" data-target-id="${targetId}" style="width: 100%; height: 100%; object-fit: cover; transition: transform 0.3s ease;"></video>
                            <div style="position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); background: rgba(0,0,0,0.55); color: #fff; width: 36px; height: 36px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 1rem; pointer-events: none; z-index: 10;">
                                <i class="fas fa-play"></i>
                            </div>
                            <div class="shared-card-overlay" style="position: absolute; top: 0; left: 0; width: 100%; height: 100%; background: linear-gradient(to top, rgba(194, 24, 91, 0.65) 0%, rgba(0,0,0,0) 65%); opacity: 0; transition: opacity 0.25s ease; display: flex; align-items: flex-end; justify-content: flex-end; padding: 10px; box-sizing: border-box; z-index: 15;">
                                <button class="btn-scroll-to-msg btn-hover-eye-icon" data-target-id="${targetId}" title="View in Chat" style="background: #fff; color: #C2185B; border: none; width: 34px; height: 34px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 1rem; cursor: pointer; box-shadow: 0 4px 12px rgba(0,0,0,0.2); transform: translateY(10px); transition: all 0.25s; outline: none; padding: 0;">
                                    <i class="fas fa-eye"></i>
                                </button>
                            </div>
                        </div>
                    `;
                    $grid.append(itemHtml);
                });
            }

            $modal.fadeIn(200).addClass('active').css('display', 'flex');
        });

        // Shared materials navigation - Book Links
        $('#btn-admin-view-shared-links').on('click', () => {
            const $modal = $('#shared-links-modal');
            if ($modal.length === 0) return;

            const $list = $('#shared-links-list');
            $list.empty();

            const $cards = $('#admin-chat-stream a[href*="#book-details-"], #admin-chat-stream a[href*="#blind-details-"]').closest('div[style*="display: flex; gap: 12px;"]');

            if ($cards.length === 0) {
                $('#shared-links-empty').show();
                $list.hide();
            } else {
                $('#shared-links-empty').hide();
                $list.show();

                const uniqueLinks = new Set();

                $cards.each(function() {
                    const title = $(this).find('h4').text();
                    const img = $(this).find('img').attr('src');
                    const link = $(this).find('a').attr('href');

                    if (uniqueLinks.has(link)) return;
                    uniqueLinks.add(link);

                    const author = $(this).find('p').length > 0 ? $(this).find('p').text() : "BookBlossom Curated";

                    const itemHtml = `
                        <div style="display: flex; gap: 12px; align-items: center; background: #fffafb; padding: 12px; border-radius: 12px; border: 1.5px solid rgba(194, 24, 91, 0.1); box-shadow: 0 2px 8px rgba(0,0,0,0.02); transition: all 0.2s;" onmouseover="this.style.borderColor='#C2185B';" onmouseout="this.style.borderColor='rgba(194, 24, 91, 0.1)';">
                            <img src="${img}" style="width: 40px; height: 55px; object-fit: cover; border-radius: 4px; box-shadow: 0 3px 8px rgba(0,0,0,0.08);">
                            <div style="flex: 1; min-width: 0; text-align: left;">
                                <h4 style="font-size: 0.85rem; font-weight: 700; color: #111; margin: 0 0 2px 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${title}</h4>
                                <p style="font-size: 0.72rem; color: #666; margin: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${author}</p>
                            </div>
                            <a href="${link}" style="font-size: 0.75rem; font-weight: 700; color: #fff; background: #C2185B; padding: 6px 12px; border-radius: 20px; text-decoration: none; display: flex; align-items: center; gap: 4px; transition: background 0.2s;" onmouseover="this.style.background='#a0134a'" onmouseout="this.style.background='#C2185B'">
                                <i class="fas fa-external-link-alt"></i> Go Detail
                            </a>
                        </div>
                    `;
                    $list.append(itemHtml);
                });
            }

            $modal.fadeIn(200).addClass('active').css('display', 'flex');
        });

        // Click on shared grid image/video to view in Lightbox
        $(document).on('click', '.shared-grid-img', function() {
            const src = $(this).data('src');
            const targetId = $(this).data('target-id');
            
            $('#lightbox-img').attr('src', src).show();
            $('#lightbox-video').hide();
            $('#btn-lightbox-scroll-to-msg').attr('data-target-id', targetId).show();
            
            $('#chat-image-lightbox').fadeIn(150).css('display', 'flex');
        });

        $(document).on('click', '.shared-grid-video', function() {
            const src = $(this).data('src');
            const targetId = $(this).data('target-id');
            
            $('#lightbox-img').hide();
            $('#lightbox-video').attr('src', src).show();
            $('#btn-lightbox-scroll-to-msg').attr('data-target-id', targetId).show();
            
            const videoEl = document.getElementById('lightbox-video');
            if (videoEl) {
                videoEl.play().catch(() => {});
            }
            
            $('#chat-image-lightbox').fadeIn(150).css('display', 'flex');
        });

        // Scroll to message in Chat Trigger
        $(document).on('click', '.btn-scroll-to-msg, #btn-lightbox-scroll-to-msg', (e) => {
            e.preventDefault();
            const targetId = $(e.currentTarget).attr('data-target-id') || $(e.currentTarget).data('target-id');
            $('#chat-image-lightbox').fadeOut(150);
            $('#shared-pictures-modal').fadeOut(150).removeClass('active');
            this.view.scrollToMessage(targetId);
        });

        // Hide Lightbox Modal
        $('#chat-image-lightbox').on('click', function(e) {
            if ($(e.target).closest('#btn-lightbox-scroll-to-msg').length > 0 || $(e.target).closest('#lightbox-video').length > 0) {
                return;
            }
            
            const videoEl = document.getElementById('lightbox-video');
            if (videoEl) {
                videoEl.pause();
            }
            
            $(this).fadeOut(150, function() {
                if ($('#shared-pictures-modal').hasClass('active')) {
                    $('#shared-pictures-modal').show();
                }
            });
        });

        // Close modals handler
        $(document).on('click', '.btn-close-shared-modal, #shared-pictures-modal .modal-backdrop, #shared-links-modal .modal-backdrop', function() {
            $(this).closest('.custom-modal').fadeOut(200).removeClass('active');
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

    async loadTagBooksFlow() {
        if (this.model.tagBooksLoaded) return;
        this.model.tagBooksLoaded = true;

        try {
            // Fetch Recent Books
            const resRecent = await this.model.fetchRecentBooks();
            const booksRecent = Array.isArray(resRecent) ? resRecent : (resRecent?.data?.items || resRecent?.data || []);
            $('#recent-books-list').empty();
            booksRecent.slice(0, 15).forEach(book => {
                let imgUrl = (book.bookImages && book.bookImages.length > 0) ? book.bookImages[0].imageUrl : `/images/Book/cover_${book.bookID}.jpg`;
                let item = `
                    <div class="book-select-item" data-id="${book.bookID}" data-title="${this.view.escapeHtml(book.title)}" data-author="${this.view.escapeHtml(book.author || 'Unknown')}" data-img="${this.view.escapeHtml(imgUrl)}" data-link="/Explore#book-details-${encodeURIComponent(book.title)}">
                        <img src="${this.view.escapeHtml(imgUrl)}" alt="Book">
                        <div>
                            <h4>${this.view.escapeHtml(book.title)}</h4>
                            <p>${this.view.escapeHtml(book.author || 'Unknown')} • ₫${book.price ? book.price.toLocaleString('vi-VN') : '0'}</p>
                        </div>
                    </div>
                `;
                $('#recent-books-list').append(item);
            });
            if (booksRecent.length === 0) {
                $('#recent-books-list').html('<div style="text-align: center; padding: 20px; color: #888;">No recent books found.</div>');
            }
        } catch(e) {
            $('#recent-books-list').html('<div style="text-align: center; padding: 20px; color: #ff4444;">Failed to load books.</div>');
        }

        try {
            const resWish = await this.model.fetchWishlist();
            const wishItems = Array.isArray(resWish) ? resWish : (resWish?.data || []);
            $('#wishlist-books-list').empty();
            wishItems.forEach(item => {
                const bookId = item.bookID || item.BookID;
                if (!bookId) return;
                const title = item.title || item.Title || 'Untitled Book';
                const author = item.author || item.Author || 'BookBlossom Selection';
                const price = item.price || item.Price || 0;
                const imgUrl = `/images/Book/cover_${bookId}.jpg`;
                let html = `
                    <div class="book-select-item" data-id="${bookId}" data-title="${this.view.escapeHtml(title)}" data-author="${this.view.escapeHtml(author)}" data-img="${this.view.escapeHtml(imgUrl)}" data-link="/Explore#book-details-${encodeURIComponent(title)}">
                        <img src="${this.view.escapeHtml(imgUrl)}" alt="Book">
                        <div>
                            <h4>${this.view.escapeHtml(title)}</h4>
                            <p>${this.view.escapeHtml(author)} • ₫${price ? price.toLocaleString('vi-VN') : '0'}</p>
                        </div>
                    </div>
                `;
                $('#wishlist-books-list').append(html);
            });
            if (wishItems.length === 0) {
                $('#wishlist-books-list').html('<div style="text-align: center; padding: 20px; color: #888;">Wishlist is empty.</div>');
            }
        } catch(e) {
            $('#wishlist-books-list').html('<div style="text-align: center; padding: 20px; color: #ff4444;">Failed to load wishlist.</div>');
        }
    }
}

// Attach to window namespace for global access
window.AdminMessagesController = AdminMessagesController;
