const NAMESPACE = 'https://s2-portal.example.com';

exports.onExecutePostLogin = async (event, api) => {
  if (!event.authorization) {
    console.log('Authorization data is unavailable for this login.');
    return;
  }

  const eventRoles = event.authorization.roles;
  const roles = Array.isArray(eventRoles)
    ? eventRoles.filter(role => typeof role === 'string')
    : [];

  console.log('[Broker Portal role diagnostics]', {
    authorizationDataPresent: true,
    rolesPresent: Array.isArray(eventRoles),
    roleNames: roles
  });

  api.idToken.setCustomClaim(`${NAMESPACE}/roles`, roles);
  api.accessToken.setCustomClaim(`${NAMESPACE}/roles`, roles);
};
