import { useMemo } from 'react'
import { useParams } from 'react-router-dom'
import { useAtomValue } from 'jotai'
import { chatRoomsAtom } from '../../core/atoms/chatAtoms'
import ChatWindow from '../../ui/components/ChatWindow'

export default function RoomChat() {
    const { roomId = '' } = useParams()
    const myRooms = useAtomValue(chatRoomsAtom)

    const room = useMemo(
        () => myRooms.find((r) => r.id === roomId),
        [myRooms, roomId],
    )

    return <ChatWindow roomId={roomId} room={room} />
}