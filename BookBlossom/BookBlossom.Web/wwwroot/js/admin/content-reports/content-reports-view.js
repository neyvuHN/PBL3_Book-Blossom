window.showPremiumAlert = function(title, message, type = 'success') {
    const alertClass = type === 'success' ? 'alert-success-premium' : 'alert-danger-premium';
    const iconClass = type === 'success' ? 'ph-fill ph-check-circle' : 'ph-fill ph-warning-circle';
    
    let container = document.getElementById('dynamic-alert-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'dynamic-alert-container';
        container.style.position = 'fixed';
        container.style.top = '20px';
        container.style.right = '20px';
        container.style.zIndex = '9999';
        container.style.minWidth = '300px';
        document.body.appendChild(container);
    }
    
    const alertEl = document.createElement('div');
    alertEl.className = 'custom-alert-container';
    
    const alertInner = document.createElement('div');
    alertInner.className = `custom-alert ${alertClass}`;
    alertInner.innerHTML = `
        <div class="alert-icon-box"><i class="${iconClass}"></i></div>
        <div class="alert-content-box">
            <h5 class="alert-heading">${title}</h5>
            <p class="alert-message">${message}</p>
        </div>
        <button class="btn-close-alert" onclick="this.parentElement.parentElement.remove()" style="background: none; border: none; cursor: pointer;"><i class="ph ph-x"></i></button>
    `;
    
    alertEl.appendChild(alertInner);
    container.appendChild(alertEl);
    
    setTimeout(() => {
        if (document.body.contains(alertEl)) {
            alertEl.style.animation = "fadeOutUp 0.4s ease-out forwards";
            setTimeout(() => {
                if (document.body.contains(alertEl)) alertEl.remove();
            }, 400);
        }
    }, 4000);
};

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

        // Window resize listener to handle dynamic screen resize/zoom (self-cleaning khi chuyển tab tránh rò rỉ bộ nhớ)
        const resizeHandler = () => {
            if (this.tabs && this.tabs.length > 0 && !document.body.contains(this.tabs[0])) {
                window.removeEventListener('resize', resizeHandler);
                return;
            }
            this.adjustReadMoreButtonsVisibility();
        };
        window.addEventListener('resize', resizeHandler);

        // Report filter select listener
        const filterSelect = document.getElementById('filterReportStatus');
        if (filterSelect) {
            filterSelect.addEventListener('change', (e) => {
                if (this.onReportFilterChange) {
                    this.onReportFilterChange(e.target.value);
                }
            });
        }
    }

    renderModerationItems(items) {
        if(!this.moderationContainer) return;
        this.moderationContainer.innerHTML = '';
        
        if (items.length === 0) {
            this.moderationContainer.innerHTML = `
                <div class="report-card" style="text-align: center; padding: 40px; color: #6B7280;">
                    <i class="ph ph-shield-check" style="font-size: 3rem; margin-bottom: 12px; color: #10B981;"></i>
                    <p style="margin: 0; font-size: 1.1rem; font-weight: 600;">No reported content pending review!</p>
                    <p style="margin: 4px 0 0 0; font-size: 0.9rem;">The community is clean and safe.</p>
                </div>
            `;
            return;
        }

        items.forEach(item => {
            const isHighRisk = item.reportsCount >= 5;
            const isPending = item.status === 'pending';
            
            const card = document.createElement('div');
            card.className = 'report-card';
            if (item.isHidden) {
                card.style.borderLeft = '5px solid #F59E0B';
                card.style.backgroundColor = '#FFFBEB';
            } else if (item.status === 'resolved') {
                card.style.borderLeft = '5px solid #10B981';
                card.style.opacity = '0.85';
            } else if (item.status === 'dismissed') {
                card.style.borderLeft = '5px solid #9CA3AF';
                card.style.opacity = '0.75';
            }

            let statusBadge = '';
            if (item.isHidden) {
                statusBadge = `<span class="report-type" style="background:#FEF3C7; color:#92400E; margin-left: 8px;">HIDDEN</span>`;
            }
            if (item.status === 'resolved') {
                statusBadge += `<span class="report-type" style="background:#D1FAE5; color:#065F46; margin-left: 8px;">RESOLVED</span>`;
            } else if (item.status === 'dismissed') {
                statusBadge += `<span class="report-type" style="background:#E5E7EB; color:#374151; margin-left: 8px;">DISMISSED</span>`;
            }

            card.innerHTML = `
                <div class="report-header">
                    <div>
                        <span class="report-type type-thread">THREAD POST</span>
                        ${statusBadge}
                        <span style="margin-left: 8px; color: #6B7280; font-size: 0.85rem;">by <strong>${item.author}</strong> • ${item.date}</span>
                    </div>
                    ${isHighRisk ? `<div class="report-stats"><i class="ph-fill ph-warning-circle"></i> ${item.reportsCount} Reports</div>` : `<div class="report-stats" style="color:#F59E0B"><i class="ph-fill ph-info"></i> ${item.reportsCount} Reports</div>`}
                </div>
                <div class="report-content">
                    <h4 style="font-weight:700; color:#1F2937;">${item.title}</h4>
                    <div class="report-text-container" style="background:#F9FAFB; padding:12px; border-radius:8px; border: 1px solid #F3F4F6;">
                        <p class="report-text" id="report-text-${item.id}">"${item.content}"</p>
                        <button class="btn-read-more" data-action="toggle-text" data-target="report-text-${item.id}" data-id="${item.id}">Show more</button>
                    </div>
                    
                    <div class="report-details" style="margin-top:12px; font-size:0.9rem; border-top:1px dashed #E5E7EB; padding-top:8px;">
                        <p style="margin: 4px 0;"><i class="ph ph-warning" style="color:#EF4444;"></i> <strong>Violation Type:</strong> <span class="badge-count" style="background:#EF4444; float:none; display:inline-block; font-size:0.75rem; padding:2px 8px; border-radius:4px;">${item.reasonText}</span></p>
                        <p style="margin: 4px 0; color:#4B5563;"><i class="ph ph-user-focus"></i> <strong>Reported by:</strong> ${item.reporter} - <em>"${item.description || 'No comment provided'}"</em></p>
                    </div>
                </div>
                
                ${isPending ? `
                <div class="report-actions" style="display:flex; flex-wrap:wrap; gap:12px; align-items:center; margin-top:16px; border-top:1px solid #E5E7EB; padding-top:12px;">
                    <button class="btn-action btn-keep" data-action="keep" data-id="${item.id}">
                        <i class="ph ph-check"></i> Ignore Report
                    </button>
                    ${!item.isHidden ? `
                    <button class="btn-action btn-hide" data-action="hide" data-id="${item.id}">
                        <i class="ph ph-eye-slash"></i> Hide Content
                    </button>
                    ` : ''}
                    <button class="btn-action btn-delete" data-action="delete" data-id="${item.id}">
                        <i class="ph ph-trash"></i> Delete Post
                    </button>
                    
                    <div class="penalty-input-group" style="margin-left:auto; display:flex; align-items:center; gap:8px;">
                        <label for="penalty-${item.id}" style="font-size: 0.85rem; color: #4B5563; font-weight: 500;">Deduct Pts:</label>
                        <input type="number" id="penalty-${item.id}" value="10" min="0" max="150" class="form-control" style="width:70px; padding:6px; border:1px solid #D1D5DB; border-radius:6px; font-size:0.875rem;" />
                    </div>
                </div>
                ` : ''}
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
            let mediaHtml = '';
            if (item.unboxVideoPath) {
                mediaHtml = `
                    <div class="evidence-video-wrapper" onclick="window.open('${item.unboxVideoPath}', '_blank')" style="cursor:pointer; display:flex; flex-direction:column; align-items:center; justify-content:center;">
                        <i class="ph-fill ph-play-circle" style="color:white; font-size:1.8rem;"></i>
                        <span style="color:white; font-size:0.75rem; margin-top:4px;">Play Video</span>
                    </div>
                `;
            }

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
            
            if (action === 'toggle-text') {
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
                let customPenalty = 10;
                const penaltyInput = document.getElementById(`penalty-${id}`);
                if (penaltyInput) {
                    customPenalty = parseInt(penaltyInput.value) || 0;
                }
                handler(action, id, customPenalty);
            }
        });
    }

    bindReportFilterChange(handler) {
        this.onReportFilterChange = handler;
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
