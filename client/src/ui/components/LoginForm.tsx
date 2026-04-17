import { useState } from 'react'
import { useAtom } from 'jotai'
import { authAtom } from '../../core/atoms/authAtom'
import { authApi } from '../../core/controllers/authApi'
import { useNavigate } from 'react-router-dom'

export default function LoginForm() {
    const [, setAuth] = useAtom(authAtom)
    const navigate = useNavigate()

    const [email, setEmail] = useState('')
    const [password, setPassword] = useState('')
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault()
        setLoading(true)
        setError(null)

        try {
            const user = await authApi.login(email, password)
            setAuth({ status: 'authenticated', user })
            navigate('/rooms', { replace: true })
        } catch (err: any) {
            setAuth({ status: 'unauthenticated' })
            setError(err.message || 'Login failed')
        } finally {
            setLoading(false)
        }
    }

    return (
        <form onSubmit={handleSubmit} className="space-y-5">
            <div>
                <label className="mb-2 block text-sm font-medium text-zinc-700" htmlFor="email">
                    Email Address
                </label>
                <input
                    type="email"
                    id="email"
                    placeholder="name@example.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    className="w-full rounded-xl border border-zinc-300 px-4 py-3 outline-none focus:border-zinc-900"
                />
            </div>

            <div>
                <label className="mb-2 block text-sm font-medium text-zinc-700" htmlFor="password">
                    Password
                </label>
                <input
                    type="password"
                    id="password"
                    placeholder="••••••••"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                    className="w-full rounded-xl border border-zinc-300 px-4 py-3 outline-none focus:border-zinc-900"
                />
            </div>

            {error && (
                <div className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-600">
                    {error}
                </div>
            )}

            <button
                type="submit"
                disabled={loading}
                className="w-full rounded-xl bg-zinc-900 px-4 py-3 text-sm font-semibold text-white hover:bg-zinc-800 disabled:opacity-60"
            >
                {loading ? 'Signing In...' : 'Sign In'}
            </button>
        </form>
    )
}