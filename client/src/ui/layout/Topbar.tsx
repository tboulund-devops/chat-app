import { LogOut, Moon } from 'lucide-react'
import { useSetAtom } from 'jotai'
import { authAtom } from '../../core/atoms/authAtom'
import { authApi } from '../../core/controllers/authApi'

export default function Topbar() {
    const setAuth = useSetAtom(authAtom)

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
                <button className="rounded-full p-2 text-zinc-500 hover:bg-zinc-100">
                    <Moon className="h-4 w-4" />
                </button>
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