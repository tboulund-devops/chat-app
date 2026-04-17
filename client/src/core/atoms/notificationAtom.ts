import { atom } from 'jotai';
import type { Notification } from '../types/Notification';

export const notificationsAtom = atom<Notification[]>([]);
notificationsAtom.debugLabel = 'Notifications Atom';

// Derived: unread count, useful for the bell badge
export const unreadCountAtom = atom(
    (get) => get(notificationsAtom).filter((n) => !n.isRead).length
);