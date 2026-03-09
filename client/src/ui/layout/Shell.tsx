import { Navigate, Outlet } from 'react-router-dom'
import Sidebar from './Sidebar'
import Topbar from './Topbar'
import SseConnection from '../components/SseConnection'
import { useAuthApi } from '../../core/hooks/useAuthApi'

export default function Shell() {
    const { auth, loading } = useAuthApi()

    if (loading) {
        return (
            <div className="flex min-h-screen items-center justify-center bg-zinc-100">
                <div className="text-sm text-zinc-500">Loading...</div>
            </div>
        )
    }

    if (auth.status !== 'authenticated') {
        return <Navigate to="/login" replace />
    }

    return (
        <div className="flex min-h-screen bg-white">
            <SseConnection />
            <Sidebar />
            <div className="flex min-w-0 flex-1 flex-col">
                <Topbar />
                <main className="flex-1 overflow-hidden">
                    <Outlet />
                </main>
            </div>
        </div>
    )
}