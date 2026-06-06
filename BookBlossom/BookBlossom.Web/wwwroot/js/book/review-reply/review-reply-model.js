class ReviewReplyModel {
    getReply(review) {
        if (!review || !review.adminReplyContent) return null;
        return review.adminReplyContent;
    }
}
window.ReviewReplyModel = ReviewReplyModel;
