class ContentReportsView {
    constructor() {
        this.tabs = document.querySelectorAll('.sub-menu-item');
        this.contentTabs = document.querySelectorAll('.content-tab');
        this.btnToggleSidebar = document.getElementById('btnToggleSidebar');
        this.sidebar = document.getElementById('reportsSidebar');
        this.mainContent = document.getElementById('mainContentArea');
        
        this.moderationContainer = document.getElementById('moderationListContainer');
        this.feedbackContainer = document.getElementById('feedbackListContainer');
        this.returnsContainer = document.getElementById('returnsListContainer');

        this.bindEvents();
    }

    bindEvents() {
        // Tab switching
        this.tabs.forEach(tab => {
            tab.addEventListener('click', () => {
                const target = tab.getAttribute('data-target');
                if(!target) return;
                
                this.tabs.forEach(t => t.classList.remove('active'));
                tab.classList.add('active');

                this.contentTabs.forEach(ct => ct.classList.remove('active'));
                document.getElementById(target).classList.add('active');

                // Adjust read-more buttons visibility when tab becomes active
                this.adjustReadMoreButtonsVisibility();
            });
        });

        // Sidebar toggle
        if(this.btnToggleSidebar) {
            this.btnToggleSidebar.addEventListener('click', () => {
                this.sidebar.classList.toggle('hidden');
                if(this.sidebar.classList.contains('hidden')){
                    this.mainContent.style.marginLeft = '0';
                } else {
                    this.mainContent.style.marginLeft = '0'; // Flex layout handles this automatically if configured correctly, but just in case
                }
                
                // Recalculate visibility because layout changed
                this.adjustReadMoreButtonsVisibility();
            });
        }

        // Window resize listener to handle dynamic screen resize/zoom
        window.addEventListener('resize', () => {
            this.adjustReadMoreButtonsVisibility();
        });
    }

    renderModerationItems(items) {
        if(!this.moderationContainer) return;
        this.moderationContainer.innerHTML = '';
        
        items.forEach(item => {
            const isHighRisk = item.reportsCount >= 5;
            
            let bookLinkHtml = '';
            if (item.bookLink) {
                bookLinkHtml = `
                    <div class="report-book-link">
                        <img src="${item.bookLink.image}" alt="Book Cover" />
                        <div class="report-book-link-info">
                            <h5>${item.bookLink.title}</h5>
                            <p>${item.bookLink.author}</p>
                        </div>
                        <button class="btn-action" style="margin-left: auto; border: 1px solid #D1D5DB; background: white;"><i class="ph ph-arrow-square-out"></i> View Book</button>
                    </div>
                `;
            }

            const card = document.createElement('div');
            card.className = 'report-card';
            card.innerHTML = `
                <div class="report-header">
                    <div>
                        <span class="report-type ${item.type === 'thread' ? 'type-thread' : 'type-review'}">${item.type.toUpperCase()}</span>
                        <span style="margin-left: 8px; color: #6B7280; font-size: 0.85rem;">by ${item.author} • ${item.date}</span>
                    </div>
                    ${isHighRisk ? `<div class="report-stats"><i class="ph-fill ph-warning-circle"></i> ${item.reportsCount} Reports</div>` : `<div class="report-stats" style="color:#F59E0B"><i class="ph-fill ph-info"></i> ${item.reportsCount} Reports</div>`}
                </div>
                <div class="report-content">
                    <h4>${item.title}</h4>
                    <div class="report-text-container">
                        <p class="report-text" id="report-text-${item.id}">"${item.content}"</p>
                        <button class="btn-read-more" data-action="toggle-text" data-target="report-text-${item.id}" data-id="${item.id}">Show more</button>
                    </div>
                    ${bookLinkHtml}
                </div>
                <div class="report-actions">
                    <button class="btn-action btn-keep" data-action="keep" data-id="${item.id}">
                        <i class="ph ph-check"></i> Keep
                    </button>
                    <button class="btn-action btn-hide" data-action="hide" data-id="${item.id}">
                        <i class="ph ph-eye-closed"></i> Hide
                    </button>
                    <button class="btn-action btn-delete" data-action="delete" data-id="${item.id}">
                        <i class="ph ph-trash"></i> Delete
                    </button>
                    <div class="penalty-input-group" id="penaltyGroup-${item.id}" style="display:none;">
                        <label style="font-size: 0.8rem; color:#4B5563;">Deduct Points:</label>
                        <input type="number" min="0" max="100" placeholder="e.g. 10" id="penaltyInput-${item.id}" />
                        <button class="btn-action btn-primary btn-sm" data-action="confirm-hide" data-id="${item.id}" style="padding: 4px 8px;">Confirm</button>
                    </div>
                </div>
            `;
            this.moderationContainer.appendChild(card);
        });

        this.adjustReadMoreButtonsVisibility();
    }

    renderFeedbackItems(items) {
        if(!this.feedbackContainer) return;
        this.feedbackContainer.innerHTML = '';
        
        items.forEach(item => {
            const stars = Array(5).fill(0).map((_, i) => i < item.rating ? '<i class="ph-fill ph-star" style="color:#FBBF24"></i>' : '<i class="ph ph-star" style="color:#D1D5DB"></i>').join('');
            
            const card = document.createElement('div');
            card.className = 'report-card';
            card.innerHTML = `
                <div class="report-header">
                    <div>
                        <span class="report-type ${item.type === 'product_review' ? 'type-review' : 'type-thread'}">
                            ${item.type === 'product_review' ? 'Product Review' : 'Community Review'}
                        </span>
                        <span style="margin-left: 8px; font-weight: 600;">${item.bookTitle}</span>
                    </div>
                    <div style="font-size: 0.85rem; color: #6B7280;">${item.date}</div>
                </div>
                <div class="report-content" style="margin-bottom: 12px;">
                    <div style="margin-bottom: 8px;">${stars}</div>
                    <div class="report-text-container">
                        <p class="report-text" id="feedback-text-${item.id}">"${item.content}"</p>
                        <button class="btn-read-more" data-action="toggle-text" data-target="feedback-text-${item.id}">Show more</button>
                    </div>
                    <p style="font-size: 0.85rem; color: #6B7280; margin-top: 4px;">- ${item.author}</p>
                </div>
                
                ${item.isReplied ? `
                    <div style="background: #F3F4F6; padding: 12px; border-radius: 8px; margin-bottom: 12px; font-size: 0.9rem;">
                        <strong>Your Reply:</strong> ${item.replyContent}
                    </div>
                ` : `
                    <div style="margin-bottom: 12px; display:flex; gap: 8px;">
                        <input type="text" class="form-control" placeholder="Type your reply here..." style="font-size:0.9rem;" id="replyInput-${item.id}">
                        <button class="btn-action btn-primary" data-action="reply" data-id="${item.id}">Reply</button>
                    </div>
                `}

                <div class="report-actions" style="margin-top:0; padding-top:12px;">
                    ${item.type === 'product_review' ? `
                        <button class="btn-action btn-transfer" data-action="transfer" data-id="${item.id}">
                            <i class="ph ph-arrows-left-right"></i> Transfer to Admin/Orders Complaints
                        </button>
                    ` : ''}
                </div>
            `;
            this.feedbackContainer.appendChild(card);
        });

        this.adjustReadMoreButtonsVisibility();
    }

    renderReturnClaims(items) {
        if(!this.returnsContainer) return;
        this.returnsContainer.innerHTML = '';
        
        items.forEach(item => {
            const mediaHtml = item.media.map(m => {
                if(m.type === 'video') {
                    return `<div class="evidence-video-wrapper"><i class="ph-fill ph-play-circle"></i></div>`;
                }
                return `<img src="${m.url}" class="evidence-item" alt="Evidence" />`;
            }).join('');

            const card = document.createElement('div');
            card.className = 'report-card';
            card.innerHTML = `
                <div class="report-header">
                    <div>
                        <span class="report-type" style="background:#FEE2E2; color:#B91C1C;">RETURN CLAIM</span>
                        <span style="margin-left: 8px; font-weight: 600;">Order: ${item.orderId}</span>
                    </div>
                    <div style="font-size: 0.85rem; color: #6B7280;">${item.date}</div>
                </div>
                <div class="report-content">
                    <h4>Reason: ${item.reason}</h4>
                    <div class="report-text-container">
                        <p class="report-text" id="return-text-${item.id}">${item.description}</p>
                        <button class="btn-read-more" data-action="toggle-text" data-target="return-text-${item.id}">Show more</button>
                    </div>
                    <p style="font-size: 0.85rem; color: #6B7280; margin-top: 4px;">Buyer: ${item.buyer}</p>
                    
                    <div class="evidence-gallery">
                        ${mediaHtml}
                    </div>
                </div>
                <div class="report-actions">
                    <button class="btn-action btn-primary" data-action="accept-return" data-id="${item.id}">
                        <i class="ph ph-check-circle"></i> Accept & Refund
                    </button>
                    <button class="btn-action btn-delete" data-action="reject-return" data-id="${item.id}">
                        <i class="ph ph-x-circle"></i> Reject Claim
                    </button>
                </div>
            `;
            this.returnsContainer.appendChild(card);
        });

        this.adjustReadMoreButtonsVisibility();
    }

    bindModerationActions(handler) {
        if(!this.moderationContainer) return;
        this.moderationContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('.btn-action, .btn-read-more');
            if(!btn) return;
            
            const action = btn.getAttribute('data-action');
            const id = btn.getAttribute('data-id');
            
            if(action === 'hide') {
                // Show penalty input
                const group = document.getElementById(`penaltyGroup-${id}`);
                if(group) {
                    group.style.display = 'flex';
                    btn.style.display = 'none'; // hide the original hide button
                }
            } else if (action === 'confirm-hide') {
                const input = document.getElementById(`penaltyInput-${id}`);
                const points = input ? parseInt(input.value) : 0;
                handler('hide', id, points);
            } else if (action === 'toggle-text') {
                const targetId = btn.getAttribute('data-target') || `report-text-${id}`;
                const textElem = document.getElementById(targetId);
                if (textElem) {
                    textElem.classList.toggle('expanded');
                    if (textElem.classList.contains('expanded')) {
                        btn.innerText = 'Show less';
                    } else {
                        btn.innerText = 'Show more';
                    }
                }
            } else {
                handler(action, id);
            }
        });
    }

    bindFeedbackActions(handler) {
        if(!this.feedbackContainer) return;
        this.feedbackContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('.btn-action, .btn-read-more');
            if(!btn) return;
            
            const action = btn.getAttribute('data-action');
            const id = btn.getAttribute('data-id');
            
            if (action === 'toggle-text') {
                const targetId = btn.getAttribute('data-target');
                const textElem = document.getElementById(targetId);
                if (textElem) {
                    textElem.classList.toggle('expanded');
                    btn.innerText = textElem.classList.contains('expanded') ? 'Show less' : 'Show more';
                }
            } else if(action === 'reply') {
                const input = document.getElementById(`replyInput-${id}`);
                const replyText = input ? input.value : '';
                handler(action, id, replyText);
            } else {
                handler(action, id);
            }
        });
    }

    bindReturnActions(handler) {
        if(!this.returnsContainer) return;
        this.returnsContainer.addEventListener('click', (e) => {
            const btn = e.target.closest('.btn-action, .btn-read-more');
            if(!btn) return;
            
            const action = btn.getAttribute('data-action');
            const id = btn.getAttribute('data-id');
            if (action === 'toggle-text') {
                const targetId = btn.getAttribute('data-target');
                const textElem = document.getElementById(targetId);
                if (textElem) {
                    textElem.classList.toggle('expanded');
                    btn.innerText = textElem.classList.contains('expanded') ? 'Show less' : 'Show more';
                }
            } else {
                handler(action, id);
            }
        });
    }

    adjustReadMoreButtonsVisibility() {
        // Wait a tiny bit for rendering/layout calculations to complete
        setTimeout(() => {
            const containers = document.querySelectorAll('.report-text-container');
            containers.forEach(container => {
                const textElem = container.querySelector('.report-text');
                const btn = container.querySelector('.btn-read-more');
                if (textElem && btn) {
                    // If the container is currently hidden (e.g. inactive tab), clientHeight will be 0.
                    // Keep the button visible in this case so it can be evaluated when the tab becomes active.
                    if (textElem.clientHeight === 0) {
                        return;
                    }
                    
                    if (textElem.classList.contains('expanded')) {
                        btn.style.display = 'inline-block';
                        return;
                    }

                    // Check if scrollHeight is greater than clientHeight (text is actually truncated)
                    if (textElem.scrollHeight <= textElem.clientHeight) {
                        btn.style.display = 'none';
                    } else {
                        btn.style.display = 'inline-block';
                    }
                }
            });
        }, 50);
    }
}
