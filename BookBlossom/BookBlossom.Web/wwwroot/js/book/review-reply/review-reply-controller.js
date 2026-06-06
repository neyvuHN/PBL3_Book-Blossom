class ReviewReplyController {
    constructor() {
        this.model = new window.ReviewReplyModel();
        this.view = new window.ReviewReplyView();
    }

    getReplyHtml(reviewObj) {
        if (!reviewObj) return '';
        const rid = reviewObj.reviewID || reviewObj.reviewId || reviewObj.id;
        const replyText = this.model.getReply(reviewObj);
        return this.view.renderReply(replyText);
    }
}
window.ReviewReplyController = ReviewReplyController;
