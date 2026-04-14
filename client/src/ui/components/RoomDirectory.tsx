import { useEffect, useMemo, useState } from 'react'
import { Search } from 'lucide-react'
import { useAtom } from 'jotai'
import { useNavigate } from 'react-router-dom'
import { chatApi } from '../../core/controllers/chatApi'
import { allchatRoomsAtom, chatRoomsAtom } from '../../core/atoms/chatAtoms'
import RoomCard from './RoomCard'
import CreateRoomModal from './CreateRoomModal'
import {ChatRoom} from "../../core/types/ChatRoom";

export default function RoomDirectory() {
    const navigate = useNavigate()
    const [myRooms, setMyRooms] = useAtom(chatRoomsAtom)
    const [allRooms, setAllRooms] = useAtom(allchatRoomsAtom)
    const [query, setQuery] = useState('')
    const [showModal, setShowModal] = useState(false)

    useEffect(() => {
        const loadRooms = async () => {
            try {
                const [mine, all] = await Promise.all([
                    chatApi.getMyRooms(),
                    chatApi.getAllRooms(),
                ])
                setMyRooms(mine)
                setAllRooms(all)
            } catch (error) {
                console.error('Failed to load rooms:', error)
            }
        }
        loadRooms()
    }, [setMyRooms, setAllRooms])

    const filteredRooms = useMemo(() => {
        const q = query.trim().toLowerCase()
        if (!q) return allRooms
        return allRooms.filter((room) => room.name.toLowerCase().includes(q))
    }, [allRooms, query])

    const isJoined = (roomId: string) => myRooms.some((r) => r.id === roomId)

    const handleJoin = async (roomId: string) => {
        try {
            await chatApi.joinRoom(roomId)
            const updated = await chatApi.getMyRooms()
            setMyRooms(updated)
        } catch (error) {
            console.error('Join failed:', error)
        }
    }

    const handleOpen = (roomId: string) => {
        navigate(`/rooms/${roomId}`)
    }

    const handleRoomCreated = (room: ChatRoom) => {
        setAllRooms((prev) => [room, ...prev])  // appears in directory immediately
        setMyRooms((prev) => [room, ...prev])   // creator is already a member
        setShowModal(false)
        navigate(`/rooms/${room.id}`)           // take them straight into the room
    }

    return (
        <div className="mx-auto max-w-6xl px-6 py-8">
            {showModal && (
                <CreateRoomModal
                    onClose={() => setShowModal(false)}
                    onCreated={handleRoomCreated}
                />
            )}

            <div className="mb-6 flex items-start justify-between gap-4">
                <div>
                    <h1 className="text-2xl font-bold text-zinc-900">Room Directory</h1>
                    <p className="mt-1 text-sm text-zinc-500">Discover and join communities.</p>
                </div>

                <button
                    onClick={() => setShowModal(true)}
                    className="rounded-xl bg-zinc-900 px-4 py-2.5 text-sm font-semibold text-white hover:bg-zinc-800"
                >
                    + Create Room
                </button>
            </div>

            <div className="relative mb-6">
                <Search className="absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-400" />
                <input
                    type="text"
                    placeholder="Search for rooms..."
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    className="w-full rounded-2xl border border-zinc-200 bg-white py-3 pl-11 pr-4 outline-none focus:border-zinc-900"
                />
            </div>

            <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
                {filteredRooms.map((room) => (
                    <RoomCard
                        key={room.id}
                        room={room}
                        joined={isJoined(room.id)}
                        onJoin={handleJoin}
                        onOpen={handleOpen}
                    />
                ))}
            </div>
        </div>
    )
}