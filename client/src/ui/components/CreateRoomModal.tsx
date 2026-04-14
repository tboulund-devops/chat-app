import { useState } from 'react'
import { X } from 'lucide-react'
import { chatApi } from '../../core/controllers/chatApi'
import type { ChatRoom } from '../../core/types/ChatRoom'

type Props = {
    onClose: () => void
    onCreated: (room: ChatRoom) => void
}

export default function CreateRoomModal({ onClose, onCreated }: Props) {
    const [name, setName] = useState('')
    const [description, setDescription] = useState('')
    const [error, setError] = useState<string | null>(null)
    const [loading, setLoading] = useState(false)

    const handleSubmit = async () => {
        if (!name.trim()) {
            setError('Room name is required.')
            return
        }

        setLoading(true)
        setError(null)

        try {
            const room = await chatApi.createRoom(name.trim(), description.trim() || undefined)
            onCreated(room)
        } catch (err: any) {
            setError(err.message || 'Failed to create room.')
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
            <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
                <div className="mb-4 flex items-center justify-between">
                    <h2 className="text-lg font-bold text-zinc-900">Create a Room</h2>
                    <button onClick={onClose} className="text-zinc-400 hover:text-zinc-600">
                        <X className="h-5 w-5" />
                    </button>
                </div>

                <div className="space-y-4">
                    <div>
                        <label className="mb-1 block text-sm font-medium text-zinc-700">
                            Room Name <span className="text-red-500">*</span>
                        </label>
                        <input
                            type="text"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            placeholder="e.g. general, announcements"
                            className="w-full rounded-xl border border-zinc-200 px-4 py-2.5 text-sm outline-none focus:border-zinc-900"
                        />
                    </div>

                    <div>
                        <label className="mb-1 block text-sm font-medium text-zinc-700">
                            Description <span className="text-zinc-400 font-normal">(optional)</span>
                        </label>
                        <textarea
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            placeholder="What's this room about?"
                            rows={3}
                            className="w-full rounded-xl border border-zinc-200 px-4 py-2.5 text-sm outline-none focus:border-zinc-900 resize-none"
                        />
                    </div>

                    {error && <p className="text-sm text-red-500">{error}</p>}

                    <div className="flex justify-end gap-3 pt-2">
                        <button
                            onClick={onClose}
                            className="rounded-xl border border-zinc-200 px-4 py-2 text-sm font-medium text-zinc-600 hover:bg-zinc-50"
                        >
                            Cancel
                        </button>
                        <button
                            onClick={handleSubmit}
                            disabled={loading}
                            className="rounded-xl bg-zinc-900 px-4 py-2 text-sm font-semibold text-white hover:bg-zinc-800 disabled:opacity-50"
                        >
                            {loading ? 'Creating...' : 'Create Room'}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    )
}