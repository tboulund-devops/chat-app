type ApiOptions = {
  init?: RequestInit
  skipRetry?: boolean
}

type RefreshResponse = {
  success?: boolean
}

const refreshState: {
  promise: Promise<boolean> | null
} = {
  promise: null,
}

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

async function refreshToken(): Promise<boolean> {
  try {
    const response = await fetch('/api/auth/refresh-token', {
      method: 'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
      },
    })

    if (!response.ok) {
      return false
    }

    if (response.status === 204) {
      return true
    }

    const contentType = response.headers.get('content-type') ?? ''
    if (contentType.includes('application/json')) {
      const data = (await response.json()) as RefreshResponse
      return data.success ?? true
    }

    return true
  } catch (error) {
    console.error('Refresh token request failed:', error)
    return false
  }
}

async function getRefreshPromise(): Promise<boolean> {
  if (!refreshState.promise) {
    refreshState.promise = refreshToken().finally(() => {
      refreshState.promise = null
    })
  }

  return refreshState.promise
}

function normalizeHeaders(headers?: HeadersInit): Record<string, string> {
  if (!headers) return {}

  if (headers instanceof Headers) {
    return Object.fromEntries(headers.entries())
  }

  if (Array.isArray(headers)) {
    return Object.fromEntries(headers)
  }

  return { ...headers }
}

export async function api<T>(
    url: string,
    options?: ApiOptions,
): Promise<T> {
  const { init = {}, skipRetry = false } = options ?? {}

  const headers = normalizeHeaders(init.headers)

  if ('authorization' in Object.keys(headers).reduce<Record<string, true>>((acc, key) => {
    acc[key.toLowerCase()] = true
    return acc
  }, {})) {
    throw new Error('Authorization header is not allowed. Use httpOnly cookies.')
  }

  let body: BodyInit | undefined

  if (init.body instanceof FormData) {
    body = init.body
  } else if (typeof init.body === 'string') {
    body = init.body
    if (!headers['Content-Type']) {
      headers['Content-Type'] = 'application/json'
    }
  } else if (init.body != null) {
    body = JSON.stringify(init.body)
    if (!headers['Content-Type']) {
      headers['Content-Type'] = 'application/json'
    }
  }

  const response = await fetch(url, {
    ...init,
    method: init.method ?? 'GET',
    credentials: 'include',
    headers,
    body,
  })

  if (!response.ok) {
    if (response.status === 401 && !skipRetry) {
      const refreshed = await getRefreshPromise()

      if (refreshed) {
        return api<T>(url, {
          ...options,
          skipRetry: true,
        })
      }

      throw new ApiError(401, 'Session expired. Please log in again.')
    }

    let message = `HTTP ${response.status}`

    try {
      const contentType = response.headers.get('content-type') ?? ''

      if (contentType.includes('application/json')) {
        const data = await response.json()
        message =
            data?.message ||
            data?.error ||
            data?.errors?.[0]?.msg ||
            JSON.stringify(data)
      } else {
        const text = await response.text()
        if (text) {
          message = text
        }
      }
    } catch {
      // ignore parsing errors
    }

    throw new ApiError(response.status, message)
  }

  if (response.status === 204) {
    return undefined as T
  }

  const contentType = response.headers.get('content-type') ?? ''

  if (contentType.includes('application/json')) {
    return (await response.json()) as T
  }

  return (await response.text()) as T
}