$(document).ready(function() {
    // Mock Buyers Database for Profile Syncing
    const mockBuyers = {
        "1": {
            name: "Anna Smith",
            avatar: "/images/Avatar/avatar1.jpg",
            status: "Active now",
            isOnline: true,
            joined: "Joined: 6 months ago",
            orders: "12",
            spent: "1.5M VND",
            address: "123 Book St, District 1, Ho Chi Minh City",
            phone: "0987 654 321",
            email: "anna.smith@example.com"
        },
        "2": {
            name: "John Doe",
            avatar: "/images/Avatar/avatar1.jpg",
            status: "Offline",
            isOnline: false,
            joined: "Joined: 3 months ago",
            orders: "8",
            spent: "950K VND",
            address: "456 Flower Ave, District 3, Ho Chi Minh City",
            phone: "0912 345 678",
            email: "john.doe@example.com"
        },
        "3": {
            name: "Michael Brown",
            avatar: "/images/Avatar/avatar1.jpg",
            status: "Offline",
            isOnline: false,
            joined: "Joined: 1 year ago",
            orders: "5",
            spent: "620K VND",
            address: "789 Pine Rd, District 5, Ho Chi Minh City",
            phone: "0933 445 566",
            email: "michael.brown@example.com"
        }
    };

    // 1. Tab Switching (Conversations vs Support Requests)
    $('.tab-btn').on('click', function() {
        // Update active tab button
        $('.tab-btn').removeClass('active');
        $(this).addClass('active');

        // Show target tab content
        const targetId = $(this).data('target');
        $('.tab-content').hide().removeClass('active');
        $(targetId).fadeIn(200).addClass('active');
    });

    // Function to sync buyer profile to right sidebar and header
    function syncBuyerProfile(buyerIdOrName) {
        let buyer = null;
        if (mockBuyers[buyerIdOrName]) {
            buyer = mockBuyers[buyerIdOrName];
        } else {
            buyer = Object.values(mockBuyers).find(b => b.name.toLowerCase() === buyerIdOrName.toString().toLowerCase());
        }

        if (!buyer) {
            // Generate temporary buyer if not found in mock database
            buyer = {
                name: decodeURIComponent(buyerIdOrName),
                avatar: "/images/Avatar/avatar1.jpg",
                status: "Offline",
                isOnline: false,
                joined: "Joined: Recently",
                orders: "1",
                spent: "0 VND",
                address: "N/A",
                phone: "N/A",
                email: "N/A"
            };
        }

        if (buyer) {
            // Close search bar if open & clear highlights
            $('#admin-chat-search-bar').hide();
            adminClearSearchHighlights();
            $('#admin-chat-search-input').val('');

            // Update Center Header
            $('#current-chat-name').text(buyer.name);
            $('#current-chat-avatar').attr('src', buyer.avatar);
            
            if (buyer.isOnline) {
                $('#current-chat-online').show();
                $('#current-chat-status').html('<i class="fas fa-circle" style="color: #2e7d32; font-size: 0.6rem;"></i> Active now');
            } else {
                $('#current-chat-online').hide();
                $('#current-chat-status').html('<i class="fas fa-circle" style="color: #888; font-size: 0.6rem;"></i> Offline');
            }

            // Update Right Sidebar
            $('#sidebar-buyer-name').text(buyer.name);
            $('#sidebar-buyer-avatar').attr('src', buyer.avatar);
            $('#sidebar-buyer-joined').text(buyer.joined);
            $('#sidebar-buyer-orders').text(buyer.orders);
            $('#sidebar-buyer-spent').text(buyer.spent);
            $('#sidebar-buyer-address').text(buyer.address);
            $('#sidebar-buyer-phone').text(buyer.phone);
            $('#sidebar-buyer-email').text(buyer.email);
            
            // Update input placeholder
            $('#admin-message-input').attr('placeholder', `Type a message to ${buyer.name}...`);
            return buyer;
        }
        return null;
    }

    // 2. Handle Conversation Selection
    $('#admin-convo-list').on('click', '.convo-item', function() {
        // Reset state
        $('#admin-convo-list .convo-item').removeClass('active');
        $(this).addClass('active');

        const buyerId = $(this).data('buyer-id');

        // Ensure Chat Area is visible, Hide Request Details
        $('#admin-req-detail-area').hide();
        $('#admin-chat-main-area').fadeIn(200);

        // Sync Profile Data
        const buyer = syncBuyerProfile(buyerId);
        
        if (buyer) {
            // Clear chat stream and simulate load
            $('#admin-chat-stream').empty().append(`
                <div class="chat-date-separator">
                    <span>Today</span>
                </div>
                <div class="msg-bubble-group incoming">
                    <img src="${buyer.avatar}" class="msg-avatar" onerror="this.src='/images/Avatar/avatar1.jpg'">
                    <div class="msg-bubble-content">
                        <div class="msg-text-bubble">
                            Hello! I am ${buyer.name}. Can you help me?
                        </div>
                        <div class="msg-meta">Just now</div>
                    </div>
                </div>
            `);
        }
    });

    // 3. Handle Support Request Selection
    $('.request-item').on('click', function() {
        const reqId = $(this).data('req-id');
        $('#admin-req-detail-area').data('active-req-id', reqId);

        // Extract Data from clicked item
        const reqCategory = $(this).find('.req-type').text().trim();
        const reqBuyer = $(this).find('.req-buyer').text().trim();
        const reqPreview = $(this).find('.req-preview').text().trim();
        const reqStatusClass = $(this).find('.req-status').attr('class');
        const reqStatusText = $(this).find('.req-status').text();

        // Find associated phone from database if possible
        const buyer = Object.values(mockBuyers).find(b => b.name === reqBuyer);
        const reqPhone = buyer ? buyer.phone : '09xxxxxxxx';

        // Update Detail View
        $('#detail-req-buyer').text(reqBuyer);
        $('#detail-req-category').text(reqCategory);
        $('#detail-req-note').text(reqPreview);
        $('#detail-req-phone').text(reqPhone);
        $('#detail-req-status').removeClass().addClass(reqStatusClass).text(reqStatusText);

        // Switch Main Content Area
        $('#admin-chat-main-area').hide();
        $('#admin-req-detail-area').fadeIn(200);
    });

    // 4. Back to Chat from Support Request
    $('#btn-back-from-req').on('click', function() {
        $('#admin-req-detail-area').hide();
        $('#admin-chat-main-area').fadeIn(200);
    });

    // 5. Message Buyer from Support Request Detail
    $('#btn-sr-chat').on('click', function() {
        const buyerName = $('#detail-req-buyer').text().trim();
        
        // Switch back to chat
        $('#admin-req-detail-area').hide();
        $('#admin-chat-main-area').fadeIn(200);

        // Switch left tab to Conversations
        $('.tab-btn[data-target="#conversations-tab"]').click();

        // Find the convo-item with this name and click it
        const $convoItem = $('.convo-item').filter(function() {
            return $(this).find('.convo-name').text().trim().toLowerCase() === buyerName.toLowerCase();
        });

        if ($convoItem.length > 0) {
            $convoItem.click();
        } else {
            // Fallback Sync Profile Data
            syncBuyerProfile(buyerName);
        }
        
        // Add context message into chat
        $('#admin-chat-stream').append(`
            <div class="chat-date-separator">
                <span>System</span>
            </div>
            <div style="text-align: center; color: #888; font-size: 0.8rem; margin: 10px 0;">
                Chat initiated from Support Request: ${$('#detail-req-category').text()}
            </div>
        `);
        
        // Scroll down
        const stream = document.getElementById('admin-chat-stream');
        if(stream) {
            stream.scrollTop = stream.scrollHeight;
        }
    });

    // ==========================================
    // Rich Inputs: Tag Book & Media Attachments
    // ==========================================

    // A. Tag Book Modal Triggers
    $('#btn-admin-tag-book-trigger').on('click', function(e) {
        e.preventDefault();
        $('#tag-book-modal').fadeIn(200).addClass('active').css('display', 'flex');
        // Reset inputs and selections
        $('#tag-book-link').val('');
        $('#link-error').hide();
        $('#tag-book-modal .book-select-item').removeClass('selected');
    });

    // B. Media Attachment File Browser Trigger
    $('#btn-admin-media-trigger').on('click', function(e) {
        e.preventDefault();
        $('#admin-media-attachment-input').click();
    });

    // C. Handle Media Selection and Validation
    $('#admin-media-attachment-input').on('change', function(e) {
        const files = e.target.files;
        if (!files || files.length === 0) return;

        Array.from(files).forEach(file => {
            if (file.type.startsWith('image/')) {
                const maxImgSize = 10 * 1024 * 1024; // 10MB
                if (file.size > maxImgSize) {
                    alert(`Image "${file.name}" exceeds the 10MB size limit!`);
                    return;
                }

                const reader = new FileReader();
                reader.onload = function(evt) {
                    addAdminMediaPreviewChip(evt.target.result, 'image', file.name);
                };
                reader.readAsDataURL(file);
            } else if (file.type.startsWith('video/')) {
                const maxVidSize = 50 * 1024 * 1024; // 50MB
                if (file.size > maxVidSize) {
                    alert(`Video "${file.name}" exceeds the 50MB size limit!`);
                    return;
                }

                const reader = new FileReader();
                reader.onload = function(evt) {
                    addAdminMediaPreviewChip(evt.target.result, 'video', file.name);
                };
                reader.readAsDataURL(file);
            } else {
                alert(`Unsupported file type: ${file.name}`);
            }
        });
    });

    function addAdminMediaPreviewChip(src, type, filename) {
        let chipContent = '';
        if (type === 'image') {
            chipContent = `<img src="${src}" style="width: 40px; height: 40px; object-fit: cover; border-radius: 6px;">`;
        } else {
            chipContent = `<div style="width: 40px; height: 40px; background: #C2185B; color: #fff; display: flex; align-items: center; justify-content: center; border-radius: 6px; font-size: 0.8rem;"><i class="fas fa-video"></i></div>`;
        }

        const previewChip = `
            <div class="admin-media-preview-chip" data-type="${type}" data-src="${src}" data-filename="${filename}" style="position: relative; display: inline-block; width: 40px; height: 40px; border: 1.5px solid #EEC7C9; border-radius: 8px; margin: 2px;">
                ${chipContent}
                <button type="button" class="btn-remove-admin-media" style="position: absolute; top: -6px; right: -6px; background: #C2185B; color: #fff; border: none; border-radius: 50%; width: 16px; height: 16px; font-size: 0.6rem; display: flex; align-items: center; justify-content: center; cursor: pointer; padding: 0; outline: none;"><i class="fas fa-times"></i></button>
            </div>
        `;

        $('#admin-media-attachment-preview').append(previewChip).css('display', 'flex');
    }

    // D. Delegate events for removal
    $(document).on('click', '.btn-remove-admin-media', function(e) {
        e.preventDefault();
        e.stopPropagation();
        $(this).closest('.admin-media-preview-chip').remove();
        if ($('#admin-media-attachment-preview .admin-media-preview-chip').length === 0) {
            $('#admin-media-attachment-preview').hide();
        }
    });

    $(document).on('click', '#admin-tagged-books-preview .btn-remove-tagged-book', function(e) {
        e.preventDefault();
        e.stopPropagation();
        $(this).closest('.tagged-book-preview-chip').remove();
        if ($('#admin-tagged-books-preview .tagged-book-preview-chip').length === 0) {
            $('#admin-tagged-books-preview').hide();
        }
    });

    // E. Lightbox for media in chat
    $(document).on('click', '.chat-media-click', function() {
        const src = $(this).attr('src');
        $('#lightbox-img').attr('src', src).show();
        $('#lightbox-video').hide();
        $('#chat-image-lightbox').fadeIn(200).css('display', 'flex');
    });

    $('#chat-image-lightbox').on('click', function() {
        $(this).fadeOut(200);
    });

    // F. Shared modals triggers since they are on page
    $('#btn-cancel-tag-book, #tag-book-modal .modal-backdrop').on('click', function() {
        $('#tag-book-modal').fadeOut(200).removeClass('active');
    });

    $('#tag-book-modal .btn-modal-tab').on('click', function() {
        $('#tag-book-modal .btn-modal-tab').removeClass('active');
        $(this).addClass('active');
        
        const target = $(this).data('target');
        $('#tag-book-modal .modal-tab-content').hide().removeClass('active');
        $('#' + target).fadeIn(150).addClass('active');
    });

    $('#tag-book-modal .book-select-item').on('click', function() {
        $('#tag-book-modal .book-select-item').removeClass('selected');
        $(this).addClass('selected');
    });

    $('#btn-confirm-tag-book').on('click', function() {
        if ($('#admin-tagged-books-preview').length === 0) return; // Only run on admin page

        const activeTab = $('#tag-book-modal .btn-modal-tab.active').data('target');
        let bookTitle = "", bookAuthor = "", bookImg = "", bookLink = "";

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
                bookTitle = "Mystery Blind Book (" + decodeURIComponent(hashPart) + ")";
                bookAuthor = "BookBlossom Curated";
                bookImg = "/images/Book/book1.jpg";
            } else if (linkVal.includes('#book-details-')) {
                const hashPart = linkVal.split('#book-details-')[1];
                bookTitle = decodeURIComponent(hashPart);
                bookAuthor = "BookBlossom Curated";
                bookImg = "/images/Book/book1.jpg";
            } else {
                bookTitle = "BookBlossom Shared Book";
                bookAuthor = "Community Curator";
                bookImg = "/images/Book/book1.jpg";
            }
            bookLink = linkVal;
        } else {
            const $selectedItem = $(`#${activeTab} .book-select-item.selected`);
            if ($selectedItem.length === 0) {
                alert("Please select a book first!");
                return;
            }
            bookTitle = $selectedItem.data('title');
            bookAuthor = $selectedItem.data('author');
            bookImg = $selectedItem.data('img');
            bookLink = $selectedItem.data('link');
        }

        const previewChip = `
            <div class="tagged-book-preview-chip" data-title="${bookTitle}" data-author="${bookAuthor}" data-img="${bookImg}" data-link="${bookLink}" style="display: inline-flex; align-items: center; gap: 8px; background: #F4E1E2; border: 1.5px solid #EEC7C9; padding: 6px 12px; border-radius: 20px; font-size: 0.82rem; color: #C2185B; font-weight: 600; margin: 2px;">
                <img src="${bookImg}" style="width: 18px; height: 26px; object-fit: cover; border-radius: 3px;">
                <span style="max-width: 150px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">${bookTitle}</span>
                <button type="button" class="btn-remove-tagged-book" style="background: none; border: none; color: #C2185B; font-size: 0.85rem; cursor: pointer; display: flex; align-items: center; justify-content: center; padding: 0; outline: none;"><i class="fas fa-times-circle"></i></button>
            </div>
        `;

        $('#admin-tagged-books-preview').append(previewChip).css('display', 'flex');
        $('#tag-book-modal').fadeOut(200).removeClass('active');
    });

    // ==========================================
    // Shared Materials Modal Populating Logic
    // ==========================================

    // 1. Shared Pictures Click
    $('#btn-admin-view-shared-pictures').on('click', function(e) {
        e.preventDefault();
        
        const $grid = $('#shared-pictures-grid');
        $grid.empty();
        
        // Assign unique IDs to any untagged lightbox triggers in admin chat stream
        $('#admin-chat-stream .chat-media-click').each(function(index) {
            if (!$(this).attr('id')) {
                $(this).attr('id', 'admin-chat-img-' + index);
            }
        });
        
        // Assign unique IDs to any untagged videos in admin chat stream
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
            
            // Render Images
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
            
            // Render Videos
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

    // 2. Shared Links Click
    $('#btn-admin-view-shared-links').on('click', function(e) {
        e.preventDefault();
        
        const $list = $('#shared-links-list');
        $list.empty();
        
        // Scan admin-chat-stream for book links
        const $cards = $('#admin-chat-stream a[href*="#book-details-"], #admin-chat-stream a[href*="#blind-details-"]').closest('div[style*="display: flex; gap:"]');
        
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
                        <a href="${link}" target="_blank" style="font-size: 0.75rem; font-weight: 700; color: #fff; background: #C2185B; padding: 6px 12px; border-radius: 20px; text-decoration: none; display: flex; align-items: center; gap: 4px; transition: background 0.2s;" onmouseover="this.style.background='#a0134a'" onmouseout="this.style.background='#C2185B'">
                            <i class="fas fa-external-link-alt"></i> Go Detail
                        </a>
                    </div>
                `;
                $list.append(itemHtml);
            });
        }
        
        $('#shared-links-modal').fadeIn(200).addClass('active').css('display', 'flex');
    });

    // Close shared modals
    $('.btn-close-shared-modal').on('click', function() {
        $('#shared-pictures-modal').fadeOut(200).removeClass('active');
        $('#shared-links-modal').fadeOut(200).removeClass('active');
    });

    // Scroll to message trigger (supporting both admin and buyer streams)
    $(document).on('click', '.btn-scroll-to-msg', function(e) {
        e.preventDefault();
        const targetId = $(this).data('target-id');
        
        $('#shared-pictures-modal').fadeOut(150).removeClass('active');
        
        const $target = $('#' + targetId);
        if ($target.length > 0) {
            const stream = document.getElementById('admin-chat-stream') || document.getElementById('chat-stream');
            if (stream) {
                const targetOffset = $target.position().top + stream.scrollTop - 100;
                $(stream).animate({ scrollTop: targetOffset }, 400);
                
                // Flashing highlight animation
                $target.closest('.msg-bubble-group').css({
                    'background': 'rgba(194, 24, 91, 0.08)',
                    'transition': 'background 0.3s ease'
                });
                setTimeout(() => {
                    $target.closest('.msg-bubble-group').css('background', 'none');
                }, 1500);
            }
        }
    });

    // ==========================================
    // Admin Chat Message Search Engine Logic
    // ==========================================
    let adminSearchMatches = [];
    let adminCurrentMatchIndex = -1;

    function escapeRegExp(string) {
        return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    }

    function adminClearSearchHighlights() {
        $('#admin-chat-stream .msg-text-bubble').each(function() {
            const $bubble = $(this);
            const originalHtml = $bubble.data('original-html');
            if (originalHtml) {
                $bubble.html(originalHtml);
                $bubble.removeData('original-html');
            }
        });
        adminSearchMatches = [];
        adminCurrentMatchIndex = -1;
        $('#admin-chat-search-results-count').text('0 matches');
    }

    function adminHighlightActiveMatch() {
        if (adminSearchMatches.length === 0 || adminCurrentMatchIndex < 0) return;
        
        $('.chat-search-highlight').css({
            'background': '#FFF59D',
            'box-shadow': 'none',
            'transform': 'none'
        });
        
        const $active = adminSearchMatches[adminCurrentMatchIndex];
        $active.css({
            'background': '#FBC02D',
            'box-shadow': '0 0 0 3px rgba(251, 192, 45, 0.45)',
            'transform': 'scale(1.1)',
            'display': 'inline-block'
        });
        
        const stream = document.getElementById('admin-chat-stream');
        const $stream = $('#admin-chat-stream');
        const targetScrollTop = $active.offset().top - $stream.offset().top + $stream.scrollTop() - 60;
        
        stream.scrollTo({
            top: targetScrollTop,
            behavior: 'smooth'
        });
        
        $('#admin-chat-search-results-count').text((adminCurrentMatchIndex + 1) + ' of ' + adminSearchMatches.length + ' matches');
    }

    function adminPerformSearch(query) {
        adminClearSearchHighlights();
        
        if (!query || query.trim() === '') {
            return;
        }
        
        query = query.trim().toLowerCase();
        adminSearchMatches = [];
        
        $('#admin-chat-stream .msg-text-bubble').each(function() {
            const $bubble = $(this);
            let originalHtml = $bubble.data('original-html');
            
            if (!originalHtml) {
                originalHtml = $bubble.html();
                $bubble.data('original-html', originalHtml);
            }
            
            const regex = new RegExp('(' + escapeRegExp(query) + ')(?![^<]*>)', 'gi');
            
            if (regex.test(originalHtml)) {
                const highlightedHtml = originalHtml.replace(regex, '<mark class="chat-search-highlight" style="background: #FFF59D; color: #333; padding: 2px 0; border-radius: 2px; box-shadow: 0 1px 4px rgba(0,0,0,0.1); transition: all 0.2s; font-weight: inherit;">$1</mark>');
                $bubble.html(highlightedHtml);
                
                $bubble.find('.chat-search-highlight').each(function() {
                    adminSearchMatches.push($(this));
                });
            }
        });
        
        if (adminSearchMatches.length > 0) {
            adminCurrentMatchIndex = 0;
            adminHighlightActiveMatch();
        } else {
            $('#admin-chat-search-results-count').text('No matches');
        }
    }

    // Toggle Search Bar
    $('#btn-admin-toggle-chat-search').on('click', function(e) {
        e.preventDefault();
        const $searchBar = $('#admin-chat-search-bar');
        if ($searchBar.is(':visible')) {
            $searchBar.slideUp(200);
            adminClearSearchHighlights();
            $('#admin-chat-search-input').val('');
        } else {
            $searchBar.slideDown(200, function() {
                $('#admin-chat-search-input').focus();
            }).css('display', 'flex');
        }
    });

    // Close Search Bar
    $('#btn-admin-close-chat-search').on('click', function(e) {
        e.preventDefault();
        $('#admin-chat-search-bar').slideUp(200);
        adminClearSearchHighlights();
        $('#admin-chat-search-input').val('');
    });

    // Search Input listener with Debounce
    let adminSearchTimeout = null;
    $('#admin-chat-search-input').on('input', function() {
        const query = $(this).val();
        clearTimeout(adminSearchTimeout);
        adminSearchTimeout = setTimeout(function() {
            adminPerformSearch(query);
        }, 250);
    });

    // Next match Click
    $('#btn-admin-chat-search-next').on('click', function(e) {
        e.preventDefault();
        if (adminSearchMatches.length > 0) {
            adminCurrentMatchIndex = (adminCurrentMatchIndex + 1) % adminSearchMatches.length;
            adminHighlightActiveMatch();
        }
    });

    // Prev match Click
    $('#btn-admin-chat-search-prev').on('click', function(e) {
        e.preventDefault();
        if (adminSearchMatches.length > 0) {
            adminCurrentMatchIndex = (adminCurrentMatchIndex - 1 + adminSearchMatches.length) % adminSearchMatches.length;
            adminHighlightActiveMatch();
        }
    });

    // 6. Simple Chat Submit Simulation for Admin
    $('#admin-btn-send').on('click', function() {
        submitAdminMessage();
    });

    $('#admin-message-input').on('keypress', function(e) {
        if (e.which === 13) {
            submitAdminMessage();
        }
    });

    function submitAdminMessage() {
        // Clear search highlights first to prevent index pollution
        adminClearSearchHighlights();

        const text = $('#admin-message-input').val().trim();
        const hasMedia = $('#admin-media-attachment-preview .admin-media-preview-chip').length > 0;
        const hasBooks = $('#admin-tagged-books-preview .tagged-book-preview-chip').length > 0;

        if (text === '' && !hasMedia && !hasBooks) return;

        const time = new Date().toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', hour12: false });
        
        // Extract Media content
        let mediaHtml = '';
        $('#admin-media-attachment-preview .admin-media-preview-chip').each(function() {
            const type = $(this).data('type');
            const src = $(this).data('src');
            if (type === 'image') {
                mediaHtml += `
                    <div style="margin-top: 8px; max-width: 250px; border-radius: 12px; overflow: hidden; border: 1px solid #EEC7C9;">
                        <img src="${src}" style="width: 100%; max-height: 200px; object-fit: cover; cursor: pointer;" class="chat-media-click">
                    </div>
                `;
            } else {
                mediaHtml += `
                    <div style="margin-top: 8px; max-width: 250px; border-radius: 12px; overflow: hidden; border: 1px solid #EEC7C9;">
                        <video src="${src}" controls style="width: 100%; max-height: 200px; object-fit: cover;"></video>
                    </div>
                `;
            }
        });

        // Extract Book content
        let booksHtml = '';
        $('#admin-tagged-books-preview .tagged-book-preview-chip').each(function() {
            const title = $(this).data('title');
            const author = $(this).data('author');
            const img = $(this).data('img');
            const link = $(this).data('link');
            booksHtml += `
                <div style="display: flex; gap: 12px; align-items: center; background: #fff; border: 1.5px solid #EEC7C9; padding: 12px; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.02); margin-top: 8px; max-width: 250px; text-align: left;">
                    <img src="${img}" style="width: 40px; height: 55px; object-fit: cover; border-radius: 4px; box-shadow: 0 3px 8px rgba(0,0,0,0.08);">
                    <div style="flex: 1; min-width: 0;">
                        <h4 style="font-size: 0.85rem; font-weight: 700; color: #111; margin: 0 0 2px 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${title}</h4>
                        <p style="font-size: 0.72rem; color: #666; margin: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;">${author}</p>
                    </div>
                    <a href="${link}" target="_blank" style="font-size: 0.75rem; font-weight: 700; color: #C2185B; padding: 5px; text-decoration: none;">
                        <i class="fas fa-chevron-right"></i>
                    </a>
                </div>
            `;
        });

        const adminBubble = `
            <div class="msg-bubble-group outgoing">
                <div class="msg-bubble-content">
                    <div class="msg-text-bubble">
                        ${text ? `<div>${text}</div>` : ''}
                        ${mediaHtml}
                        ${booksHtml}
                    </div>
                    <div class="msg-meta">${time} • Sent <i class="fas fa-check-double" style="color: #C2185B; margin-left: 2px;"></i></div>
                </div>
            </div>
        `;

        $('#admin-chat-stream').append(adminBubble);
        $('#admin-message-input').val('');
        
        // Reset previews
        $('#admin-tagged-books-preview').empty().hide();
        $('#admin-media-attachment-preview').empty().hide();
        $('#admin-media-attachment-input').val('');

        const stream = document.getElementById('admin-chat-stream');
        stream.scrollTop = stream.scrollHeight;
    }

    // ==========================================
    // SPA Book Details Navigation Interceptors
    // ==========================================

    // 1. Intercept clicks on links pointing to book details
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

    // 2. Make the entire book card clickable in the chat stream
    $(document).on('click', '#admin-chat-stream div', function(e) {
        const $card = $(this);
        // Check if this div is a book card by seeing if it has a border and contains a book details link
        if ($card.css('border-style') === 'solid' || $card.find('a[href*="#book-details-"], a[href*="#blind-details-"]').length > 0) {
            const $link = $card.find('a[href*="#book-details-"], a[href*="#blind-details-"]');
            if ($link.length > 0) {
                // If the user clicked the link directly, let the link's click handler handle it
                if ($(e.target).closest('a').length > 0) return;
                
                e.preventDefault();
                $link.first().click();
            }
        }
    });

    // ==========================================
    // Support Request Resolve Modal Actions
    // ==========================================

    // 1. Mark as Resolved button click -> Show Confirm Modal
    $('.btn-sr-resolve').on('click', function(e) {
        e.preventDefault();
        const activeReqId = $('#admin-req-detail-area').data('active-req-id');
        if (activeReqId) {
            $('#admin-confirm-resolve-modal').fadeIn(200).addClass('active').css('display', 'flex');
        }
    });

    // 2. Dismiss Resolve Modal
    $('#btn-admin-cancel-resolve, #admin-confirm-resolve-modal .modal-backdrop').on('click', function(e) {
        e.preventDefault();
        $('#admin-confirm-resolve-modal').fadeOut(200).removeClass('active');
    });

    // 3. Confirm Resolve Modal button click -> Perform Resolution
    $('#btn-admin-confirm-resolve').on('click', function(e) {
        e.preventDefault();
        const activeReqId = $('#admin-req-detail-area').data('active-req-id');
        
        if (activeReqId) {
            // Find support request list item in sidebar and update its status badge
            const $reqItem = $(`.request-item[data-req-id="${activeReqId}"]`);
            if ($reqItem.length > 0) {
                const $badge = $reqItem.find('.req-status');
                $badge.removeClass('status-pending').addClass('status-resolved').text('Resolved');
            }

            // Update details view status badge
            $('#detail-req-status').removeClass('status-pending').addClass('status-resolved').text('Resolved');
        }
        
        // Hide Modal
        $('#admin-confirm-resolve-modal').fadeOut(200).removeClass('active');
    });

    // ==========================================
    // Search & Filter Conversation List Sidebar
    // ==========================================
    $('#admin-convo-search').on('input', function() {
        const query = $(this).val().toLowerCase().trim();
        
        $('#admin-convo-list .convo-item').each(function() {
            const name = $(this).find('.convo-name').text().toLowerCase();
            const preview = $(this).find('.convo-preview').text().toLowerCase();
            
            if (name.includes(query) || preview.includes(query)) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    });

    // ==========================================
    // Auto-select Buyer from URL Parameter
    // ==========================================
    const urlParams = new URLSearchParams(window.location.search);
    const buyerFromUrl = urlParams.get('buyer');
    if (buyerFromUrl) {
        // Switch left tab to Conversations
        $('.tab-btn[data-target="#conversations-tab"]').click();

        // Find the convo-item with this name and click it
        const $convoItem = $('.convo-item').filter(function() {
            return $(this).find('.convo-name').text().trim().toLowerCase() === buyerFromUrl.toLowerCase();
        });

        if ($convoItem.length > 0) {
            $convoItem.click();
        } else {
            // Fallback Sync Profile Data
            const buyer = syncBuyerProfile(buyerFromUrl);
            if (buyer) {
                // Dynamically create and prepend a convo item
                const newConvoItem = $(`
                    <div class="convo-item" data-buyer-id="${buyer.name}">
                        <div class="convo-avatar-wrapper">
                            <img src="${buyer.avatar}" alt="Buyer" class="convo-avatar" onerror="this.src='/images/Avatar/default.png'">
                        </div>
                        <div class="convo-info">
                            <div class="convo-name-time">
                                <span class="convo-name">${buyer.name}</span>
                                <span class="convo-time">Just now</span>
                            </div>
                            <div class="convo-preview">Contacted from Orders</div>
                        </div>
                    </div>
                `);
                $('#admin-convo-list').prepend(newConvoItem);
                
                // Trigger click on the newly created item to load the chat
                newConvoItem.click();
                
                // Override the generic message with a complaint-specific one
                $('#admin-chat-stream').empty().append(`
                    <div class="chat-date-separator">
                        <span>Today</span>
                    </div>
                    <div class="msg-bubble-group incoming">
                        <img src="${buyer.avatar}" class="msg-avatar" onerror="this.src='/images/Avatar/avatar1.jpg'">
                        <div class="msg-bubble-content">
                            <div class="msg-text-bubble">
                                Hello! I am ${buyer.name}. I have a complaint about my order.
                            </div>
                            <div class="msg-meta">Just now</div>
                        </div>
                    </div>
                `);
            }
        }
    }
});
