import { useState } from 'react'
import { Pencil, Trash2, Check, X } from 'lucide-react'
import type { ChatMessage } from '../../core/types/ChatMessage'
import type { User } from '../../core/types/User'
import { chatApi } from '../../core/controllers/chatApi'

type Props = {
    messages: ChatMessage[]
    currentUser: User | null
    onMessageChanged: () => void
}

export default function MessageList({ messages, currentUser, onMessageChanged }: Props) {
    const [editingId, setEditingId] = useState<string | null>(null)
    const [editText, setEditText] = useState('')

    const startEdit = (m: ChatMessage) => {
        setEditingId(m.id)
        setEditText(m.content)
    }

    const cancelEdit = () => {
        setEditingId(null)
        setEditText('')
    }

    const submitEdit = async (messageId: string) => {
        if (!editText.trim()) return
        try {
            await chatApi.editMessage(messageId, editText.trim())
            cancelEdit()
            onMessageChanged()
        } catch (err) {
            console.error('Edit failed:', err)
        }
    }

    const handleDelete = async (messageId: string) => {
        try {
            await chatApi.deleteMessage(messageId)
            onMessageChanged()
        } catch (err) {
            console.error('Delete failed:', err)
        }
    }

    return (
        <div className="flex-1 space-y-4 overflow-y-auto px-6 py-6">
            {messages.map((m) => {
                const isOwn = !!currentUser && m.sender?.email === currentUser.email
                const isDeleted = m.isDeleted
                const isEditing = editingId === m.id

                return (
                    <div key={m.id} className="flex justify-end">
                        <div className="max-w-xs group">
                            <div className="mb-1 text-right text-xs text-zinc-400">
                                {m.sender?.username}
                            </div>

                            {isEditing ? (
                                <div className="flex items-center gap-1">
                                    <input
                                        className="rounded-xl border border-zinc-300 bg-white px-3 py-1.5 text-sm text-zinc-900 outline-none focus:ring-2 focus:ring-zinc-400"
                                        value={editText}
                                        onChange={(e) => setEditText(e.target.value)}
                                        onKeyDown={(e) => {
                                            if (e.key === 'Enter') submitEdit(m.id)
                                            if (e.key === 'Escape') cancelEdit()
                                        }}
                                        autoFocus
                                    />
                                    <button onClick={() => submitEdit(m.id)} className="text-green-500 hover:text-green-600">
                                        <Check className="h-4 w-4" />
                                    </button>
                                    <button onClick={cancelEdit} className="text-zinc-400 hover:text-zinc-600">
                                        <X className="h-4 w-4" />
                                    </button>
                                </div>
                            ) : (
                                <div className="flex items-end gap-1">
                                    {isOwn && !isDeleted && (
                                        <div className="mb-1 flex gap-1 opacity-0 transition-opacity group-hover:opacity-100">
                                            <button onClick={() => startEdit(m)} className="text-zinc-400 hover:text-zinc-600">
                                                <Pencil className="h-3.5 w-3.5" />
                                            </button>
                                            <button onClick={() => handleDelete(m.id)} className="text-zinc-400 hover:text-red-500">
                                                <Trash2 className="h-3.5 w-3.5" />
                                            </button>
                                        </div>
                                    )}

                                    <div className={`rounded-2xl px-4 py-2 text-sm ${
                                        isDeleted
                                            ? 'bg-zinc-800 italic text-zinc-500'
                                            : 'bg-zinc-900 text-white'
                                    }`}>
                                        {m.content}
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                )
            })}
        </div>
    )
}