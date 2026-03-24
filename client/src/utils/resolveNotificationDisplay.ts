import type { Notification } from "../core/types/Notification";
import { userApi } from "../core/controllers/userApi";

export async function resolveNotificationDisplay(
    notification: Notification,
    getRoomName: (roomId: string) => string | undefined
): Promise<Notification> {
    const payload = JSON.parse(notification.payload);
    const type = Number(notification.type);

    if (type === 0) { // Poke
        const pokerId = payload.PokerId ?? payload.from;
        let pokerName = "Someone";
        try {
            const user = await userApi.getById(pokerId);
            pokerName = user.username;
        } catch { /* fallback to "Someone" */ }

        return { ...notification, displayTitle: `👉 You were poked by ${pokerName}` };
    }

    if (type === 1) { // NewMessage
        const roomId = payload.RoomId ?? payload.roomId ?? payload.requestRoomId;
        const roomName = getRoomName(roomId) ?? "a room";

        return {
            ...notification,
            displayTitle: `💬 New message in ${roomName}`,
            displayPreview: payload.Content ?? payload.content,
        };
    }

    return notification;
}