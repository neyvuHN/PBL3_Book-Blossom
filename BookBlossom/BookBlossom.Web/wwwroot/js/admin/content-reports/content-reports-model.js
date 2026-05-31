class ContentReportsModel {
    constructor() {
        this.moderationItems = [
            {
                id: 1,
                type: 'thread',
                title: 'Suspicious link in book discussion',
                content: 'Check out this site for free pdfs of the new release...',
                reportsCount: 8,
                author: 'user_spammer123',
                date: '2 hours ago',
                status: 'pending'
            },
            {
                id: 2,
                type: 'review',
                title: 'Inappropriate language in review',
                content: 'This book was absolutely **** and the author is a ****.',
                reportsCount: 5,
                author: 'angry_reader',
                date: '4 hours ago',
                status: 'pending'
            },
            {
                id: 3,
                type: 'thread',
                title: 'Phone number sharing',
                content: 'Call me at 0912345678 if you want to trade books.',
                reportsCount: 2,
                author: 'trader_joe',
                date: '1 day ago',
                status: 'pending' // < 5 reports but visible
            }
        ];

        this.feedbackItems = [
            {
                id: 1,
                type: 'product_review',
                bookTitle: 'The Great Gatsby',
                rating: 2,
                content: 'The packaging was terrible, book arrived with bent corners.',
                author: 'Alice Smith',
                date: 'Yesterday',
                isReplied: false
            },
            {
                id: 2,
                type: 'community_review',
                bookTitle: 'Atomic Habits',
                rating: 5,
                content: 'Life-changing book! Highly recommend to everyone.',
                author: 'Bob Johnson',
                date: '2 days ago',
                isReplied: true,
                replyContent: 'Thank you for your kind words!'
            }
        ];

        this.returnClaims = [
            {
                id: 1,
                orderId: 'ORD-8715',
                buyer: 'Charlie Brown',
                reason: 'Received wrong book',
                description: 'I ordered a chemistry textbook but received a history one instead. See video of unboxing.',
                media: [
                    { type: 'video', url: '/samples/dummy_video.mp4' },
                    { type: 'image', url: 'https://placehold.co/400x300?text=Wrong+Book' }
                ],
                date: 'Today',
                status: 'pending'
            }
        ];
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
}
