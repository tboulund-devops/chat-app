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
import GifPickerPopup from './GifPickerPopup'

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
    const eventSourceRef = useRef<EventSource | null>(null)

    const title = useMemo(() => room?.name ?? 'Room', [room])

    // 1. Fetch current user once
    useEffect(() => {
        authApi.me().then(setCurrentUser).catch(console.error)
    }, [])

    // 2. Load message history when room changes
    useEffect(() => {
        if (!roomId) return
        chatApi.getRoomMessages(roomId)
            .then(setMessages)
            .catch(console.error)
    }, [roomId])

    // 3. Single persistent SSE connection for the lifetime of the component
    useEffect(() => {
        const es = new EventSource('/api/chat/stream', { withCredentials: true })
        eventSourceRef.current = es

        es.onerror = (error) => console.error('SSE connection error:', error)

        return () => {
            es.close()
            eventSourceRef.current = null
        }
    }, [])

    // 4. Re-register room listener when roomId changes, without touching the connection
    useEffect(() => {
        if (!roomId) return

        const es = eventSourceRef.current
        if (!es) return

        const handleRoomMessage = (event: MessageEvent) => {
            try {
                const message = JSON.parse(event.data) as ChatMessage
                if (message.roomId !== roomId) return  // ignore other rooms
                setMessages(prev => [...prev, message])
            } catch {
                console.error('Failed to parse SSE message:', event.data)
            }
        }

        es.addEventListener('message', handleRoomMessage)

        return () => {
            es.removeEventListener(roomId, handleRoomMessage)
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
            await chatApi.sendMessage(roomId, `[Image] ${file.name}`)
        } catch (error) {
            console.error('Image upload failed:', error)
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
                    onMessageChanged={() =>
                        chatApi.getRoomMessages(roomId).then(setMessages).catch(console.error)
                    }
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
                    <GifPickerPopup
                        onSelect={async (gifUrl) => {
                            try {
                                await chatApi.sendMessage(roomId, `[GIF] ${gifUrl}`)
                                setShowGifPicker(false)
                            } catch (error) {
                                console.error('GIF send failed:', error)
                            }
                        }}
                        onClose={() => setShowGifPicker(false)}
                    />
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