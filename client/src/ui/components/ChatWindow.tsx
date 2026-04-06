import { useEffect, useMemo, useRef, useState } from 'react'
import { DoorClosed } from 'lucide-react'
import type { ChatRoom } from '../../core/types/ChatRoom'
import type { ChatMessage } from '../../core/types/ChatMessage'
import type { User } from '../../core/types/User'
import { chatApi } from '../../core/controllers/chatApi'
import { authApi } from '../../core/controllers/authApi'
import MessageList from './MessageList'
import MessageComposer from './MessageComposer'
import MembersPanel from './MembersPanel_seeder'
import EmojiPickerPopup from './EmojiPickerPopup'

type Props = {
    roomId: string
    room?: ChatRoom
}

export default function ChatWindow({ roomId, room }: Props) {
    const [messages, setMessages] = useState<ChatMessage[]>([])
    const [text, setText] = useState('')
    const [currentUser, setCurrentUser] = useState<User | null>(null)
    const [showEmojiPicker, setShowEmojiPicker] = useState(false)
    const [showGifPicker, setShowGifPicker] = useState(false)
    const [gifUrl, setGifUrl] = useState('')
    const eventSourceRef = useRef<EventSource | null>(null)

    const title = useMemo(() => room?.name ?? 'Room', [room])

    useEffect(() => {
        authApi.me().then(setCurrentUser).catch(console.error)
    }, [])

    const loadMessages = async () => {
        if (!roomId) return

        try {
            const data = await chatApi.getRoomMessages(roomId)
            setMessages(data)
        } catch (error) {
            console.error('Failed to load messages:', error)
        }
    }

    useEffect(() => {
        if (!roomId) return

        let cancelled = false

        chatApi.getRoomMessages(roomId)
            .then((data) => {
                if (!cancelled) setMessages(data)
            })
            .catch(console.error)

        return () => {
            cancelled = true
        }
    }, [roomId])

    useEffect(() => {
        if (!roomId) return

        if (eventSourceRef.current) {
            eventSourceRef.current.close()
            eventSourceRef.current = null
        }

        const es = new EventSource('/api/chat/stream', { withCredentials: true })

        const handleRoomMessage = () => {
            loadMessages().catch(console.error)
        }

        es.addEventListener(roomId, handleRoomMessage)
        eventSourceRef.current = es

        es.onerror = (error) => {
            console.error('SSE connection error:', error)
        }

        return () => {
            es.removeEventListener(roomId, handleRoomMessage)
            es.close()
            eventSourceRef.current = null
        }
    }, [roomId])

    const handleSend = async () => {
        const trimmed = text.trim()
        if (!trimmed) return

        try {
            await chatApi.sendMessage(roomId, trimmed)
            setText('')
            setShowEmojiPicker(false)
        } catch (error) {
            console.error('Send failed:', error)
        }
    }

    const handleImageUpload = async (file: File) => {
        try {
            // Waiting for backend API, fx
            // await chatApi.sendImageMessage(roomId, file)

            // Temporary fallback:
            // send image name as text until backend file upload is ready
            await chatApi.sendMessage(roomId, `[Image] ${file.name}`)
        } catch (error) {
            console.error('Image upload failed:', error)
        }
    }

    const handleGifSend = async () => {
        const trimmed = gifUrl.trim()
        if (!trimmed) return

        try {
            // Option A:
            // If backend supports GIF messages:
            // await chatApi.sendGifMessage(roomId, trimmed)

            // Temporary fallback:
            await chatApi.sendMessage(roomId, `[GIF] ${trimmed}`)

            setGifUrl('')
            setShowGifPicker(false)
        } catch (error) {
            console.error('GIF send failed:', error)
        }
    }

    return (
        <div className="grid h-[calc(100vh-56px)] grid-cols-1 lg:grid-cols-[1fr_260px]">
            <section className="relative flex min-h-0 flex-col border-r border-zinc-200">
                <div className="flex h-14 items-center gap-3 border-b border-zinc-200 px-5">
                    <DoorClosed className="h-4 w-4 text-zinc-400" />
                    <h1 className="text-sm font-semibold text-zinc-900">{title}</h1>
                    <span className="rounded-full bg-zinc-100 px-2 py-0.5 text-xs text-zinc-500">
                        {messages.length} messages
                    </span>
                </div>

                <MessageList
                    messages={messages}
                    currentUser={currentUser}
                    onMessageChanged={loadMessages}
                />

                {showEmojiPicker && (
                    <EmojiPickerPopup
                        onSelect={(emoji) => {
                            setText((prev) => prev + emoji)
                            setShowEmojiPicker(false)
                        }}
                        onClose={() => setShowEmojiPicker(false)}
                    />
                )}

                {showGifPicker && (
                    <div className="absolute bottom-24 right-6 z-20 w-80 rounded-2xl border border-zinc-200 bg-white p-4 shadow-xl">
                        <div className="mb-3 text-sm font-semibold text-zinc-800">
                            Paste GIF URL
                        </div>
                        <input
                            value={gifUrl}
                            onChange={(e) => setGifUrl(e.target.value)}
                            placeholder="https://..."
                            className="w-full rounded-xl border border-zinc-200 px-3 py-2 text-sm outline-none focus:border-zinc-400"
                        />
                        <div className="mt-3 flex justify-end gap-2">
                            <button
                                type="button"
                                onClick={() => {
                                    setGifUrl('')
                                    setShowGifPicker(false)
                                }}
                                className="rounded-xl border border-zinc-200 px-3 py-2 text-sm text-zinc-600 hover:bg-zinc-50"
                            >
                                Cancel
                            </button>
                            <button
                                type="button"
                                onClick={handleGifSend}
                                className="rounded-xl bg-zinc-900 px-3 py-2 text-sm text-white hover:bg-zinc-800"
                            >
                                Send GIF
                            </button>
                        </div>
                    </div>
                )}

                <MessageComposer
                    value={text}
                    onChange={setText}
                    onSend={handleSend}
                    onOpenEmoji={() => {
                        setShowGifPicker(false)
                        setShowEmojiPicker((prev) => !prev)
                    }}
                    onPickGif={() => {
                        setShowEmojiPicker(false)
                        setShowGifPicker((prev) => !prev)
                    }}
                    onPickImage={handleImageUpload}
                    placeholder={`Message ${title}`}
                />
            </section>

            <MembersPanel roomId={roomId} />
        </div>
    )
}