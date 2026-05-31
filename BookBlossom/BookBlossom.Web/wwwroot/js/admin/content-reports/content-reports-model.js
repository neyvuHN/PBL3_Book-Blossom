class ContentReportsModel {
    constructor() {
        this.moderationItems = [
            {
                id: 1,
                type: 'thread',
                title: 'Suspicious link in book discussion',
                content: 'Check out this site for free pdfs of the new release... I have been using it for a while and you can get almost any book for free. It is totally safe and you do not need to pay anything. Just click the link and download. Sometimes there are ads but just close them. Really guys, why pay when you can get it for free? This is the best way to read books online without spending money. Also they have audiobooks!',
                reportsCount: 8,
                author: 'user_spammer123',
                date: '2 hours ago',
                status: 'pending',
                bookLink: {
                    title: 'The Secret Garden',
                    author: 'Frances Hodgson Burnett',
                    image: '/images/Book/book1.jpg'
                }
            },
            {
                id: 2,
                type: 'review',
                title: 'Inappropriate language in review',
                content: 'This book was absolutely **** and the author is a ****. I cannot believe I spent my hard-earned money on this garbage. The plot makes no sense, the characters are flat, and the ending was rushed. DO NOT BUY THIS BOOK. If you do, you will regret it forever. I want my money back but the store refused.',
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
