import { useEffect, useMemo, useState } from 'react'
import { DoorClosed } from 'lucide-react'
import type { ChatRoom } from '../../core/types/ChatRoom'
import type { ChatMessage } from '../../core/types/ChatMessage'
import { chatApi } from '../../core/controllers/chatApi'
import MessageList from './MessageList'
import MessageComposer from './MessageComposer'
import MembersPanel_seeder from './MembersPanel_seeder'

type Props = {
    roomId: string
    room?: ChatRoom
}

export default function ChatWindow({ roomId, room }: Props) {
    const [messages, setMessages] = useState<ChatMessage[]>([])
    const [text, setText] = useState('')

    const title = useMemo(() => room?.name ?? 'Room', [room])

    const loadMessages = async () => {
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

        const fetchMessages = async () => {
            try {
                const data = await chatApi.getRoomMessages(roomId)
                if (!cancelled) {
                    setMessages(data)
                }
            } catch (error) {
                console.error('Failed to load messages:', error)
            }
        }

        fetchMessages()

        return () => {
            cancelled = true
        }
    }, [roomId])

    const handleSend = async () => {
        if (!text.trim()) return

        try {
            await chatApi.sendMessage(roomId, text)
            setText('')
            await loadMessages()
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

                <MessageList messages={messages} />

                <MessageComposer
                    value={text}
                    onChange={setText}
                    onSend={handleSend}
                    placeholder={`Message ${title}`}
                />
            </section>

            <MembersPanel_seeder />
        </div>
    )
}