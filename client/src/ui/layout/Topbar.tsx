import { useState } from 'react'
import { LogOut, Bell } from 'lucide-react'
import { useAtom, useAtomValue, useSetAtom } from 'jotai'
import { authAtom } from '../../core/atoms/authAtom'
import { authApi } from '../../core/controllers/authApi'
import { notificationsAtom, unreadCountAtom } from '../../core/atoms/notificationAtom'
import { notificationApi } from '../../core/controllers/notificationApi'

export default function Topbar() {
    const setAuth = useSetAtom(authAtom)
    const [open, setOpen] = useState(false)
    const [notifications, setNotifications] = useAtom(notificationsAtom)
    const unreadCount = useAtomValue(unreadCountAtom)

    const handleMarkRead = async (id: string) => {
        await notificationApi.markRead(id)
        setNotifications((prev) =>
            prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
        )
    }

    const handleMarkAllRead = async () => {
        await notificationApi.markAllRead();
        setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
    };

    const handleLogout = async () => {
        try {
            await authApi.logout()
            setAuth({ status: 'unauthenticated' })
        } catch (error) {
            console.error('Logout failed:', error)
        }
    }

    return (
        <header className="flex h-14 items-center justify-end border-b border-zinc-200 px-6">
            <div className="flex items-center gap-2">

                {/* Bell */}
                <div className="relative">
                    <button
                        onClick={() => setOpen((o) => !o)}
                        className="relative rounded-full p-2 text-zinc-500 hover:bg-zinc-100"
                    >
                        <Bell className="h-4 w-4" />
                        {unreadCount > 0 && (
                            <span className="absolute right-1 top-1 flex h-4 w-4 items-center
                                            justify-center rounded-full bg-red-500 text-[10px] text-white">
                                {unreadCount}
                            </span>
                        )}
                    </button>

                    {open && (
                        <div className="absolute right-0 top-10 z-50 w-80 rounded-xl border
                                        border-zinc-200 bg-white shadow-lg">
                            <div className="border-b border-zinc-100 px-4 py-3 flex items-center justify-between">
                                <span className="text-sm font-semibold text-zinc-900">Notifications</span>
                                {unreadCount > 0 && (
                                    <button
                                        onClick={handleMarkAllRead}
                                        className="text-xs text-zinc-500 hover:text-zinc-900"
                                    >
                                        Mark all read
                                    </button>
                                )}
                            </div>
                            <ul className="max-h-80 overflow-y-auto">
                                {notifications.length === 0 ? (
                                    <li className="px-4 py-6 text-center text-sm text-zinc-400">
                                        All caught up!
                                    </li>
                                ) : (
                                    notifications.map((n) => (
                                        <li
                                            key={n.id}
                                            onClick={() => handleMarkRead(n.id)}
                                            className={`cursor-pointer px-4 py-3 text-sm hover:bg-zinc-50
                    ${n.isRead ? 'text-zinc-400' : 'font-medium text-zinc-800'}`}
                                        >
                                            <div>{n.displayTitle}</div>
                                            {n.displayPreview && (
                                                <div className="mt-0.5 truncate text-xs text-zinc-400">
                                                    {n.displayPreview}
                                                </div>
                                            )}
                                            <div className="mt-0.5 text-xs text-zinc-400">
                                                {new Date(n.createdAt).toLocaleTimeString()}
                                            </div>
                                        </li>
                                    ))
                                )}
                            </ul>
                        </div>
                    )}
                </div>



                {/* Logout */}
                <button
                    onClick={handleLogout}
                    className="rounded-full p-2 text-red-500 hover:bg-red-50"
                >
                    <LogOut className="h-4 w-4" />
                </button>
            </div>
        </header>
    )
}