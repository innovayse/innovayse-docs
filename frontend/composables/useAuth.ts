import { UserManager, type User } from 'oidc-client-ts'

let userManager: UserManager | null = null

function getUserManager(): UserManager {
  if (userManager) return userManager

  const config = useRuntimeConfig()
  // Same useState key as useAuth()'s — Nuxt caches by key, so this is the same ref.
  const user = useState<User | null>('auth-user', () => null)

  userManager = new UserManager({
    authority: config.public.ssoAuthority as string,
    client_id: 'innovayse-docs',
    redirect_uri: `${window.location.origin}/auth/callback`,
    post_logout_redirect_uri: window.location.origin,
    response_type: 'code',
    // 'offline_access' gets us a refresh token — SSO already grants this client the
    // RefreshToken grant type — and automaticSilentRenew uses it to renew the access
    // token in the background before it expires. Without this, the ~15min access token
    // just goes stale and every authenticated call (including document creation) starts
    // silently 401ing until the user manually logs out and back in.
    scope: 'openid profile email offline_access',
    automaticSilentRenew: true,
  })

  // Silent renewal happens inside oidc-client-ts's own background timer, not through any
  // of our own functions — without this listener our reactive `user` (and the `accessToken`
  // computed from it) would keep pointing at the stale pre-renewal token forever.
  userManager.events.addUserLoaded((renewedUser) => {
    user.value = renewedUser
  })
  userManager.events.addUserUnloaded(() => {
    user.value = null
  })
  userManager.events.addSilentRenewError((error) => {
    // Refresh token itself expired/revoked — nothing left to do silently; the next
    // authenticated call will 401 and the user has to log in again.
    console.error('Silent token renew failed', error)
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

  /** @param returnTo Path to send the user back to after sign-in (e.g. the document URL they
   * landed on cold, with no local session) — round-tripped through OIDC's `state` and read
   * back in pages/auth/callback.vue. Without this, a signed-out visitor always lands on the
   * home page after logging in, losing whatever page (or share link) they actually wanted. */
  async function login(returnTo?: string): Promise<void> {
    if (!import.meta.client) return
    await getUserManager().signinRedirect(returnTo ? { state: { returnTo } } : undefined)
  }

  async function logout(): Promise<void> {
    if (!import.meta.client) return
    await getUserManager().signoutRedirect()
    user.value = null
  }

  /** Completes the authorization code flow on the /auth/callback page. Returns the signed-in
   * user (carrying the `returnTo` state login() attached, if any) so the callback page can
   * navigate back to where the visitor actually came from instead of always the home page. */
  async function handleLoginCallback(): Promise<User | undefined> {
    if (!import.meta.client) return undefined
    user.value = await getUserManager().signinRedirectCallback()
    return user.value
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
