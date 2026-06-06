/**
 * BOOKBLOSSOM ADMIN MESSAGES VIEW (MVC PATTERN)
 * Handles UI DOM selectors, template rendering, formatting, and rendering components.
 */
class AdminMessagesView {
    constructor() {
        // Left sidebar tabs
        this.$tabBtns = $('.tab-btn');
        this.$tabContents = $('.tab-content');
        this.$convoList = $('#admin-convo-list');
        this.$convoSearch = $('#admin-convo-search');
        this.$requestList = $('#support-requests-tab .request-list');

        // Central Chat Area
        this.$emptyStateArea = $('#admin-chat-empty-state');
        this.$chatMainArea = $('#admin-chat-main-area');
        this.$chatStream = $('#admin-chat-stream');
        this.$chatHeaderName = $('#current-chat-name');
        this.$chatHeaderAvatar = $('#current-chat-avatar');
        this.$messageInput = $('#admin-message-input');
        this.$btnSend = $('#admin-btn-send');

        // Chat Search
        this.$btnToggleSearch = $('#btn-admin-toggle-chat-search');
        this.$chatSearchBar = $('#admin-chat-search-bar');
        this.$chatSearchInput = $('#admin-chat-search-input');
        this.$chatSearchResultsCount = $('#admin-chat-search-results-count');
        this.$btnSearchPrev = $('#btn-admin-chat-search-prev');
        this.$btnSearchNext = $('#btn-admin-chat-search-next');
        this.$btnCloseSearch = $('#btn-admin-close-chat-search');

        // Media & Book tagging attachments
        this.$mediaTrigger = $('#btn-admin-media-trigger');
        this.$mediaInput = $('#admin-media-attachment-input');
        this.$mediaPreviewBar = $('#admin-media-attachment-preview');
        this.$tagBookTrigger = $('#btn-admin-tag-book-trigger');
        this.$taggedBookPreviewBar = $('#admin-tagged-books-preview');

        // Right sidebar (Buyer Profile)
        this.$buyerSidebarArea = $('#admin-buyer-sidebar-area');
        this.$sidebarAvatar = $('#sidebar-buyer-avatar');
        this.$sidebarName = $('#sidebar-buyer-name');
        this.$sidebarJoined = $('#sidebar-buyer-joined');
        this.$sidebarOrders = $('#sidebar-buyer-orders');
        this.$sidebarSpent = $('#sidebar-buyer-spent');
        this.$sidebarAddress = $('#sidebar-buyer-address');
        this.$sidebarPhone = $('#sidebar-buyer-phone');
        this.$sidebarEmail = $('#sidebar-buyer-email');

        // Support request detail panel
        this.$reqDetailArea = $('#admin-req-detail-area');
        this.$detailReqStatus = $('#detail-req-status');
        this.$detailReqBuyer = $('#detail-req-buyer');
        this.$detailReqCategory = $('#detail-req-category');
        this.$detailReqPhone = $('#detail-req-phone');
        this.$detailReqNote = $('#detail-req-note');
        this.$btnBackFromReq = $('#btn-back-from-req');
        this.$btnSrChat = $('#btn-sr-chat');

        // Modals
        this.$confirmResolveModal = $('#admin-confirm-resolve-modal');
        this.$btnCancelResolve = $('#btn-admin-cancel-resolve');
        this.$btnConfirmResolve = $('#btn-admin-confirm-resolve');
    }

    escapeHtml(text) {
        if (!text) return '';
        return text.toString()
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    scrollToBottom() {
        const stream = document.getElementById('admin-chat-stream');
        if (stream) {
            stream.scrollTop = stream.scrollHeight;
        }
    }

    renderConversations(conversations, activeConvoId) {
        this.$convoList.empty();
        if (conversations.length === 0) {
            this.$convoList.append('<div style="padding: 20px; text-align: center; color: #888; font-size: 0.9rem;">No conversations found</div>');
            return;
        }

        conversations.forEach(c => {
            const convoId = c.conversationID || c.conversationId;
            const activeClass = convoId === activeConvoId ? 'active' : '';
            const unreadIndicator = c.hasUnreadMessages 
                ? '<span style="color:red; font-weight:bold; font-size:1.2rem; margin-left:auto;">•</span>' 
                : '';
            const avatar = c.customerAvatar || '/images/Avatar/default.png';
            const time = new Date(c.updatedAt).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: true });

            const item = `
                <div class="convo-item ${activeClass}" data-convo-id="${convoId}" data-buyer-name="${this.escapeHtml(c.customerName)}" data-buyer-avatar="${avatar}">
                    <div class="convo-avatar-wrapper">
                        <div class="convo-avatar-container">
                            <img src="${avatar}" alt="Buyer" class="convo-avatar" onerror="this.src='/images/Avatar/default.png'">
                        </div>
                    </div>
                    <div class="convo-info">
                        <div class="convo-name-time">
                            <span class="convo-name">${this.escapeHtml(c.customerName)}</span>
                            <span class="convo-time">${time}</span>
                        </div>
                        <div class="convo-preview" style="display:flex; align-items:center;">
                            ${this.escapeHtml(c.lastMessageSnippet)} ${unreadIndicator}
                        </div>
                    </div>
                </div>
            `;
            this.$convoList.append(item);
        });
    }

    renderSupportRequests(requests) {
        this.$requestList.empty();
        if (requests.length === 0) {
            this.$requestList.append('<div style="padding: 20px; text-align: center; color: #888; font-size: 0.9rem;">No support requests</div>');
            return;
        }

        requests.forEach(req => {
            const statusClass = req.status === 0 ? "status-pending" : (req.status === 1 ? "status-inprogress" : "status-resolved");
            const time = new Date(req.createdAt).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: true });

            const item = `
                <div class="request-item" data-req-id="${req.requestID}" data-buyer-name="${this.escapeHtml(req.customerName)}">
                    <div class="req-header">
                        <span class="req-type">${this.escapeHtml(req.categoryName)}</span>
                        <span class="req-time">${time}</span>
                    </div>
                    <div class="req-buyer">${this.escapeHtml(req.customerName)}</div>
                    <div class="req-preview">${this.escapeHtml(req.note)}</div>
                    <div class="req-footer">
                        <span class="req-phone"><i class="fas fa-phone-alt"></i> ${this.escapeHtml(req.phoneNumber)}</span>
                        <span class="req-status ${statusClass}">${this.escapeHtml(req.statusName)}</span>
                    </div>
                </div>
            `;
            this.$requestList.append(item);
        });
    }

    renderMessages(messages, append = false) {
        if (!append) {
            this.$chatStream.empty();
        }
        
        if (messages.length === 0 && !append) {
            this.$chatStream.append('<div style="padding: 40px; text-align: center; color: #aaa; font-style: italic;">No messages yet. Send a message to start conversation.</div>');
            return;
        }

        messages.forEach(msg => {
            const msgId = msg.messageID || msg.messageId;
            // Avoid duplicates
            if (append && this.$chatStream.find(`[data-msg-id="${msgId}"]`).length > 0) {
                return;
            }
            const time = new Date(msg.sentAt).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: true });
            
            let booksHtml = "";
            const attachedBookId = msg.attachedBookID || msg.attachedBookId;
            if (attachedBookId) {
                let bookLink = `/Explore#book-details-${encodeURIComponent(msg.attachedBookTitle)}`;
                if (msg.attachedBookTitle && msg.attachedBookTitle.includes('Blind Book')) {
                    bookLink = `/BlindDate#blind-details-${attachedBookId}`;
                }

                booksHtml = `
                    <div style="display: flex; gap: 12px; align-items: start; background: #fff; padding: 12px; border-radius: 12px; border: 1.5px solid rgba(194, 24, 91, 0.1); margin-top: 10px;">
                        <img src="${msg.attachedBookImage || '/images/Book/book1.jpg'}" style="width: 45px; height: 62px; object-fit: cover; border-radius: 4px;">
                        <div style="flex: 1; min-width: 0; text-align: left;">
                            <h4 style="font-size: 0.85rem; font-weight: 700; color: #111; margin: 0 0 2px 0;">${this.escapeHtml(msg.attachedBookTitle)}</h4>
                            <p style="font-size: 0.72rem; color: #666; margin: 0;">${this.escapeHtml(msg.attachedBookAuthor || 'Unknown')}</p>
                            <a href="${bookLink}" style="font-size: 0.72rem; font-weight: 700; color: #C2185B; text-decoration: none; display: inline-block; margin-top: 6px;" target="_blank"><i class="fas fa-external-link-alt"></i> View Book</a>
                        </div>
                    </div>
                `;
            }

            let mediaHtml = "";
            if (msg.attachmentUrls && msg.attachmentUrls.length > 0) {
                mediaHtml += `<div style="display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px;">`;
                msg.attachmentUrls.forEach(url => {
                    if (url.match(/\.(jpeg|jpg|gif|png)$/i) != null || url.startsWith('data:image')) {
                        mediaHtml += `<img src="${url}" style="width: calc(50% - 4px); min-width: 100px; height: 120px; object-fit: cover; border-radius: 8px; cursor: pointer;" class="chat-media-click" onerror="this.src='/images/Avatar/default.png'">`;
                    } else {
                        mediaHtml += `<video src="${url}" controls style="width: 100%; max-height: 200px; background: #000; border-radius: 8px;"></video>`;
                    }
                });
                mediaHtml += `</div>`;
            }

            let bubble = '';
            const contentHtml = msg.content ? `<div>${this.escapeHtml(msg.content)}</div>` : '';

            if (msg.senderAvatar.includes('admin') || msg.senderName === "BookBlossom Shop") {
                // Outgoing for Admin
                bubble = `
                    <div class="msg-bubble-group outgoing" data-msg-id="${msgId}">
                        <div class="msg-bubble-content">
                            <div class="msg-text-bubble" style="background: #fff5f6; border: 1.5px solid #EEC7C9; color: #333;">
                                ${contentHtml}
                                ${booksHtml}
                                ${mediaHtml}
                            </div>
                            <div class="msg-meta">${time}</div>
                        </div>
                    </div>
                `;
            } else {
                // Incoming from User
                const avatar = msg.senderAvatar || '/images/Avatar/default.png';
                bubble = `
                    <div class="msg-bubble-group incoming" data-msg-id="${msgId}">
                        <div class="msg-avatar-container">
                            <img src="${avatar}" alt="${this.escapeHtml(msg.senderName)}" class="msg-avatar" onerror="this.src='/images/Avatar/default.png'">
                        </div>
                        <div class="msg-bubble-content">
                            <div class="msg-text-bubble">
                                ${contentHtml}
                                ${booksHtml}
                                ${mediaHtml}
                            </div>
                            <div class="msg-meta">${time}</div>
                        </div>
                    </div>
                `;
            }
            this.$chatStream.append(bubble);
        });

        setTimeout(() => this.scrollToBottom(), 50);
    }

    renderBuyerProfile(profile) {
        if (!profile) {
            this.clearBuyerProfile();
            return;
        }

        const avatar = profile.avatar || '/images/Avatar/default.png';
        
        // Header info
        this.$chatHeaderName.text(profile.buyerName);
        this.$chatHeaderAvatar.attr('src', avatar);
        this.$messageInput.attr('placeholder', `Type a message to ${profile.buyerName}...`);

        // Sidebar info
        this.$sidebarName.text(profile.buyerName);
        this.$sidebarAvatar.attr('src', avatar);
        this.$sidebarJoined.text(profile.joinedDate);
        this.$sidebarOrders.text(profile.totalOrders);
        
        // Format Total Spending
        const spentFormatted = new Intl.NumberFormat('vi-VN').format(profile.totalSpent) + ' VND';
        this.$sidebarSpent.text(spentFormatted);

        this.$sidebarAddress.text(profile.defaultAddress);
        this.$sidebarPhone.text(profile.phoneNumber);
        this.$sidebarEmail.text(profile.email);
    }

    clearBuyerProfile() {
        this.$sidebarName.text('Buyer Profile');
        this.$sidebarAvatar.attr('src', '/images/Avatar/default.png');
        this.$sidebarJoined.text('Joined: N/A');
        this.$sidebarOrders.text('0');
        this.$sidebarSpent.text('0 VND');
        this.$sidebarAddress.text('No address registered');
        this.$sidebarPhone.text('N/A');
        this.$sidebarEmail.text('N/A');
    }

    renderSupportRequestDetails(req) {
        this.$detailReqBuyer.text(req.customerName);
        this.$detailReqCategory.text(req.categoryName);
        this.$detailReqNote.text(req.note);
        this.$detailReqPhone.text(req.phoneNumber);

        const statusClass = req.status === 0 ? "status-pending" : (req.status === 1 ? "status-inprogress" : "status-resolved");
        this.$detailReqStatus.removeClass().addClass(`req-status ${statusClass}`).text(req.statusName);

        this.$emptyStateArea.hide();
        this.$chatMainArea.hide();
        this.$reqDetailArea.fadeIn(200);
        this.$buyerSidebarArea.fadeIn(200);
    }

    renderTaggedBookPreview(book) {
        this.$taggedBookPreviewBar.empty();
        if (!book) {
            this.$taggedBookPreviewBar.hide();
            return;
        }

        const previewHtml = `
            <div style="background: #F4E1E2; border: 1px solid #C2185B; border-radius: 8px; padding: 4px 10px; display: flex; align-items: center; gap: 8px; font-size: 0.8rem; font-weight: 700; color: #C2185B;">
                <i class="fas fa-link"></i>
                <span class="preview-title" style="max-width: 150px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${this.escapeHtml(book.title)}</span>
                <button id="btn-admin-clear-tag" style="border: none; background: none; color: #C2185B; cursor: pointer; padding: 0; font-size: 0.85rem;"><i class="fas fa-times-circle"></i></button>
            </div>
        `;
        this.$taggedBookPreviewBar.append(previewHtml).css('display', 'flex');
    }

    renderMediaPreviews(files) {
        this.$mediaPreviewBar.empty();
        if (!files || files.length === 0) {
            this.$mediaPreviewBar.hide();
            return;
        }

        files.forEach((file, index) => {
            const previewHtml = `
                <div style="position: relative; width: 60px; height: 60px; border-radius: 8px; border: 1.5px solid #EEC7C9; overflow: hidden; background: #eee;">
                    <img src="${file.data}" style="width: 100%; height: 100%; object-fit: cover;">
                    <button class="btn-remove-media-preview" data-index="${index}" style="position: absolute; top: 2px; right: 2px; border: none; background: rgba(0,0,0,0.6); color: #fff; width: 18px; height: 18px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 0.65rem; cursor: pointer; padding: 0;"><i class="fas fa-times"></i></button>
                </div>
            `;
            this.$mediaPreviewBar.append(previewHtml);
        });

        this.$mediaPreviewBar.css('display', 'flex');
    }

    clearInput() {
        this.$messageInput.val('');
        this.renderTaggedBookPreview(null);
        this.renderMediaPreviews([]);
    }

    scrollToMessage(targetId) {
        const $target = $('#' + targetId);
        if ($target.length > 0) {
            const stream = document.getElementById('admin-chat-stream');
            const $stream = $('#admin-chat-stream');
            
            const targetScrollTop = $target.offset().top - $stream.offset().top + $stream.scrollTop() - 40;
            
            if (stream) {
                stream.scrollTo({
                    top: targetScrollTop,
                    behavior: 'smooth'
                });
            }
            
            const $highlightTarget = $target.closest('.msg-text-bubble');
            if ($highlightTarget.length > 0) {
                $highlightTarget.css({
                    'box-shadow': '0 0 0 5px rgba(194, 24, 91, 0.45)',
                    'transition': 'all 0.3s ease',
                    'transform': 'scale(1.015)'
                });
                
                setTimeout(function() {
                    $highlightTarget.css({
                        'box-shadow': 'none',
                        'transform': 'none'
                    });
                }, 1800);
            }
        }
    }
}

// Attach to window namespace for global access
window.AdminMessagesView = AdminMessagesView;
