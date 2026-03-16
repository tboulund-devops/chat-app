import { Users } from 'lucide-react'

export default function Dms() {
    return (
        <div className="mx-auto max-w-5xl px-6 py-8">
            <h1 className="text-2xl font-bold text-zinc-900">Direct Messages</h1>
            <p className="mt-1 text-sm text-zinc-500">
                Private conversations and requests.
            </p>

            <div className="mt-8 flex min-h-[280px] flex-col items-center justify-center rounded-3xl border border-dashed border-zinc-200 bg-zinc-50 text-center">
                <Users className="mb-4 h-10 w-10 text-zinc-300" />
                <p className="text-sm text-zinc-500">No active conversations yet.</p>
                <button className="mt-4 rounded-xl px-4 py-2 text-sm font-medium text-zinc-900 hover:bg-zinc-100">
                    Find people in rooms
                </button>
            </div>
        </div>
    )
}