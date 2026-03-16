import { SendHorizonal, Smile } from 'lucide-react'

type Props = {
    value: string
    onChange: (value: string) => void
    onSend: () => void
    placeholder?: string
}

export default function MessageComposer({
                                            value,
                                            onChange,
                                            onSend,
                                            placeholder,
                                        }: Props) {
    return (
        <div className="border-t border-zinc-200 p-4">
            <div className="flex items-center gap-3 rounded-2xl bg-zinc-100 px-4 py-3">
                <input
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && onSend()}
                    placeholder={placeholder}
                    className="flex-1 bg-transparent text-sm outline-none"
                />
                <button className="text-zinc-500 hover:text-zinc-800">
                    <Smile className="h-4 w-4" />
                </button>
                <button
                    onClick={onSend}
                    className="rounded-xl bg-zinc-900 p-2 text-white hover:bg-zinc-800"
                >
                    <SendHorizonal className="h-4 w-4" />
                </button>
            </div>
        </div>
    )
}