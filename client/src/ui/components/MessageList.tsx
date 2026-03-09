import type { ChatMessage } from '../../core/types/ChatMessage'

type Props = {
    messages: ChatMessage[]
}

export default function MessageList({ messages }: Props) {
    return (
        <div className="flex-1 space-y-4 overflow-y-auto px-6 py-6">
            {messages.map((m) => (
                <div key={m.id} className="flex justify-end">
                    <div className="max-w-xs">
                        <div className="mb-1 text-right text-xs text-zinc-400">
                            {m.sender?.username}
                        </div>
                        <div className="rounded-2xl bg-zinc-900 px-4 py-2 text-sm text-white">
                            {m.content}
                        </div>
                    </div>
                </div>
            ))}
        </div>
    )
}
