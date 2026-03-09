import { useEffect, useState } from 'react'
import { useAtom } from 'jotai'
import { authAtom } from '../atoms/authAtom'
import { authApi } from '../../core/controllers/authApi'

export function useAuthApi() {
    const [auth, setAuth] = useAtom(authAtom)
    const [loading, setLoading] = useState(true)

    useEffect(() => {
        let cancelled = false

        const checkAuth = async () => {
            try {
                const user = await authApi.me()

                if (!cancelled) {
                    setAuth({ status: 'authenticated', user })
                }
            } catch {
                if (!cancelled) {
                    setAuth({ status: 'unauthenticated' })
                }
            } finally {
                if (!cancelled) {
                    setLoading(false)
                }
            }
        }

        if (auth.status === 'unknown') {
            checkAuth()
        } else {
            setLoading(false)
        }

        return () => {
            cancelled = true
        }
    }, [auth.status, setAuth])

    return { auth, loading }
}