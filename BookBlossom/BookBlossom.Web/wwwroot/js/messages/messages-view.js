/**
 * BOOKBLOSSOM MESSAGES VIEW (MVC PATTERN)
 * Handles UI, rendering templates, and DOM operations.
 */
class MessagesView {
    constructor() {
        this.$chatStream = $('#chat-stream');
        this.$toast = $('#chat-toast');
        this.$toastMessage = $('#toast-message');
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
        const stream = document.getElementById('chat-stream');
        if (stream) {
            stream.scrollTop = stream.scrollHeight;
        }
    }

    scrollToMessage(targetId) {
        const $target = $('#' + targetId);
        if ($target.length > 0) {
            const stream = document.getElementById('chat-stream');
            const $stream = $('#chat-stream');
            
            const targetScrollTop = $target.offset().top - $stream.offset().top + $stream.scrollTop() - 40;
            
            stream.scrollTo({
                top: targetScrollTop,
                behavior: 'smooth'
            });
            
            const $highlightTarget = $target.closest('.msg-text-bubble');
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

    triggerToast(message) {
        this.$toastMessage.text(message);
        this.$toast.fadeIn(300);
        setTimeout(() => {
            this.$toast.fadeOut(300);
        }, 5000);
    }

    showConfettiOrToast(message) {
        const toastHtml = `
            <div class="support-success-toast" style="position: fixed; bottom: 25px; right: 25px; background: #C2185B; color: #fff; padding: 14px 28px; border-radius: 30px; font-weight: 700; font-size: 0.9rem; z-index: 100000; box-shadow: 0 10px 30px rgba(194, 24, 91, 0.45); display: flex; align-items: center; gap: 10px; opacity: 0; transform: translateY(20px); transition: all 0.3s ease;">
                <i class="fas fa-check-circle" style="font-size: 1.1rem;"></i> ${message}
            </div>
        `;
        const $toast = $(toastHtml);
        $('body').append($toast);
        
        setTimeout(function() {
            $toast.css({
                'opacity': '1',
                'transform': 'translateY(0)'
            });
        }, 50);
        
        setTimeout(function() {
            $toast.css({
                'opacity': '0',
                'transform': 'translateY(20px)'
            });
            setTimeout(function() { $toast.remove(); }, 300);
        }, 4000);
    }

    renderMessages(messages, append = false) {
        if (!append) {
            this.$chatStream.empty();
        }
        
        messages.forEach(msg => {
            const msgId = msg.messageID || msg.messageId;
            if (append && this.$chatStream.find(`#chat-msg-${msgId}`).length > 0) {
                return;
            }
            let bubble = '';
            let time = new Date(msg.sentAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
            
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
                            <h4 style="font-size: 0.85rem; font-weight: 700; color: #111; margin: 0 0 2px 0;">${msg.attachedBookTitle}</h4>
                            <p style="font-size: 0.72rem; color: #666; margin: 0;">${msg.attachedBookAuthor || 'Unknown'}</p>
                            <a href="${bookLink}" style="font-size: 0.72rem; font-weight: 700; color: #C2185B; text-decoration: none; display: inline-block; margin-top: 6px;" target="_blank"><i class="fas fa-external-link-alt"></i> View Book</a>
                        </div>
                    </div>
                `;
            }

            let mediaHtml = "";
            if (msg.attachmentUrls && msg.attachmentUrls.length > 0) {
                mediaHtml += `<div style="display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px;">`;
                msg.attachmentUrls.forEach(url => {
                    if (url.match(/\.(jpeg|jpg|gif|png)$/) != null) {
                        mediaHtml += `<div class="chat-lightbox-trigger" data-src="${url}" style="cursor: zoom-in; width: calc(50% - 4px); min-width: 100px; border-radius: 8px; overflow: hidden; border: 1.5px solid #EEC7C9;"><img src="${url}" style="width: 100%; height: 120px; object-fit: cover; display: block;"></div>`;
                    } else {
                        mediaHtml += `<div style="width: 100%; border-radius: 8px; overflow: hidden; border: 1.5px solid #EEC7C9;"><video src="${url}" controls style="width: 100%; max-height: 200px; background: #000;"></video></div>`;
                    }
                });
                mediaHtml += `</div>`;
            }

            if (msg.senderAvatar && msg.senderAvatar.includes('admin')) {
                // Incoming from Shop
                bubble = `
                    <div class="msg-bubble-group incoming" id="chat-msg-${msgId}">
                        <div class="msg-avatar-container">
                            <img src="${msg.senderAvatar}" alt="${msg.senderName}" class="msg-avatar">
                        </div>
                        <div class="msg-bubble-content">
                            <div class="msg-text-bubble">
                                ${msg.content ? `<div>${msg.content}</div>` : ''}
                                ${booksHtml}
                                ${mediaHtml}
                            </div>
                            <div class="msg-meta">${time} • ${msg.senderName}</div>
                        </div>
                    </div>
                `;
            } else {
                // Outgoing from User
                bubble = `
                    <div class="msg-bubble-group outgoing" id="chat-msg-${msgId}">
                        <div class="msg-bubble-content">
                            <div class="msg-text-bubble" style="background: #fff5f6; border: 1.5px solid #EEC7C9; color: #333;">
                                ${msg.content ? `<div style="font-weight: 500;">${msg.content}</div>` : ''}
                                ${booksHtml}
                                ${mediaHtml}
                            </div>
                            <div class="msg-meta">${time} • Sent <i class="fas fa-check-double" style="color: #C2185B; margin-left: 2px;"></i></div>
                        </div>
                    </div>
                `;
            }
            this.$chatStream.append(bubble);
        });
        this.scrollToBottom();
    }

    addMediaPreviewChip(dataUrl, type, filename, duration = 0) {
        let previewItem = "";
        if (type === 'image') {
            previewItem = `
                <div class="media-preview-item" data-type="image" data-src="${dataUrl}" style="position: relative; width: 60px; height: 60px; border-radius: 8px; overflow: hidden; border: 1.5px solid #EEC7C9; box-shadow: 0 4px 10px rgba(0,0,0,0.05); display: flex;">
                    <img src="${dataUrl}" style="width: 100%; height: 100%; object-fit: cover;">
                    <button class="btn-remove-media" style="position: absolute; top: 2px; right: 2px; background: rgba(194, 24, 91, 0.85); color: #fff; border: none; border-radius: 50%; width: 16px; height: 16px; font-size: 0.6rem; cursor: pointer; display: flex; align-items: center; justify-content: center; padding: 0; outline: none;"><i class="fas fa-times"></i></button>
                </div>
            `;
        } else {
            previewItem = `
                <div class="media-preview-item" data-type="video" data-src="${dataUrl}" style="position: relative; width: 60px; height: 60px; border-radius: 8px; overflow: hidden; border: 1.5px solid #EEC7C9; box-shadow: 0 4px 10px rgba(0,0,0,0.05); display: flex; background: #000;">
                    <video src="${dataUrl}" style="width: 100%; height: 100%; object-fit: cover;"></video>
                    <div style="position: absolute; bottom: 2px; left: 2px; background: rgba(0,0,0,0.65); color: #fff; font-size: 0.55rem; padding: 1px 4px; border-radius: 3px; font-weight: 700;"><i class="fas fa-video" style="font-size: 0.5rem; margin-right: 2px;"></i>${duration}s</div>
                    <button class="btn-remove-media" style="position: absolute; top: 2px; right: 2px; background: rgba(194, 24, 91, 0.85); color: #fff; border: none; border-radius: 50%; width: 16px; height: 16px; font-size: 0.6rem; cursor: pointer; display: flex; align-items: center; justify-content: center; padding: 0; outline: none;"><i class="fas fa-times"></i></button>
                </div>
            `;
        }
        $('#media-attachment-preview').append(previewItem).css('display', 'flex');
        this.scrollToBottom();
    }

    addTaggedBookChip(bookTitle, bookImg, bookLink, bookId) {
        const previewChip = `
            <div class="tagged-book-preview-chip" data-id="${bookId}" data-title="${bookTitle}" data-img="${bookImg}" data-link="${bookLink}" style="display: inline-flex; align-items: center; gap: 8px; background: #F4E1E2; border: 1.5px solid #EEC7C9; padding: 6px 12px; border-radius: 20px; font-size: 0.82rem; color: #C2185B; font-weight: 600; margin: 2px;">
                <img src="${bookImg}" style="width: 18px; height: 26px; object-fit: cover; border-radius: 3px;">
                <span style="max-width: 150px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${bookTitle}</span>
                <button class="btn-remove-tagged-book" style="background: none; border: none; color: #C2185B; font-size: 0.85rem; cursor: pointer; display: flex; align-items: center; justify-content: center; padding: 0; outline: none;"><i class="fas fa-times-circle"></i></button>
            </div>
        `;
        $('#tagged-books-preview').append(previewChip).css('display', 'flex');
        this.scrollToBottom();
    }

    clearSearchHighlights() {
        $('#chat-stream .msg-text-bubble').each(function() {
            const $bubble = $(this);
            const originalHtml = $bubble.data('original-html');
            if (originalHtml) {
                $bubble.html(originalHtml);
                $bubble.removeData('original-html');
            }
        });
        $('#chat-search-results-count').text('0 matches');
    }

    highlightActiveMatch(searchMatches, currentMatchIndex) {
        if (searchMatches.length === 0 || currentMatchIndex < 0) return;
        
        $('.chat-search-highlight').css({
            'background': '#FFF59D',
            'box-shadow': 'none',
            'transform': 'none'
        });
        
        const $active = searchMatches[currentMatchIndex];
        $active.css({
            'background': '#FBC02D',
            'box-shadow': '0 0 0 3px rgba(251, 192, 45, 0.45)',
            'transform': 'scale(1.1)',
            'display': 'inline-block'
        });
        
        const stream = document.getElementById('chat-stream');
        const $stream = $('#chat-stream');
        const targetScrollTop = $active.offset().top - $stream.offset().top + $stream.scrollTop() - 60;
        
        stream.scrollTo({
            top: targetScrollTop,
            behavior: 'smooth'
        });
        
        $('#chat-search-results-count').text((currentMatchIndex + 1) + ' of ' + searchMatches.length + ' matches');
    }
}

// Attach to window namespace for global access
window.MessagesView = MessagesView;
