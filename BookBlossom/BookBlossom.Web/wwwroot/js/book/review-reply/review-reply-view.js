class ReviewReplyView {
    renderReply(replyText) {
        if (!replyText) return '';
        return `
            <div class="admin-reply-box" style="margin-top: 15px; margin-left: 52px; background: #fff8fa; border-radius: 8px; padding: 12px 16px; border-left: 4px solid #C2185B; box-shadow: 0 1px 3px rgba(0,0,0,0.05);">
                <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 6px;">
                    <i class="fas fa-store" style="color: #C2185B;"></i>
                    <span style="font-weight: 700; font-size: 0.9rem; color: #333;">Shop's Reply</span>
                </div>
                <p style="margin: 0; font-size: 0.85rem; color: #555; line-height: 1.5;">${replyText}</p>
            </div>
        `;
    }
}
window.ReviewReplyView = ReviewReplyView;
