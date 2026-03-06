import { Navigate } from 'react-router-dom'
import { Bot } from 'lucide-react'
import { useAtomValue } from 'jotai'
import { authAtom } from '../../core/atoms/authAtom'
import LoginForm from '../../ui/components/LoginForm'
import { useAuthApi } from '../../core/hooks/useAuthApi'

export default function Login() {
  const { loading } = useAuthApi()
  const auth = useAtomValue(authAtom)

  if (!loading && auth.status === 'authenticated') {
    return <Navigate to="/rooms" replace />
  }

  return (
      <div className="flex min-h-screen items-center justify-center bg-zinc-100 px-4">
        <div className="w-full max-w-md rounded-3xl border border-zinc-200 bg-white p-8 shadow-sm">
          <div className="mb-8 flex flex-col items-center text-center">
            <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-zinc-900 text-white">
              <Bot className="h-5 w-5" />
            </div>
            <h1 className="text-xl font-bold text-zinc-900">Incident Tracker</h1>
            <p className="mt-2 text-sm text-zinc-500">
              Welcome back! Sign in to continue.
            </p>
          </div>

          <LoginForm />

          <p className="mt-6 text-center text-sm text-zinc-500">
            Don&apos;t have an account? <span className="font-medium">Sign up</span>
          </p>
        </div>
      </div>
  )
}