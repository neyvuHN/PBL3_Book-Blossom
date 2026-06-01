class ContentReportsModel {
    constructor() {
        this.moderationItems = [];
        this.feedbackItems = [];
        this.returnClaims = [];
    }

    getModerationItems() {
        return this.moderationItems;
    }

    getFeedbackItems() {
        return this.feedbackItems;
    }

    getReturnClaims() {
        return this.returnClaims;
    }

    async loadAll() {
        await Promise.all([
            this.loadModerationItems(),
            this.loadFeedbackItems(),
            this.loadReturnClaims()
        ]);
    }

    async loadModerationItems() {
        try {
            const data = await window.apiClient.apiGet('/api/moderation/Report');
            if (Array.isArray(data)) {
                this.moderationItems = data.map(r => ({
                    id: r.reportID,
                    postId: r.postID,
                    type: 'thread',
                    title: r.post?.title || `Report #${r.reportID}`,
                    content: r.post?.content || `No content available`,
                    reportsCount: r.post?.reportCount || 1,
                    author: r.post?.authorUsername || "Unknown",
                    date: r.createdAt ? new Date(r.createdAt).toLocaleDateString() : 'N/A',
                    status: r.isAccurate === null ? 'pending' : (r.isAccurate ? 'resolved' : 'dismissed'),
                    isHidden: r.post?.isHidden || false,
                    reasonText: r.reasonText || 'Spam',
                    description: r.description || '',
                    reporter: r.reporterUsername || 'Unknown'
                }));
            }
        } catch (error) {
            console.error('Error loading moderation items:', error);
            this.moderationItems = [];
        }
    }

    async loadFeedbackItems() {
        try {
            const data = await window.apiClient.apiGet('/api/Review');
            if (Array.isArray(data)) {
                this.feedbackItems = data.map(rev => {
                    const localReply = localStorage.getItem(`review_reply_${rev.reviewID}`);
                    return {
                        id: rev.reviewID,
                        type: rev.blindBookID ? 'community_review' : 'product_review',
                        bookTitle: rev.blindBookID ? `Mystery Book #${rev.blindBookID}` : `Book #${rev.bookID}`,
                        rating: rev.rating,
                        content: rev.content,
                        author: rev.customerName || 'Anonymous',
                        date: rev.createdAt ? new Date(rev.createdAt).toLocaleDateString() : 'N/A',
                        isReplied: !!localReply,
                        replyContent: localReply || ''
                    };
                });
            }
        } catch (error) {
            console.error('Error loading feedback items:', error);
            this.feedbackItems = [];
        }
    }

    async loadReturnClaims() {
        try {
            const data = await window.apiClient.apiGet('/api/Return/staff');
            if (Array.isArray(data)) {
                this.returnClaims = data.filter(r => r.returnStatus === 0).map(ret => ({
                    id: ret.returnRequestID,
                    orderId: `ORD-${ret.orderID}`,
                    buyer: ret.customerName || 'Customer',
                    reason: ret.returnReason || 'No reason provided',
                    description: `Quantity: ${ret.returnQuantity}. Reason: ${ret.returnReason}`,
                    unboxVideoPath: ret.unboxVideoPath,
                    date: ret.requestDate ? new Date(ret.requestDate).toLocaleDateString() : 'N/A',
                    status: 'pending'
                }));
            }
        } catch (error) {
            console.error('Error loading return claims:', error);
            this.returnClaims = [];
        }
    }

    async keepReport(reportId) {
        return await window.apiClient.apiPost(`/api/moderation/Report/${reportId}/process?isAccurate=false`);
    }

    async hidePost(reportId, postId, customDeduction = 10) {
        await window.apiClient.apiPost(`/api/moderation/Report/${reportId}/process?isAccurate=true&customDeduction=${customDeduction}`);
        return await window.apiClient.apiPost(`/api/Thread/${postId}/hide?isHidden=true`);
    }

    async deletePost(reportId, postId, customDeduction = 10) {
        await window.apiClient.apiPost(`/api/moderation/Report/${reportId}/process?isAccurate=true&customDeduction=${customDeduction}`);
        return await window.apiClient.apiDelete(`/api/Thread/${postId}`);
    }

    async replyFeedback(reviewId, replyText) {
        localStorage.setItem(`review_reply_${reviewId}`, replyText);
        return { success: true };
    }

    async reviewReturnClaim(requestId, isApproved, rejectReason = '') {
        const payload = {
            isApproved: isApproved,
            rejectReason: rejectReason
        };
        return await window.apiClient.apiPost(`/api/Return/staff/${requestId}/review`, payload);
    }
}
