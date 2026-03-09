import { DoorClosedLocked } from 'lucide-react'
import type { ChatRoom } from '../../core/types/ChatRoom'

type Props = {
    room: ChatRoom
    joined: boolean
    onJoin: (roomId: string) => void
    onOpen: (roomId: string) => void
}

export default function RoomCard({ room, joined, onJoin, onOpen }: Props) {
    return (
        <div className="rounded-2xl border border-zinc-200 bg-white p-4">
            <div className="mb-4 flex items-start justify-between">
                <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-zinc-100">
                    <DoorClosedLocked className="h-5 w-5 text-zinc-600" />
                </div>
                <div className="flex items-center gap-1 text-xs text-zinc-400">
                </div>
            </div>

            <h3 className="text-sm font-semibold text-zinc-900">{room.name}</h3>
            <p className="mt-1 text-sm text-zinc-500">
                {room.description || 'Real-time coordination for ongoing issues.'}
            </p>

            <div className="mt-4">
                {joined ? (
                    <button
                        onClick={() => onOpen(room.id)}
                        className="w-full rounded-xl bg-zinc-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-zinc-800"
                    >
                        Enter
                    </button>
                ) : (
                    <button
                        onClick={() => onJoin(room.id)}
                        className="w-full rounded-xl bg-zinc-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-zinc-800"
                    >
                        Join Room
                    </button>
                )}
            </div>
        </div>
    )
}