import { api } from '../../utils/api';
import type { Notification } from '../types/Notification';

const endpoint = '/api/notifications';

export const notificationApi = {
    getUnread: async (): Promise<Notification[]> => {
        return await api<Notification[]>(endpoint);
    },

    markRead: async (notificationId: string): Promise<void> => {
        await api(`${endpoint}/${notificationId}/read`, {
            init: { method: 'PATCH' },
        });
    },

    poke: async (targetUserId: string): Promise<void> => {
        await api(`${endpoint}/poke/${targetUserId}`, {
            init: { method: 'POST' },
        });
    },

    markAllRead: async (): Promise<void> => {
        await api('/api/notifications/read-all', {
            init: { method: 'PATCH' },
        });
    },
};