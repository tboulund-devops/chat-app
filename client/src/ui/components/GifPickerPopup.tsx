import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { X, Search } from 'lucide-react'
import { GiphyFetch } from '@giphy/js-fetch-api'

type Props = {
    onSelect: (gifUrl: string) => void
    onClose: () => void
}

type GiphyImage = {
    url: string
}

type GiphyImages = {
    fixed_width_small?: GiphyImage
    fixed_height_small?: GiphyImage
    original?: GiphyImage
}

type GiphyGif = {
    id: string
    title: string
    images: GiphyImages
}

const API_KEY = import.meta.env.VITE_GIPHY_API_KEY as string | undefined

export default function GifPickerPopup({ onSelect, onClose }: Props) {
    const popupRef = useRef<HTMLDivElement | null>(null)
    const [query, setQuery] = useState('happy')
    const [items, setItems] = useState<GiphyGif[]>([])
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState('')

    const gf = useMemo(() => {
        if (!API_KEY) return null
        return new GiphyFetch(API_KEY)
    }, [])

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

    const loadTrending = useCallback(async () => {
        if (!gf) {
            setError('Missing VITE_GIPHY_API_KEY')
            return
        }

        try {
            setLoading(true)
            setError('')
            const { data } = await gf.trending({ limit: 12, rating: 'g' })
            setItems(data as GiphyGif[])
        } catch (err) {
            console.error(err)
            setError('Failed to load trending GIFs')
        } finally {
            setLoading(false)
        }
    }, [gf])

    const handleSearch = useCallback(async (searchText: string) => {
        const trimmed = searchText.trim()

        if (!gf) {
            setError('Missing VITE_GIPHY_API_KEY')
            return
        }

        if (!trimmed) {
            await loadTrending()
            return
        }

        try {
            setLoading(true)
            setError('')
            const { data } = await gf.search(trimmed, {
                limit: 12,
                rating: 'g',
                lang: 'en',
            })
            setItems(data as GiphyGif[])
        } catch (err) {
            console.error(err)
            setError('Failed to search GIFs')
        } finally {
            setLoading(false)
        }
    }, [gf, loadTrending])

    useEffect(() => {
        void loadTrending()
    }, [loadTrending])

    return (
        <div
            ref={popupRef}
            className="absolute bottom-24 right-6 z-20 w-[420px] rounded-2xl border border-zinc-200 bg-white p-4 shadow-xl"
        >
            <div className="mb-3 flex items-center justify-between">
                <div className="text-sm font-semibold text-zinc-800">Choose GIF</div>
                <button
                    type="button"
                    onClick={onClose}
                    className="rounded-md p-1 text-zinc-500 hover:bg-zinc-100 hover:text-zinc-800"
                    aria-label="Close gif picker"
                >
                    <X className="h-4 w-4" />
                </button>
            </div>

            <form
                onSubmit={(e) => {
                    e.preventDefault()
                    void handleSearch(query)
                }}
                className="mb-3 flex items-center gap-2"
            >
                <div className="flex flex-1 items-center gap-2 rounded-xl border border-zinc-200 px-3 py-2">
                    <Search className="h-4 w-4 text-zinc-400" />
                    <input
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        placeholder="Search GIFs"
                        className="w-full bg-transparent text-sm outline-none"
                    />
                </div>

                <button
                    type="submit"
                    className="rounded-xl bg-zinc-900 px-3 py-2 text-sm text-white hover:bg-zinc-800"
                >
                    Search
                </button>
            </form>

            {loading && (
                <div className="py-6 text-center text-sm text-zinc-500">
                    Loading GIFs...
                </div>
            )}

            {error && (
                <div className="py-4 text-sm text-red-500">
                    {error}
                </div>
            )}

            {!loading && !error && (
                <div className="grid max-h-80 grid-cols-2 gap-2 overflow-y-auto">
                    {items.map((gif) => {
                        const previewUrl =
                            gif.images.fixed_width_small?.url ||
                            gif.images.fixed_height_small?.url ||
                            gif.images.original?.url

                        const fullUrl = gif.images.original?.url || previewUrl

                        if (!previewUrl || !fullUrl) return null

                        return (
                            <button
                                key={gif.id}
                                type="button"
                                onClick={() => onSelect(fullUrl)}
                                className="overflow-hidden rounded-xl border border-zinc-200 hover:border-zinc-400"
                                title={gif.title}
                            >
                                <img
                                    src={previewUrl}
                                    alt={gif.title || 'GIF'}
                                    className="h-32 w-full object-cover"
                                />
                            </button>
                        )
                    })}
                </div>
            )}

            <div className="mt-3 text-[11px] text-zinc-400">
                Powered by GIPHY
            </div>
        </div>
    )
}