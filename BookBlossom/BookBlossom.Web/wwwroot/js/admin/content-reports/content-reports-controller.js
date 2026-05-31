class ContentReportsController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
    }

    init() {
        this.renderAll();
        
        this.view.bindModerationActions(this.handleModerationAction.bind(this));
        this.view.bindFeedbackActions(this.handleFeedbackAction.bind(this));
        this.view.bindReturnActions(this.handleReturnAction.bind(this));
    }

    renderAll() {
        this.view.renderModerationItems(this.model.getModerationItems());
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

    handleModerationAction(action, idStr, penaltyPoints = 0) {
        const id = parseInt(idStr);
        const items = this.model.getModerationItems();
        const index = items.findIndex(i => i.id === id);
        
        if (index > -1) {
            if (action === 'keep') {
                alert('Content kept. Report dismissed.');
                this.model.moderationItems.splice(index, 1);
            } else if (action === 'delete') {
                alert('Content deleted. Standard penalty points applied to author.');
                this.model.moderationItems.splice(index, 1);
            } else if (action === 'confirm-hide') {
                if (isNaN(penaltyPoints) || penaltyPoints < 0) {
                    alert('Please enter a valid penalty score.');
                    return;
                }
                alert(`Content hidden. User penalized by ${penaltyPoints} points.`);
                this.model.moderationItems.splice(index, 1);
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
                    alert('Reply cannot be empty.');
                    return;
                }
                item.isReplied = true;
                item.replyContent = replyText;
                alert('Reply submitted successfully.');
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
                        alert('Review information and stars transferred to Admin/Orders Complaints tab successfully.');
                        const index = items.findIndex(i => i.id === id);
                        if (index > -1) {
                            this.model.feedbackItems.splice(index, 1);
                            this.renderAll();
                        }
                    } else {
                        alert('Failed to transfer complaint.');
                    }
                }).catch(err => {
                    console.error('Error:', err);
                    alert('Error transferring complaint.');
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
                        alert('Refund Accepted. Order moved to Returns tab in Admin/Orders.');
                        this.model.returnClaims.splice(index, 1);
                        this.renderAll();
                    } else {
                        alert('Failed to process return claim.');
                    }
                }).catch(err => {
                    console.error('Error:', err);
                    alert('Error processing return claim.');
                });

            } else if (action === 'reject-return') {
                alert('Return Claim Rejected.');
                this.model.returnClaims.splice(index, 1);
                this.renderAll();
            }
        }
    }
}
