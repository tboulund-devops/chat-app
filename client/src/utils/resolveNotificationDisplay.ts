import type { Notification } from "../core/types/Notification";

export function resolveNotificationDisplay(
    notification: Notification,
) : Notification {
    const payload = JSON.parse(notification.payload);
    const type = Number(notification.type);
    
    
    if(type === 0) {
        return {
            ...notification,
            displayTitle: `You were poked by ${payload.pokerName ?? "Someone"}`,
        };
    }
    
    if(type === 1) {
        return {
            ...notification,
            displayTitle: `💬 ${payload.senderName ?? "Someone"} in ${ payload.roomName ?? "Some room"} `,
            displayPreview: payload.content,
        };
    }
    
    return notification;
}