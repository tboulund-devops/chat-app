import { useState } from 'react';
import { notificationApi } from '../../core/controllers/notificationApi';

type Props = { targetUserId: string; targetName?: string };

export default function PokeButton({ targetUserId, targetName }: Props) {
    const [poking, setPoking] = useState(false);
    const [done, setDone] = useState(false);

    const handlePoke = async () => {
        setPoking(true);
        try {
            await notificationApi.poke(targetUserId);
            setDone(true);
            setTimeout(() => setDone(false), 2000);
        } catch (e) {
            console.error('Poke failed:', e);
        } finally {
            setPoking(false);
        }
    };

    return (
        <button
            onClick={handlePoke}
            disabled={poking}
            className="rounded-lg px-3 py-1 text-xs font-medium bg-zinc-100
                 hover:bg-zinc-200 text-zinc-700 disabled:opacity-50"
        >
            {done ? '👉 Poked!' : poking ? '...' : '👉 Poke'}
        </button>
    );
}