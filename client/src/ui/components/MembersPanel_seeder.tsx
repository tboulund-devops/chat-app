// src/ui/components/MembersPanel.tsx
import { useEffect, useState } from 'react'
import { useAtomValue } from 'jotai'
import { authAtom } from '../../core/atoms/authAtom'
import { chatApi } from '../../core/controllers/chatApi'
import type { RoomMember } from '../../core/types/RoomMember'
import PokeButton from './PokeButton'

type Props = {
    roomId: string
}

export default function MembersPanel({ roomId }: Props) {
    const auth = useAtomValue(authAtom)
    const currentUserId = auth.status === 'authenticated' ? auth.user.id : null

    const [members, setMembers] = useState<RoomMember[]>([])

    useEffect(() => {
        if (!roomId) return
        chatApi.getRoomMembers(roomId)
            .then(setMembers)
            .catch(console.error)
    }, [roomId])

    const avatarColours = [
        'bg-orange-200', 'bg-teal-200', 'bg-red-200',
        'bg-blue-200', 'bg-purple-200', 'bg-yellow-200',
    ]

    return (
        <aside className="hidden bg-white lg:block">
            <div className="border-b border-zinc-200 px-4 py-4">
                <h2 className="text-xs font-semibold uppercase tracking-[0.2em] text-zinc-400">
                    Members · {members.length}
                </h2>
            </div>

            <div className="space-y-3 px-4 py-4 text-sm">
                {members.map((member, index) => {
                    const isMe = member.userId === currentUserId
                    const initials = `${member.firstName[0]}${member.lastName[0]}`
                    const fullName = `${member.firstName} ${member.lastName}`

                    return (
                        <div key={member.userId} className="flex items-center gap-3">
                            {/* Avatar */}
                            <div className={`flex h-8 w-8 shrink-0 items-center justify-center
                                            rounded-full text-xs font-semibold text-zinc-700
                                            ${avatarColours[index % avatarColours.length]}`}>
                                {initials}
                            </div>

                            {/* Name */}
                            <span className="min-w-0 flex-1 truncate">
                                {fullName}
                                {isMe && (
                                    <span className="ml-1 text-xs text-zinc-400">(you)</span>
                                )}
                            </span>

                            {/* Actions — hidden for yourself, extend here for DMs later */}
                            {!isMe && (
                                <div className="flex items-center gap-1">
                                    <PokeButton
                                        targetUserId={member.userId}
                                        targetName={member.firstName}
                                    />
                                    {/* <DmButton userId={member.userId} /> ← add here later */}
                                </div>
                            )}
                        </div>
                    )
                })}
            </div>
        </aside>
    )
}