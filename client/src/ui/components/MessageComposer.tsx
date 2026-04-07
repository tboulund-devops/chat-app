import { useRef } from 'react'
import { Image as ImageIcon, SendHorizontal, Smile } from 'lucide-react'

type Props = {
    value: string
    onChange: (value: string) => void
    onSend: () => void
    onOpenEmoji: () => void
    onPickGif: () => void
    onPickImage: (file: File) => void
    placeholder?: string
}

export default function MessageComposer({
                                            value,
                                            onChange,
                                            onSend,
                                            onOpenEmoji,
                                            onPickGif,
                                            onPickImage,
                                            placeholder,
                                        }: Props) {
    const fileInputRef = useRef<HTMLInputElement | null>(null)

    function handleImageClick() {
        fileInputRef.current?.click()
    }

    function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
        const file = e.target.files?.[0]
        if (!file) return

        onPickImage(file)
        e.target.value = ''
    }

    function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
        if (e.key === 'Enter') {
            e.preventDefault()
            onSend()
        }
    }

    return (
        <div className="border-t border-zinc-200 p-4">
            <div className="flex items-center gap-3 rounded-2xl bg-zinc-100 px-4 py-3">
                <input
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder={placeholder}
                    className="flex-1 bg-transparent text-sm outline-none"
                />

                <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/*"
                    className="hidden"
                    onChange={handleFileChange}
                />

                <button
                    type="button"
                    onClick={handleImageClick}
                    className="text-zinc-500 hover:text-zinc-800"
                    title="Upload image"
                >
                    <ImageIcon className="h-5 w-5" />
                </button>

                <button
                    type="button"
                    onClick={onPickGif}
                    className="rounded-md border border-zinc-300 px-2 py-1 text-xs font-medium text-zinc-600 hover:border-zinc-400 hover:text-zinc-900"
                    title="Choose GIF"
                >
                    GIF
                </button>

                <button
                    type="button"
                    onClick={onOpenEmoji}
                    className="text-zinc-500 hover:text-zinc-800"
                    title="Choose emoji"
                >
                    <Smile className="h-5 w-5" />
                </button>

                <button
                    type="button"
                    onClick={onSend}
                    className="rounded-xl bg-zinc-900 p-2 text-white hover:bg-zinc-800"
                    title="Send"
                >
                    <SendHorizontal className="h-4 w-4" />
                </button>
            </div>
        </div>
    )
}