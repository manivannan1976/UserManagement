export const environment = {
  production: true,
  apiBaseUrl: '/api',
  auth0: {
    domain: 'YOUR_AUTH0_DOMAIN',
    clientId: 'YOUR_AUTH0_CLIENT_ID',
    audience: 'YOUR_API_IDENTIFIER',
    rolesClaim: 'https://s2-portal.example.com/roles',
    redirectUri: 'https://YOUR_PORTAL_ORIGIN/callback',
    logoutRedirectUri: 'https://YOUR_PORTAL_ORIGIN/login',
    connection: ''
  },
  demoAuth: {
    enabled: false,
    email: '',
    password: '',
    sessionStorageKey: 'brokerPortal.demoSession'
  }
};