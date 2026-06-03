/**
 * BOOKBLOSSOM MESSAGES CONTROLLER (MVC PATTERN)
 * Orchestrates event handlers, click listeners, state mutations,
 * and ties Model and View layers together cleanly.
 */
class MessagesController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
        
        // Search state
        this.searchMatches = [];
        this.currentMatchIndex = -1;
        this.searchTimeout = null;
    }

    async init() {
        this.bindEvents();

        // 1. Initial Load Messages
        if (!this.model.isGuest()) {
            this.loadMessagesFlow();
        }

        // 2. Parse active product context from URL query params
        this.parseUrlContext();
        
        // 3. Initialize SignalR
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
            if (this.model.activeConversationId?.toString() === msgConvoId?.toString()) {
                this.view.renderMessages([message], true);
            }
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

    async loadMessagesFlow() {
        try {
            const conversationsRes = await this.model.fetchConversations();
            if (conversationsRes && conversationsRes.data && conversationsRes.data.length > 0) {
                this.model.activeConversationId = conversationsRes.data[0].conversationID || conversationsRes.data[0].conversationId;
                
                // Join SignalR group if connected
                if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
                    this.hubConnection.invoke("JoinConversation", this.model.activeConversationId)
                        .then(() => console.log("Joined conversation group (initial load):", this.model.activeConversationId))
                        .catch(console.error);
                }
                
                const messagesRes = await this.model.fetchMessages(this.model.activeConversationId);
                if (messagesRes && messagesRes.data) {
                    this.view.renderMessages(messagesRes.data);
                }
            } else {
                $('#chat-stream').empty().append('<div class="msg-bubble-group incoming"><div class="msg-avatar-container"><img src="/images/Avatar/BookBlossom.png" alt="BookBlossom Shop" class="msg-avatar"></div><div class="msg-bubble-content"><div class="msg-text-bubble">Welcome to BookBlossom! Send a message to start chatting.</div></div></div>');
            }
        } catch (err) {
            console.error("Failed to load messages:", err);
        }
    }

    parseUrlContext() {
        const urlParams = new URLSearchParams(window.location.search);
        const paramTitle = urlParams.get('title');
        const paramPrice = urlParams.get('price');
        const paramImg = urlParams.get('img');
        const paramLink = urlParams.get('link');
        
        if (paramTitle && paramPrice && paramImg) {
            // Update Context Bar UI dynamically
            $('#context-book-img').attr('src', paramImg);
            $('#context-book-title').html(paramTitle);
            $('#context-book-price').text(paramPrice);
            
            // Show context bar in case it was hidden
            $('#chat-product-bar').slideDown(150);
        }

        // Auto-attach tagged book preview chip if book link is provided in the URL
        if (paramTitle && paramImg && paramLink) {
            // Ensure no duplicate chip is attached
            if ($(`#tagged-books-preview .tagged-book-preview-chip[data-link="${paramLink}"]`).length === 0) {
                this.view.addTaggedBookChip(paramTitle, paramImg, paramLink, "");
            }
        }
    }

    escapeRegExp(string) {
        return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    performSearch(query) {
        this.view.clearSearchHighlights();
        this.searchMatches = [];
        this.currentMatchIndex = -1;
        
        if (!query || query.trim() === '') {
            return;
        }
        
        query = query.trim().toLowerCase();
        const self = this;
        
        $('#chat-stream .msg-text-bubble').each(function() {
            const $bubble = $(this);
            let originalHtml = $bubble.data('original-html');
            
            if (!originalHtml) {
                originalHtml = $bubble.html();
                $bubble.data('original-html', originalHtml);
            }
            
            // Regex replacement avoiding html tag internals
            const regex = new RegExp('(' + self.escapeRegExp(query) + ')(?![^<]*>)', 'gi');
            
            if (regex.test(originalHtml)) {
                const highlightedHtml = originalHtml.replace(regex, '<mark class="chat-search-highlight" style="background: #FFF59D; color: #333; padding: 2px 0; border-radius: 2px; box-shadow: 0 1px 4px rgba(0,0,0,0.1); transition: all 0.2s; font-weight: inherit;">$1</mark>');
                $bubble.html(highlightedHtml);
                
                $bubble.find('.chat-search-highlight').each(function() {
                    self.searchMatches.push($(this));
                });
            }
        });
        
        if (this.searchMatches.length > 0) {
            this.currentMatchIndex = 0;
            this.view.highlightActiveMatch(this.searchMatches, this.currentMatchIndex);
        } else {
            $('#chat-search-results-count').text('No matches');
        }
    }

    submitMessageFlow(text) {
        const $chips = $('#tagged-books-preview .tagged-book-preview-chip');
        const $mediaItems = $('#media-attachment-preview .media-preview-item');
        if (text === '' && $chips.length === 0 && $mediaItems.length === 0) return;

        const time = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });
        const self = this;

        // Construct books HTML
        let booksHtml = "";
        let bookTitlesList = [];
        if ($chips.length > 0) {
            booksHtml += `<div style="display: flex; flex-direction: column; gap: 10px; margin-top: 10px; padding-top: 10px; border-top: 1px solid rgba(194, 24, 91, 0.15);">`;
            $chips.each(function() {
                const title = $(this).data('title');
                const author = $(this).data('author') || "BookBlossom Curated";
                const img = $(this).data('img');
                const link = $(this).data('link');
                bookTitlesList.push(title);
                
                booksHtml += `
                    <div style="display: flex; gap: 12px; align-items: start; background: #fff; padding: 12px; border-radius: 12px; border: 1.5px solid rgba(194, 24, 91, 0.1); box-shadow: 0 2px 8px rgba(0,0,0,0.02); transition: all 0.2s;">
                        <img src="${img}" style="width: 45px; height: 62px; object-fit: cover; border-radius: 4px; box-shadow: 0 4px 10px rgba(0,0,0,0.08);">
                        <div style="flex: 1; min-width: 0; text-align: left;">
                            <span style="font-size: 0.65rem; text-transform: uppercase; font-weight: 700; color: #C2185B; background: #F4E1E2; padding: 2px 6px; border-radius: 4px; display: inline-block; margin-bottom: 4px;">Book Tagged</span>
                            <h4 style="font-size: 0.85rem; font-weight: 700; color: #111; margin: 0 0 2px 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${title}</h4>
                            <p style="font-size: 0.72rem; color: #666; margin: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${author}</p>
                            <a href="${link}" style="font-size: 0.72rem; font-weight: 700; color: #C2185B; text-decoration: none; display: inline-block; margin-top: 6px;" target="_blank"><i class="fas fa-external-link-alt"></i> View Book</a>
                        </div>
                    </div>
                `;
            });
            booksHtml += `</div>`;
        }

        // Construct media attachments HTML
        let mediaHtml = "";
        if ($mediaItems.length > 0) {
            mediaHtml += `<div style="display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px; padding-top: 10px; border-top: 1px solid rgba(194, 24, 91, 0.15);">`;
            $mediaItems.each(function() {
                const type = $(this).data('type');
                const src = $(this).data('src');
                
                if (type === 'image') {
                    mediaHtml += `
                        <div class="chat-lightbox-trigger" data-src="${src}" style="cursor: zoom-in; width: calc(50% - 4px); min-width: 100px; border-radius: 8px; overflow: hidden; border: 1.5px solid #EEC7C9; box-shadow: 0 4px 12px rgba(0,0,0,0.04); transition: transform 0.2s;">
                            <img src="${src}" style="width: 100%; height: 120px; object-fit: cover; display: block;">
                        </div>
                    `;
                } else {
                    mediaHtml += `
                        <div style="width: calc(50% - 4px); min-width: 100px; border-radius: 8px; overflow: hidden; border: 1.5px solid #EEC7C9; background: #000; box-shadow: 0 4px 12px rgba(0,0,0,0.04);">
                            <video src="${src}" controls style="width: 100%; height: 180px; object-fit: contain; background: #000; display: block;"></video>
                        </div>
                    `;
                }
            });
            mediaHtml += `</div>`;
        }

        let attachedBookId = null;
        if ($chips.length > 0) {
            let did = $chips.first().data('id');
            if (did) attachedBookId = parseInt(did);
        }
        
        let attachmentUrls = [];
        if ($mediaItems.length > 0) {
            $mediaItems.each(function() {
                attachmentUrls.push($(this).data('src'));
            });
        }
        
        const dto = {
            conversationID: this.model.activeConversationId,
            receiverID: 1, 
            content: text,
            attachmentUrls: attachmentUrls,
            attachedBookID: attachedBookId
        };

        // Clear previews
        $('#tagged-books-preview').empty().hide();
        $('#media-attachment-preview').empty().hide();

        // Post to server
        this.model.sendMessage(dto).then(res => {
            console.log("Message sent to DB", res);
            if (res && (res.messageID || res.messageId)) {
                this.view.renderMessages([res], true);
            }
        }).catch(err => {
            console.error("Failed to save message", err);
        });

        // Update convo item preview on left pane
        let previewText = text !== '' ? text : "";
        if ($chips.length > 0) {
            previewText = `[Tagged ${$chips.length} Book(s)] ` + previewText;
        }
        if ($mediaItems.length > 0) {
            previewText = `[Sent ${$mediaItems.length} media file(s)] ` + previewText;
        }
        $('[data-shop-id="bookblossom"] .convo-preview').text(previewText);
        $('[data-shop-id="bookblossom"] .convo-time').text("Just now");

        // Trigger Auto Replies
        if ($chips.length > 0) {
            setTimeout(function() {
                const $typingWrapper = $('#typing-indicator-wrapper');
                $typingWrapper.show();
                self.view.scrollToBottom();
                
                setTimeout(function() {
                    $typingWrapper.hide();
                    let replyText = "";
                    if (bookTitlesList.length === 1) {
                        replyText = `Oh! I see you tagged <strong>${bookTitlesList[0]}</strong>. That is an amazing choice! We have this title in excellent condition and packaged elegantly. Would you like to check out now or add a custom message wrapper? 🌸`;
                    } else {
                        replyText = `Wow! You tagged multiple books: <strong>${bookTitlesList.join(', ')}</strong>. Those are absolutely wonderful selections! We can bundle them together beautifully in a special gift wrapper with dynamic vibes. Do you have any custom requests for this order? 📦💖`;
                    }
                    const shopBubble = `
                        <div class="msg-bubble-group incoming">
                            <div class="msg-avatar-container">
                                <img src="/images/Avatar/BookBlossom.png" alt="BookBlossom Shop" class="msg-avatar">
                            </div>
                            <div class="msg-bubble-content">
                                <div class="msg-text-bubble">${replyText}</div>
                                <div class="msg-meta">${time} • Shop Supporter</div>
                            </div>
                        </div>
                    `;
                    self.view.$chatStream.append(shopBubble);
                    self.view.scrollToBottom();
                }, 1800);
            }, 1000);
        } else if ($mediaItems.length > 0) {
            setTimeout(function() {
                const $typingWrapper = $('#typing-indicator-wrapper');
                $typingWrapper.show();
                self.view.scrollToBottom();
                
                setTimeout(function() {
                    $typingWrapper.hide();
                    const replyText = "Oh! Thank you for sharing these media files. 📸 Our curators are looking at them right now! Let us know if you need any assistance or have specific preferences. 🌸";
                    const shopBubble = `
                        <div class="msg-bubble-group incoming">
                            <div class="msg-avatar-container">
                                <img src="/images/Avatar/BookBlossom.png" alt="BookBlossom Shop" class="msg-avatar">
                            </div>
                            <div class="msg-bubble-content">
                                <div class="msg-text-bubble">${replyText}</div>
                                <div class="msg-meta">${time} • Shop Supporter</div>
                            </div>
                        </div>
                    `;
                    self.view.$chatStream.append(shopBubble);
                    self.view.scrollToBottom();
                }, 1800);
            }, 1000);
        } else {
            this.simulateShopReply(text);
        }
    }

    simulateShopReply(userQuery) {
        const $typingWrapper = $('#typing-indicator-wrapper');
        const self = this;
        
        setTimeout(function() {
            $typingWrapper.show();
            self.view.scrollToBottom();
            
            setTimeout(function() {
                $typingWrapper.hide();
                
                let replyText = "Thank you for writing to Book Blossom! 🌸 Our customer service agents are currently assisting other readers, but we will write back to you in just a brief moment. Please feel free to check our Shop Vouchers sidebar to apply special discounts!";
                const queryLower = userQuery.toLowerCase();

                if (queryLower.includes('stock') || queryLower.includes('mystery book')) {
                    replyText = "Yes! Our <strong>Mystery Science Fiction Book</strong> is fully in stock and ready to ship! Every order is premium-wrapped and unboxed directly by our curators with no spoilers. 📦✨";
                } else if (queryLower.includes('how') || queryLower.includes('works') || queryLower.includes('blind date')) {
                    replyText = "It is super simple! You browse mystery titles based entirely on genre, clues, publication year, and Goodreads scores, instead of covers. We print wrappers decorated beautifully with vibes. It's a wonderful gift for yourself! 🌸";
                } else if (queryLower.includes('coupon') || queryLower.includes('discount') || queryLower.includes('voucher')) {
                    replyText = "Absolutely! We currently have active shop coupon codes like <strong>BLOSSOM10</strong> (10k off orders above 150k) and <strong>MYSTERY50</strong> (50k off orders above 500k). Just tap 'Apply' on the right sidebar to add them! 🎫";
                } else if (queryLower.includes('shipping') || queryLower.includes('deliver') || queryLower.includes('delivery')) {
                    replyText = "Standard delivery takes approximately <strong>2 to 3 business days</strong> to Ho Chi Minh City and surrounding areas. For other provinces, delivery normally takes 3 to 5 business days. 🚚";
                }

                const time = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });
                const shopBubble = `
                    <div class="msg-bubble-group incoming">
                        <div class="msg-avatar-container">
                            <img src="/images/Avatar/BookBlossom.png" alt="BookBlossom Shop" class="msg-avatar">
                        </div>
                        <div class="msg-bubble-content">
                            <div class="msg-text-bubble">${replyText}</div>
                            <div class="msg-meta">${time} • Shop Supporter</div>
                        </div>
                    </div>
                `;

                self.view.$chatStream.append(shopBubble);
                self.view.scrollToBottom();

                $('[data-shop-id="bookblossom"] .convo-preview').html(replyText);
            }, 1800);
        }, 1000);
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

        // Fetch Wishlist
        if (!this.model.isGuest()) {
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
                    $('#wishlist-books-list').html('<div style="text-align: center; padding: 20px; color: #888;">Your wishlist is empty.</div>');
                }
            } catch(e) {
                $('#wishlist-books-list').html('<div style="text-align: center; padding: 20px; color: #ff4444;">Failed to load wishlist.</div>');
            }
        } else {
            $('#wishlist-books-list').html('<div style="text-align: center; padding: 20px; color: #888;">Please login to view wishlist.</div>');
        }
    }

    bindEvents() {
        const self = this;

        // Conversation search filtering
        $('#convo-search-input').on('input', function() {
            const query = $(this).val().toLowerCase().trim();
            $('.convo-item').each(function() {
                const name = $(this).find('.convo-name').text().toLowerCase();
                const text = $(this).find('.convo-preview').text().toLowerCase();
                if (name.includes(query) || text.includes(query)) {
                    $(this).show();
                } else {
                    $(this).hide();
                }
            });
        });

        // Dismiss product context bar inside chat
        $('#btn-close-context-bar').on('click', function() {
            $('#chat-product-bar').slideUp(250);
        });

        // View Shared Pictures Trigger
        $('#btn-view-shared-pictures').on('click', function(e) {
            e.preventDefault();
            const $grid = $('#shared-pictures-grid');
            $grid.empty();
            
            // Assign unique IDs to any untagged lightbox triggers in chat stream
            $('#chat-stream .chat-lightbox-trigger').each(function(index) {
                if (!$(this).attr('id')) {
                    $(this).attr('id', 'chat-img-' + index);
                }
            });
            
            // Assign unique IDs to any untagged videos in chat stream
            $('#chat-stream video').each(function(index) {
                if (!$(this).attr('id')) {
                    $(this).attr('id', 'chat-video-' + index);
                }
            });
            
            const $images = $('#chat-stream .chat-lightbox-trigger');
            const $videos = $('#chat-stream video');
            
            if ($images.length === 0 && $videos.length === 0) {
                $('#shared-pictures-empty').show();
                $grid.hide();
            } else {
                $('#shared-pictures-empty').hide();
                $grid.show();
                
                $images.each(function() {
                    const src = $(this).data('src');
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
            
            $('#shared-pictures-modal').fadeIn(200).addClass('active').css('display', 'flex');
        });

        // View Shared Book Links Trigger
        $('#btn-view-shared-links').on('click', function(e) {
            e.preventDefault();
            const $list = $('#shared-links-list');
            $list.empty();
            
            const $cards = $('#chat-stream a[href*="#book-details-"], #chat-stream a[href*="#blind-details-"]').closest('div[style*="display: flex; gap: 12px;"]');
            
            if ($cards.length === 0) {
                $('#shared-links-empty').show();
                $list.hide();
            } else {
                $('#shared-links-empty').hide();
                $list.show();
                
                const uniqueLinks = new Set();
                
                $cards.each(function() {
                    const title = $(this).find('h4').text();
                    const author = $(this).find('p').text();
                    const img = $(this).find('img').attr('src');
                    const link = $(this).find('a').attr('href');
                    
                    if (uniqueLinks.has(link)) return;
                    uniqueLinks.add(link);
                    
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
            
            $('#shared-links-modal').fadeIn(200).addClass('active').css('display', 'flex');
        });

        // Scroll to message in Chat Trigger
        $(document).on('click', '.btn-scroll-to-msg', function(e) {
            e.preventDefault();
            const targetId = $(this).data('target-id');
            $('#chat-image-lightbox').fadeOut(150);
            $('#shared-pictures-modal').fadeOut(150).removeClass('active');
            self.view.scrollToMessage(targetId);
        });

        // Close modals handler
        $('.btn-close-shared-modal, .custom-modal .modal-backdrop').on('click', function() {
            $(this).closest('.custom-modal').fadeOut(200).removeClass('active');
        });

        // Checkout Now Button Reference Click
        $('#btn-context-checkout').on('click', function() {
            self.view.triggerToast("Redirecting to checkout session with active mystery book...");
            setTimeout(function() {
                window.location.href = "/#blind-date";
            }, 1000);
        });

        // Lightbox trigger inside chat
        $(document).on('click', '.chat-lightbox-trigger', function() {
            const src = $(this).data('src');
            const targetId = $(this).attr('id');
            
            $('#lightbox-img').attr('src', src).show();
            $('#lightbox-video').hide();
            $('#btn-lightbox-scroll-to-msg').attr('data-target-id', targetId);
            
            $('#chat-image-lightbox').fadeIn(150).css('display', 'flex');
        });

        // Lightbox trigger for shared grid video
        $(document).on('click', '.shared-grid-video', function() {
            const src = $(this).data('src');
            const targetId = $(this).data('target-id');
            
            $('#lightbox-img').hide();
            $('#lightbox-video').attr('src', src).show();
            $('#btn-lightbox-scroll-to-msg').attr('data-target-id', targetId);
            
            // Auto play fullscreen video
            const videoEl = document.getElementById('lightbox-video');
            if (videoEl) {
                videoEl.play().catch(() => {});
            }
            
            $('#chat-image-lightbox').fadeIn(150).css('display', 'flex');
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

        // Search toggle button
        $('#btn-toggle-chat-search').on('click', function(e) {
            e.preventDefault();
            const $searchBar = $('#chat-search-bar');
            if ($searchBar.is(':visible')) {
                $searchBar.slideUp(200);
                self.view.clearSearchHighlights();
                $('#chat-search-input').val('');
            } else {
                $searchBar.slideDown(200, function() {
                    $('#chat-search-input').focus();
                }).css('display', 'flex');
            }
        });

        // Close search bar button
        $('#btn-close-chat-search').on('click', function(e) {
            e.preventDefault();
            $('#chat-search-bar').slideUp(200);
            self.view.clearSearchHighlights();
            $('#chat-search-input').val('');
        });

        // Input search listener with debounce
        $('#chat-search-input').on('input', function() {
            const query = $(this).val();
            clearTimeout(self.searchTimeout);
            self.searchTimeout = setTimeout(function() {
                self.performSearch(query);
            }, 250);
        });

        // Next Match
        $('#btn-chat-search-next').on('click', function(e) {
            e.preventDefault();
            if (self.searchMatches.length > 0) {
                self.currentMatchIndex = (self.currentMatchIndex + 1) % self.searchMatches.length;
                self.view.highlightActiveMatch(self.searchMatches, self.currentMatchIndex);
            }
        });

        // Prev Match
        $('#btn-chat-search-prev').on('click', function(e) {
            e.preventDefault();
            if (self.searchMatches.length > 0) {
                self.currentMatchIndex = (self.currentMatchIndex - 1 + self.searchMatches.length) % self.searchMatches.length;
                self.view.highlightActiveMatch(self.searchMatches, self.currentMatchIndex);
            }
        });

        // Tag Book Modal Trigger
        $('#btn-tag-book-trigger').on('click', function(e) {
            e.preventDefault();
            $('#tag-book-modal').fadeIn(200).addClass('active').css('display', 'flex');
            $('#tag-book-link').val('');
            $('#link-error').hide();
            $('#tag-book-modal .book-select-item').removeClass('selected');
            
            self.loadTagBooksFlow();
        });

        // Call Center / Support Request Modal triggers
        $('#btn-call-center').on('click', function(e) {
            e.preventDefault();
            $('#call-center-modal').fadeIn(200).addClass('active').css('display', 'flex');
            
            $('#call-center-form')[0].reset();
            $('.support-checkbox-label').removeClass('checked').css({
                'borderColor': '#F4E1E2',
                'background': 'none',
                'color': '#555',
                'fontWeight': 'normal'
            });
        });

        // Dismiss Support Request Modal
        $('.btn-close-call-modal, #call-center-modal .modal-backdrop').on('click', function() {
            $('#call-center-modal').fadeOut(200).removeClass('active');
        });

        // Checkbox styling highlight toggle
        $('.support-checkbox-label input[type="checkbox"]').on('change', function() {
            const $label = $(this).closest('.support-checkbox-label');
            if ($(this).is(':checked')) {
                $label.addClass('checked').css({
                    'borderColor': '#C2185B',
                    'background': '#fdf5f6',
                    'color': '#C2185B',
                    'fontWeight': '700'
                });
            } else {
                $label.removeClass('checked').css({
                    'borderColor': '#F4E1E2',
                    'background': 'none',
                    'color': '#555',
                    'fontWeight': 'normal'
                });
            }
        });

        // Handle submission of Support Request Form
        $('#call-center-form').on('submit', function(e) {
            e.preventDefault();
            
            const selectedTopics = [];
            $('input[name="supportTopic"]:checked').each(function() {
                selectedTopics.push($(this).val());
            });
            
            if (selectedTopics.length === 0) {
                alert('Vui lòng chọn ít nhất một chủ đề bạn cần hỗ trợ!');
                return;
            }
            
            const rawPhone = $('#support-phone').val().trim();
            const phone = rawPhone.replace(/[\s.-]/g, '');
            const phoneRegex = /^(0[3|5|7|8|9])[0-9]{8}$/;
            if (!phoneRegex.test(phone)) {
                alert('Số điện thoại không hợp lệ! Vui lòng nhập số điện thoại Việt Nam gồm 10 chữ số (bắt đầu bằng 03, 05, 07, 08 hoặc 09, ví dụ: 0935516370).');
                return;
            }
            
            const notes = $('#support-notes').val().trim();
            
            $('#call-center-modal').fadeOut(200).removeClass('active');
            
            let categoryValue = 0;
            $('input[name="supportTopic"]:checked').each(function() {
                const topic = $(this).val();
                if (topic === 'Product / Book') categoryValue |= 1;
                else if (topic === 'Order') categoryValue |= 2;
                else if (topic === 'Payment') categoryValue |= 4;
                else if (topic === 'Shipping') categoryValue |= 8;
                else if (topic === 'Complaint / Refund') categoryValue |= 16;
                else if (topic === 'Other') categoryValue |= 32;
            });

            const currentUsername = $('.user-dropdown .username').text().trim() || "Jane Doe";

            const dto = {
                category: categoryValue,
                phoneNumber: phone,
                note: notes,
                conversationID: self.model.activeConversationId || null
            };

            if (window.apiClient) {
                window.apiClient.apiPost('/api/MessagesAPI/call-request', dto).then(res => {
                    const timestamp = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
                    
                    const ticketHtml = `
                        <div class="msg-bubble-group outgoing">
                            <div class="msg-bubble-content">
                                <div class="msg-text-bubble" style="background: #FFF5F6; border: 1.5px solid #EEC7C9; color: #333; padding: 15px; border-radius: 18px; box-shadow: 0 4px 15px rgba(194, 24, 91, 0.05); max-width: 320px;">
                                    <div style="display: flex; align-items: center; gap: 8px; font-weight: 800; color: #C2185B; border-bottom: 1.5px dashed #EEC7C9; padding-bottom: 8px; margin-bottom: 8px; font-size: 0.9rem;">
                                        <i class="fas fa-ticket-alt"></i> SUPPORT REQUEST TICKET
                                    </div>
                                    <div style="font-size: 0.8rem; line-height: 1.5; color: #444;">
                                        <strong>Topics:</strong> ${selectedTopics.join(', ')}<br>
                                        <strong>Phone:</strong> ${phone}<br>
                                        ${notes ? `<strong>Notes:</strong> ${notes}<br>` : ''}
                                        <span style="display: inline-block; margin-top: 8px; background: rgba(194, 24, 91, 0.1); color: #C2185B; font-weight: 700; padding: 2px 8px; border-radius: 12px; font-size: 0.7rem;"><i class="fas fa-spinner fa-spin" style="margin-right: 4px;"></i>Pending curation team</span>
                                    </div>
                                </div>
                                <div class="msg-meta">${timestamp} • ${currentUsername}</div>
                            </div>
                        </div>
                    `;
                    
                    self.view.$chatStream.append(ticketHtml);
                    self.view.scrollToBottom();
                    
                    setTimeout(function() {
                        const replyHtml = `
                            <div class="msg-bubble-group incoming">
                                <div class="msg-avatar-container">
                                    <img src="/images/Avatar/BookBlossom.png" alt="BookBlossom Shop" class="msg-avatar">
                                </div>
                                <div class="msg-bubble-content">
                                    <div class="msg-text-bubble">
                                        Thank you ${currentUsername}! 🌸 We have successfully received your support ticket regarding <strong>${selectedTopics[0]}</strong>. A BookBlossom support specialist will call you at <strong>${phone}</strong> shortly!
                                    </div>
                                    <div class="msg-meta">${new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })} • Shop Supporter</div>
                                </div>
                            </div>
                        `;
                        self.view.$chatStream.append(replyHtml);
                        self.view.scrollToBottom();
                        self.view.showConfettiOrToast("Support request sent! Our curator will reach out to you within minutes.");
                    }, 2000);
                }).catch(err => {
                    console.error("Failed to submit support request:", err);
                    alert("Failed to submit support request: " + (err.message || err));
                });
            } else {
                alert("API Client is not ready. Please try again.");
            }
        });

        // Dismiss Modal
        $('#btn-cancel-tag-book, #tag-book-modal .modal-backdrop').on('click', function() {
            $('#tag-book-modal').fadeOut(200).removeClass('active');
        });

        // Tabs toggle inside Tag Book Modal
        $('#tag-book-modal .btn-modal-tab').on('click', function() {
            $('#tag-book-modal .btn-modal-tab').removeClass('active');
            $(this).addClass('active');
            
            const target = $(this).data('target');
            $('#tag-book-modal .modal-tab-content').hide().removeClass('active');
            $('#' + target).fadeIn(150).addClass('active');
        });

        // Select Book inside lists (Delegated)
        $('#tag-book-modal').on('click', '.book-select-item', function() {
            $('#tag-book-modal .book-select-item').removeClass('selected');
            $(this).addClass('selected');
        });

        // Delegate event to remove tagged book chip
        $(document).on('click', '.btn-remove-tagged-book', function(e) {
            e.preventDefault();
            e.stopPropagation();
            $(this).closest('.tagged-book-preview-chip').remove();
            if ($('#tagged-books-preview .tagged-book-preview-chip').length === 0) {
                $('#tagged-books-preview').hide();
            }
        });

        // Confirm Tag Book click
        $('#btn-confirm-tag-book').on('click', async function() {
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
                            const res = await self.model.fetchBookDetails(numericId);
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
                const $selectedItem = $(`#${activeTab} .book-select-item.selected`);
                if ($selectedItem.length === 0) {
                    self.view.triggerToast("Please select a book first!");
                    return;
                }
                bookTitle = $selectedItem.data('title');
                bookAuthor = $selectedItem.data('author');
                bookImg = $selectedItem.data('img');
                bookLink = $selectedItem.data('link');
                bookId = $selectedItem.data('id') || "";
            }

            self.view.addTaggedBookChip(bookTitle, bookImg, bookLink, bookId);
            $('#tag-book-modal').fadeOut(200).removeClass('active');
        });

        // Intercept clicks on links pointing to book details
        $(document).on('click', 'a[href*="#book-details-"], a[href*="#blind-details-"]', function(e) {
            e.preventDefault();
            const href = $(this).attr('href');
            if (!href) return;
            
            let title = "";
            if (href.includes('#book-details-')) {
                title = href.split('#book-details-')[1];
            } else if (href.includes('#blind-details-')) {
                title = href.split('#blind-details-')[1];
            }
            
            title = decodeURIComponent(title);
            window.location.href = `/Explore#book-details-${encodeURIComponent(title)}`;
        });

        // Make the entire book card clickable in the chat stream
        $(document).on('click', '#chat-stream div', function(e) {
            const $card = $(this);
            if ($card.css('border-style') === 'solid' || $card.find('a[href*="#book-details-"], a[href*="#blind-details-"]').length > 0) {
                const $link = $card.find('a[href*="#book-details-"], a[href*="#blind-details-"]');
                if ($link.length > 0) {
                    if ($(e.target).closest('a').length > 0) return;
                    e.preventDefault();
                    $link.first().click();
                }
            }
        });

        // Trigger Media Attachment File Browser
        $('#btn-media-attachment-trigger').on('click', function(e) {
            e.preventDefault();
            $('#media-attachment-input').click();
        });

        // Handle Media Selection and Validation
        $('#media-attachment-input').on('change', function(e) {
            const files = e.target.files;
            if (!files || files.length === 0) return;

            Array.from(files).forEach(file => {
                if (file.type.startsWith('image/')) {
                    const maxImgSize = 10 * 1024 * 1024;
                    if (file.size > maxImgSize) {
                        self.view.triggerToast(`Image "${file.name}" exceeds the 10MB size limit! (${(file.size / (1024 * 1024)).toFixed(1)}MB)`);
                        return;
                    }

                    const reader = new FileReader();
                    reader.onload = function(evt) {
                        self.view.addMediaPreviewChip(evt.target.result, 'image', file.name);
                    };
                    reader.readAsDataURL(file);
                } else if (file.type.startsWith('video/')) {
                    const maxVidSize = 50 * 1024 * 1024;
                    if (file.size > maxVidSize) {
                        self.view.triggerToast(`Video "${file.name}" exceeds the 50MB size limit! (${(file.size / (1024 * 1024)).toFixed(1)}MB)`);
                        return;
                    }

                    const video = document.createElement('video');
                    video.preload = 'metadata';
                    video.src = URL.createObjectURL(file);
                    
                    video.onloadedmetadata = function() {
                        URL.revokeObjectURL(video.src);
                        const duration = Math.round(video.duration);
                        if (duration > 60) {
                            self.view.triggerToast(`Video "${file.name}" exceeds the 60 seconds limit! (${duration}s)`);
                        } else {
                            const reader = new FileReader();
                            reader.onload = function(evt) {
                                self.view.addMediaPreviewChip(evt.target.result, 'video', file.name, duration);
                            };
                            reader.readAsDataURL(file);
                        }
                    };
                    
                    video.onerror = function() {
                        self.view.triggerToast(`Failed to load video file: "${file.name}"`);
                    };
                } else {
                    self.view.triggerToast(`Unsupported file type: "${file.name}"`);
                }
            });

            $(this).val('');
        });

        // Delegate event to remove media preview item
        $(document).on('click', '.btn-remove-media', function(e) {
            e.preventDefault();
            e.stopPropagation();
            $(this).closest('.media-preview-item').remove();
            if ($('#media-attachment-preview .media-preview-item').length === 0) {
                $('#media-attachment-preview').hide();
            }
        });

        // Submit on Send button click
        $('#btn-submit-chat').on('click', function() {
            const text = $('#message-text-input').val().trim();
            self.submitMessageFlow(text);
            $('#message-text-input').val('');
        });

        // Submit on Enter key press
        $('#message-text-input').on('keypress', function(e) {
            if (e.which === 13) {
                const text = $(this).val().trim();
                self.submitMessageFlow(text);
                $(this).val('');
            }
        });

        // Quick reply chips triggers
        $('.quick-reply-chip').on('click', function() {
            const text = $(this).data('text');
            self.submitMessageFlow(text);
        });
    }
}

// Attach to window namespace for global access
window.MessagesController = MessagesController;
