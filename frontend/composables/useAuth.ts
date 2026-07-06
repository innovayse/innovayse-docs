import { UserManager, type User } from 'oidc-client-ts'

let userManager: UserManager | null = null

function getUserManager(): UserManager {
  if (userManager) return userManager

  const config = useRuntimeConfig()

  userManager = new UserManager({
    authority: config.public.ssoAuthority as string,
    client_id: 'innovayse-docs',
    redirect_uri: `${window.location.origin}/auth/callback`,
    post_logout_redirect_uri: window.location.origin,
    response_type: 'code',
    scope: 'openid profile email',
  })

  return userManager
}

export function useAuth() {
  // Shared across all consumers via Nuxt's SSR-safe useState.
  const user = useState<User | null>('auth-user', () => null)

  const accessToken = computed(() => user.value?.access_token ?? null)

  async function loadUser(): Promise<void> {
    if (!import.meta.client) return
    user.value = await getUserManager().getUser()
  }

  async function login(): Promise<void> {
    if (!import.meta.client) return
    await getUserManager().signinRedirect()
  }

  async function logout(): Promise<void> {
    if (!import.meta.client) return
    await getUserManager().signoutRedirect()
    user.value = null
  }

  /** Completes the authorization code flow on the /auth/callback page. */
  async function handleLoginCallback(): Promise<void> {
    if (!import.meta.client) return
    user.value = await getUserManager().signinRedirectCallback()
  }

  return {
    user,
    accessToken,
    login,
    logout,
    loadUser,
    handleLoginCallback,
  }
}
