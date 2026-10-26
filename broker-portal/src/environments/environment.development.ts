export const environment = {
  production: false,
  apiBaseUrl: '/api',
  auth0: {
    domain: 'dev-mo5xpgqtpslsitwy.us.auth0.com',
    clientId: 'raov4KL6dlj9zsKNiWTAcWPzsOeUeL0R',
    audience: 'YOUR_API_IDENTIFIER',
    rolesClaim: 'https://s2-portal.example.com/roles',
    redirectUri: 'http://localhost:4200/callback',
    logoutRedirectUri: 'http://localhost:4200/login',
    connection: ''
  },
  demoAuth: {
    enabled: false,
    email: '',
    password: '',
    sessionStorageKey: 'brokerPortal.demoSession'
  }
};