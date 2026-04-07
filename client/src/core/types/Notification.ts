
export type Notification = {
    id: string;
    type: number;
    payload: string;
    isRead: boolean;
    createdAt: string;
    // Display fields resolved on the frontend
    displayTitle: string;
    displayPreview?: string;
};