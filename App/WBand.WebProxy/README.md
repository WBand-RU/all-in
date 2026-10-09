# Cookie authentication (BFF)

Open the application through the proxy, locally at `http://localhost:4000/app`.
Keycloak is available at `https://localhost:28080`. Aspire passes the actual frontend,
API, Redis and Keycloak addresses to the proxy. The Vite dev server forwards auth/API
requests to the proxy as well.

The proxy uses the confidential `spa-bff-client` with authorization code + PKCE.
Access, refresh and ID tokens are stored in encrypted Redis session tickets. The
browser gets an HttpOnly session cookie. Production cookies use the `__Host-` prefix,
Secure, Path=/ and SameSite=Lax; development also supports HTTP on localhost.
Redis holds the shared Data Protection keys and encrypted refresh coordination results.
Concurrent requests, including requests to different replicas, share one token refresh.

- `GET /auth/login?returnUrl=/app` starts login and permits only local return paths.
- `GET /auth/me` returns `id`, `username`, `email`, `name`, `avatar`, realm `roles`,
  and `csrfToken`; anonymous requests return 401. Responses are not cached.
- `POST /auth/logout` requires the CSRF token and signs out of both the proxy and Keycloak.
  Send `X-CSRF-TOKEN` or a form field named `__RequestVerificationToken`.
- Every unsafe API request requires the same CSRF token. Missing/invalid tokens return 403.
  API paths retain their prefixes (`/api`, `/band-api`, `/song-api`, etc.).
  Only API destinations receive the session's Bearer token. Browser cookies and
  caller-supplied Authorization headers are removed before forwarding.

React's `useAuthContext()` exposes the profile, all realm roles, `platformRoles`,
`hasRole`, authentication/loading/error state, and `login`, `logout`, `refresh`.
The provider gates `/app` until the session is loaded. Axios sends cookies and the
CSRF header, and restarts login on 401. OAuth tokens are never handled by JavaScript.
Generate the proxy's client after contract changes:

```sh
cd App/web
bun run generate-api -- --config orval.auth.config.mjs
```

## Keycloak and API configuration

The API validates JWT signature, exact realm issuer, lifetime, `aud=webapi`,
a UUID subject and a nonempty email.
It uses realm roles for platform policies. Its JWT verification does not require a
client secret. Cookies alone cannot authenticate directly to the API. API endpoints
require authentication by default; health endpoints and development OpenAPI are public.

AppHost imports `keycloak/realms/wband-dev-realm.json`. It defines the BFF client,
PKCE, basic/email/profile/roles scopes, the `webapi` audience mapper, platform roles, and
`keycloak-to-rabbitmq` event listener. The secret comes from AppHost's
`bff-client-secret` parameter (override the development default using User Secrets).

**Keycloak startup import skips realms that already exist.** For a persistent existing
`wband-dev` realm, apply the client, audience mapper and listener settings in the
Keycloak admin console. Keep the `basic` default client scope: Keycloak 26 uses it
to include `sub` in access tokens ([Keycloak documentation](https://www.keycloak.org/docs/latest/server_admin/index.html)). Match the BFF secret to the AppHost parameter. Do not delete
the persistent volume to apply configuration.

The provider JAR belongs under `/opt/keycloak/providers` (read-only mount). Its
connection must use RabbitMQ's AMQP endpoint, not the management HTTP endpoint.
Local publisher and consumer use virtual host `/` and exchange `amq.topic`, and
Keycloak waits for RabbitMQ. Local RabbitMQ permits `guest` connections from other
containers; production should use a dedicated user with permissions on the selected
vhost. Mounting the JAR alone does not enable realm event delivery: the realm must
list `keycloak-to-rabbitmq` under its event listeners.

Production configuration (environment variables or secret store):

```text
AUTHORIZED_PROXY_OAUTH_AUTHORITY=https://auth.example/realms/wband
AUTHORIZED_PROXY_OAUTH_CLIENT_ID=spa-bff-client
AUTHORIZED_PROXY_OAUTH_CLIENT_SECRET=<secret>
AUTHORIZED_PROXY_REDIS_CONNECTION_STRING=<redis connection>
AUTHORIZED_PROXY_REDIS_KEY_PREFIX=wband-proxy:
AUTHORIZED_PROXY_PUBLIC_ORIGIN=https://wband.example
WEB_COMMA_SPLIT_ADDRESSES=http://web:3000
API_COMMA_SPLIT_ADDRESSES=http://webapi:5000

KEYCLOAK_URL=https://auth.example
KEYCLOAK_REALM=wband
KEYCLOAK_CLIENT_ID=webapi
KEYCLOAK_SSL_REQUIRED=true
```

Register the exact external callback URLs in Keycloak:
`https://wband.example/signin-oidc` and
`https://wband.example/signout-callback-oidc` (post logout).
`AUTHORIZED_PROXY_PUBLIC_ORIGIN` supplies the trusted external origin when TLS is
terminated upstream. No arbitrary forwarded headers are trusted. The issuer must
match the full realm URL; root server URLs are not token issuers. Production requires
HTTPS discovery, and the BFF client's access tokens must include the API audience.
