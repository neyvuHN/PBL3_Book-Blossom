class ContentReportsController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
        this.currentReportFilter = 'all';
    }

    init() {
        this.renderAll();
        
        this.view.bindModerationActions(this.handleModerationAction.bind(this));
        this.view.bindFeedbackActions(this.handleFeedbackAction.bind(this));
        this.view.bindReturnActions(this.handleReturnAction.bind(this));
        this.view.bindReportFilterChange(this.handleReportFilterChange.bind(this));
    }

    renderAll() {
        let moderationItems = this.model.getModerationItems();
        if (this.currentReportFilter === 'high') {
            moderationItems = moderationItems.filter(item => item.reportsCount >= 5);
        } else if (this.currentReportFilter === 'low') {
            moderationItems = moderationItems.filter(item => item.reportsCount < 5);
        }

        this.view.renderModerationItems(moderationItems);
        this.view.renderFeedbackItems(this.model.getFeedbackItems());
        this.view.renderReturnClaims(this.model.getReturnClaims());
        this.updateBadges();
    }

    updateBadges() {
        const modCount = this.model.getModerationItems().length;
        const feedCount = this.model.getFeedbackItems().filter(f => !f.isReplied).length;
        const returnCount = this.model.getReturnClaims().length;

        const bMod = document.getElementById('badgeModeration');
        const bFeed = document.getElementById('badgeFeedback');
        const bRet = document.getElementById('badgeReturns');

        if(bMod) bMod.textContent = modCount;
        if(bFeed) bFeed.textContent = feedCount;
        if(bRet) bRet.textContent = returnCount;
    }

    handleReportFilterChange(filterValue) {
        this.currentReportFilter = filterValue;
        this.renderAll();
    }

    handleModerationAction(action, idStr) {
        const id = parseInt(idStr);
        const items = this.model.getModerationItems();
        const index = items.findIndex(i => i.id === id);
        
        if (index > -1) {
            const item = items[index];
            if (action === 'keep') {
                showPremiumAlert('Content Kept', 'Content kept. Report dismissed.', 'success');
                this.model.moderationItems.splice(index, 1);
            } else if (action === 'delete') {
                showPremiumAlert('Content Deleted', 'Content deleted. Standard penalty points applied to author.', 'success');
                this.model.moderationItems.splice(index, 1);
            } else if (action === 'view-book' && item.bookLink) {
                window.location.href = `/Admin/Inventory?book=${encodeURIComponent(item.bookLink.title)}#books`;
                return; // no need to re-render
            }
            this.renderAll();
        }
    }

    handleFeedbackAction(action, idStr, replyText = '') {
        const id = parseInt(idStr);
        const items = this.model.getFeedbackItems();
        const item = items.find(i => i.id === id);
        
        if (item) {
            if (action === 'reply') {
                if (!replyText.trim()) {
                    showPremiumAlert('Empty Reply', 'Reply cannot be empty.', 'danger');
                    return;
                }
                item.isReplied = true;
                item.replyContent = replyText;
                showPremiumAlert('Reply Submitted', 'Reply submitted successfully.', 'success');
                this.renderAll();
            } else if (action === 'transfer') {
                const payload = {
                    BuyerName: item.author,
                    Type: "Review",
                    Rating: item.rating,
                    Content: item.content,
                    ModeratorNote: "Transferred from Content Reports."
                };
 
                fetch('/Admin/TransferComplaint', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                }).then(res => res.json()).then(data => {
                    if (data.success) {
                        showPremiumAlert('Complaint Transferred', 'Review information and stars transferred to Admin/Orders Complaints tab successfully.', 'success');
                        const index = items.findIndex(i => i.id === id);
                        if (index > -1) {
                            this.model.feedbackItems.splice(index, 1);
                            this.renderAll();
                        }
                    } else {
                        showPremiumAlert('Transfer Failed', 'Failed to transfer complaint.', 'danger');
                    }
                }).catch(err => {
                    console.error('Error:', err);
                    showPremiumAlert('Error', 'Error transferring complaint.', 'danger');
                });
            }
        }
    }

    handleReturnAction(action, idStr) {
        const id = parseInt(idStr);
        const items = this.model.getReturnClaims();
        const index = items.findIndex(i => i.id === id);
        
        if (index > -1) {
            const item = items[index];
            if (action === 'accept-return') {
                const payload = {
                    OrderId: item.orderId,
                    BookTitle: "Unknown Title", // Dummy since we don't store it in mock data precisely
                    Quantity: 1,
                    RefundAmount: 150000, // Dummy
                    ReturnReason: item.reason
                };

                fetch('/Admin/TransferReturn', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(payload)
                }).then(res => res.json()).then(data => {
                    if (data.success) {
                        showPremiumAlert('Refund Approved', 'Refund Accepted. Order moved to Returns tab in Admin/Orders.', 'success');
                        this.model.returnClaims.splice(index, 1);
                        this.renderAll();
                    } else {
                        showPremiumAlert('Process Failed', 'Failed to process return claim.', 'danger');
                    }
                }).catch(err => {
                    console.error('Error:', err);
                    showPremiumAlert('Error', 'Error processing return claim.', 'danger');
                });

            } else if (action === 'reject-return') {
                showPremiumAlert('Claim Rejected', 'Return Claim Rejected.', 'success');
                this.model.returnClaims.splice(index, 1);
                this.renderAll();
            }
        }
    }
}
