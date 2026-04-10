export async function getRegisterFeatureFlag(): Promise<boolean> {
    const response = await fetch('http://localhost:5285/api/features/register-user', {
        credentials: 'include',
    })

    if (!response.ok) {
        return false
    }

    const data = await response.json()
    return data.enabled === true
}