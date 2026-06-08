class ContentReportsController {
    constructor(model, view) {
        this.model = model;
        this.view = view;
        this.currentReportFilter = 'all';
    }

    async init() {
        await this.loadAndRender();
        
        this.view.bindModerationActions(this.handleModerationAction.bind(this));
        this.view.bindFeedbackActions(this.handleFeedbackAction.bind(this));
        this.view.bindReturnActions(this.handleReturnAction.bind(this));
        this.view.bindReportFilterChange(this.handleReportFilterChange.bind(this));
    }

    async loadAndRender() {
        await this.model.loadAll();
        this.renderAll();
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
        const modCount = this.model.getModerationItems().filter(m => m.status === 'pending').length;
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

    async handleModerationAction(action, idStr, customPenalty = 10) {
        const id = parseInt(idStr);
        const items = this.model.getModerationItems();
        const item = items.find(i => i.id === id);
        
        if (item) {
            try {
                if (action === 'keep') {
                    await this.model.keepReport(id);
                    showPremiumAlert('Report Ignored', 'Violation report dismissed successfully.', 'success');
                } else if (action === 'hide') {
                    await this.model.hidePost(id, item.postId, customPenalty);
                    showPremiumAlert('Content Hidden', `Content has been hidden. Penalized the author -${customPenalty} reputation points.`, 'success');
                } else if (action === 'delete') {
                    await this.model.deletePost(id, item.postId, customPenalty);
                    showPremiumAlert('Content Deleted', `Content deleted permanently from the database. Penalized the author -${customPenalty} reputation points.`, 'success');
                }
                await this.loadAndRender();
            } catch (err) {
                console.error(err);
                showPremiumAlert('Action Failed', err.message || 'Failed to process moderation action.', 'danger');
            }
        }
    }

    async handleFeedbackAction(action, idStr, replyText = '') {
        const id = parseInt(idStr);
        const items = this.model.getFeedbackItems();
        const item = items.find(i => i.id === id);
        
        if (item) {
            if (action === 'reply') {
                if (!replyText.trim()) {
                    showPremiumAlert('Empty Reply', 'Reply cannot be empty.', 'danger');
                    return;
                }
                await this.model.replyFeedback(id, replyText);
                showPremiumAlert('Reply Saved', 'Reply updated successfully.', 'success');
                await this.loadAndRender();
            } else if (action === 'delete-reply') {
                if(confirm('Are you sure you want to delete this reply?')) {
                    try {
                        await this.model.deleteFeedbackReply(id);
                        showPremiumAlert('Reply Deleted', 'The reply has been removed.', 'success');
                        await this.loadAndRender();
                    } catch (err) {
                        console.error(err);
                        showPremiumAlert('Action Failed', err.message || 'Failed to delete reply.', 'danger');
                    }
                }
            } else if (action === 'transfer') {
                const payload = {
                    BuyerName: item.author,
                    Type: "Review",
                    Rating: item.rating,
                    Content: item.content,
                    ModeratorNote: "Transferred from Content Reports."
                };
 
                try {
                    await window.apiClient.apiPost('/Admin/TransferComplaint', payload);
                    showPremiumAlert('Complaint Transferred', 'Review information and stars transferred successfully.', 'success');
                    await this.loadAndRender();
                } catch (err) {
                    console.error(err);
                    showPremiumAlert('Transfer Failed', err.message || 'Failed to transfer complaint.', 'danger');
                }
            }
        }
    }

    async handleReturnAction(action, idStr) {
        if (action === 'view-order-details') {
            try {
                const detail = await this.model.fetchOrderDetails(idStr);
                if (detail) {
                    this.view.showOrderDetailsModal(detail);
                } else {
                    showPremiumAlert('Error', 'Order not found.', 'danger');
                }
            } catch (err) {
                console.error(err);
                showPremiumAlert('Error', 'Failed to load order details.', 'danger');
            }
            return;
        }

        const id = parseInt(idStr);
        try {
            if (action === 'accept-return') {
                await this.model.reviewReturnClaim(id, true);
                showPremiumAlert('Return Approved', 'Return Request has been approved and refund processed.', 'success');
            } else if (action === 'reject-return') {
                const rejectReason = prompt("Please enter the reason for rejection:") || "Rejected by moderator";
                await this.model.reviewReturnClaim(id, false, rejectReason);
                showPremiumAlert('Return Rejected', 'Return Claim has been rejected successfully.', 'success');
            }
            await this.loadAndRender();
        } catch (err) {
            console.error(err);
            showPremiumAlert('Review Failed', err.message || 'Failed to review return request.', 'danger');
        }
    }
}
