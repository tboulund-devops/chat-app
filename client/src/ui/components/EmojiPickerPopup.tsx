import { useEffect, useRef } from 'react'
import { X } from 'lucide-react'
import EmojiPicker, { type EmojiClickData, Theme } from 'emoji-picker-react'

type Props = {
    onSelect: (emoji: string) => void
    onClose: () => void
}

export default function EmojiPickerPopup({ onSelect, onClose }: Props) {
    const popupRef = useRef<HTMLDivElement | null>(null)

    useEffect(() => {
        function handleClickOutside(event: MouseEvent) {
            const target = event.target as Node
            if (popupRef.current && !popupRef.current.contains(target)) {
                onClose()
            }
        }

        function handleEscape(event: KeyboardEvent) {
            if (event.key === 'Escape') {
                onClose()
            }
        }

        document.addEventListener('mousedown', handleClickOutside)
        document.addEventListener('keydown', handleEscape)

        return () => {
            document.removeEventListener('mousedown', handleClickOutside)
            document.removeEventListener('keydown', handleEscape)
        }
    }, [onClose])

    return (
        <div
            ref={popupRef}
            className="absolute bottom-24 right-6 z-20 rounded-2xl border border-zinc-200 bg-white p-2 shadow-xl"
        >
            <div className="mb-2 flex items-center justify-between px-1">
                <span className="text-sm font-medium text-zinc-700">Emoji</span>
                <button
                    type="button"
                    onClick={onClose}
                    className="rounded-md p-1 text-zinc-500 hover:bg-zinc-100 hover:text-zinc-800"
                    aria-label="Close emoji picker"
                    title="Close"
                >
                    <X className="h-4 w-4" />
                </button>
            </div>

            <EmojiPicker
                theme={Theme.LIGHT}
                onEmojiClick={(emojiData: EmojiClickData) => {
                    onSelect(emojiData.emoji)
                }}
                lazyLoadEmojis
                searchDisabled={false}
                skinTonesDisabled
            />
        </div>
    )
}