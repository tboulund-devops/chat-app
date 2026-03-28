// ChatWindow.tsx
import { useEffect, useMemo, useRef, useState } from 'react'
import { DoorClosed } from 'lucide-react'
import type { ChatRoom } from '../../core/types/ChatRoom'
import type { ChatMessage } from '../../core/types/ChatMessage'
import type { User } from '../../core/types/User'
import { chatApi } from '../../core/controllers/chatApi'
import { authApi } from '../../core/controllers/authApi'
import MessageList from './MessageList'
import MessageComposer from './MessageComposer'
import MembersPanel from "./MembersPanel_seeder";

type Props = {
    roomId: string
    room?: ChatRoom
}

export default function ChatWindow({ roomId, room }: Props) {
    const [messages, setMessages] = useState<ChatMessage[]>([])
    const [text, setText] = useState('')
    const [currentUser, setCurrentUser] = useState<User | null>(null)
    const eventSourceRef = useRef<EventSource | null>(null)

    const title = useMemo(() => room?.name ?? 'Room', [room])

    useEffect(() => {
        authApi.me().then(setCurrentUser).catch(console.error)
    }, [])

    // Initial message load
    useEffect(() => {
        if (!roomId) return
        let cancelled = false
        chatApi.getRoomMessages(roomId)
            .then(data => { if (!cancelled) setMessages(data) })
            .catch(console.error)
        return () => { cancelled = true }
    }, [roomId])

    // Listen to SSE for real-time messages in this room
    useEffect(() => {
        if (!roomId) return

        // Reuse the existing SSE connection via a second EventSource
        // pointed at the same stream — browser deduplicates these
        const es = new EventSource('/api/chat/stream', { withCredentials: true })

        // The backend sends room messages with the roomId as the event name
        es.addEventListener(roomId, (event) => {
            const data = JSON.parse((event as MessageEvent).data)
            // data is the SendMessageRequest shape: { roomId, content }
            // reload messages to get full message with sender info
            chatApi.getRoomMessages(roomId)
                .then(setMessages)
                .catch(console.error)
        })

        eventSourceRef.current = es

        return () => {
            es.close()
            eventSourceRef.current = null
        }
    }, [roomId])

    const loadMessages = async () => {
        try {
            const data = await chatApi.getRoomMessages(roomId)
            setMessages(data)
        } catch (error) {
            console.error('Failed to load messages:', error)
        }
    }

    const handleSend = async () => {
        if (!text.trim()) return
        try {
            await chatApi.sendMessage(roomId, text)
            setText('')
            // Don't reload here — SSE will trigger it
        } catch (error) {
            console.error('Send failed:', error)
        }
    }

    return (
        <div className="grid h-[calc(100vh-56px)] grid-cols-1 lg:grid-cols-[1fr_260px]">
            <section className="flex min-h-0 flex-col border-r border-zinc-200">
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

                <MessageComposer
                    value={text}
                    onChange={setText}
                    onSend={handleSend}
                    placeholder={`Message ${title}`}
                />
            </section>

            <MembersPanel roomId={roomId} />
        </div>
    )
}