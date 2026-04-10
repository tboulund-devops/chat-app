import { House, MessageSquare, Bot } from 'lucide-react'
import { NavLink, useLocation } from 'react-router-dom'
import { useAtomValue } from 'jotai'
import { authAtom } from '../../core/atoms/authAtom'

const baseClass =
    'flex items-center gap-3 rounded-xl px-3 py-2 text-sm transition-colors'
const activeClass = 'bg-zinc-900 text-white'
const inactiveClass = 'text-zinc-600 hover:bg-zinc-100'

export default function Sidebar() {
    const auth = useAtomValue(authAtom)
    const location = useLocation()
    const showMyRoom = location.pathname.startsWith('/rooms/')

    return (
        <aside className="hidden w-64 shrink-0 border-r border-zinc-200 bg-zinc-50 lg:flex lg:flex-col">
            <div className="flex items-center gap-3 px-4 py-5">
                <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-zinc-900 text-white">
                    <Bot className="h-4 w-4" />
                </div>
                <span className="text-sm font-semibold">Incident Tracker</span>
            </div>

            <div className="px-4">
                <p className="mb-3 text-[10px] font-semibold uppercase tracking-[0.2em] text-zinc-400">
                    Main
                </p>

                <nav className="space-y-1">
                    <NavLink
                        to="/rooms"
                        className={({ isActive }) =>
                            `${baseClass} ${isActive && location.pathname === '/rooms' ? activeClass : inactiveClass}`
                        }
                    >
                        <House className="h-4 w-4" />
                        Rooms
                    </NavLink>                    
                </nav>
            </div>

            {showMyRoom && (
                <div className="px-4 pt-6">
                    <p className="mb-3 text-[10px] font-semibold uppercase tracking-[0.2em] text-zinc-400">
                        My Rooms
                    </p>
                    <div className="flex items-center gap-3 rounded-xl bg-zinc-900 px-3 py-2 text-sm text-white">
                        <House className="h-4 w-4" />
                        Active Room
                    </div>
                </div>
            )}

            <div className="mt-auto border-t border-zinc-200 px-4 py-4">
                <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-zinc-900">
                        {auth.status === 'authenticated' ? auth.user.username : 'Guest'}
                    </p>
                    <p className="truncate text-xs text-zinc-500">
                        {auth.status === 'authenticated' ? auth.user.email : ''}
                    </p>
                </div>
            </div>
        </aside>
    )
}