import http from 'k6/http';
import { check, fail } from 'k6';
 
function jsonHeaders(extra = {}) {
  return { 'Content-Type': 'application/json', ...extra };
}
 
export function authHeaders(accessToken, extra = {}) {
  return { Authorization: `Bearer ${accessToken}`, ...extra };
}
 
function nowId() {
  return new Date().toISOString().replace(/[-:.TZ]/g, '');
}
 
export function createK6Credentials(testName = 'loadtest') {
  const prefix = __ENV.AUTH_EMAIL_PREFIX || 'k6';
  const runId = __ENV.RUN_ID || nowId();
  const email = `${prefix}-${testName}-${runId}@example.com`.toLowerCase();
  const password = __ENV.AUTH_PASSWORD || 'P@ssw0rd123!';
  return { email, password };
}
 
export function registerOrLogin(baseUrl, credentials) {
  const registerRes = http.post(
    `${baseUrl}/api/auth/register`,
    JSON.stringify({ email: credentials.email, password: credentials.password }),
    { headers: jsonHeaders(), tags: { name: 'POST /api/auth/register' } }
  );
 
  const registerOk = check(registerRes, {
    'register ok (201|409)': (r) => r.status === 201 || r.status === 409,
  });
 
  if (!registerOk) {
    console.error(`register failed: status=${registerRes.status} body=${registerRes.body}`);
    fail('register failed');
  }
 
  const shouldValidateLogin = (__ENV.AUTH_VALIDATE_LOGIN || '').toLowerCase() === 'true';
 
  if (registerRes.status === 201 && !shouldValidateLogin) {
    const token = registerRes.json()?.accessToken;
    if (!token) fail('register response missing accessToken');
    return token;
  }
 
  const loginRes = http.post(
    `${baseUrl}/api/auth/login`,
    JSON.stringify({ email: credentials.email, password: credentials.password }),
    { headers: jsonHeaders(), tags: { name: 'POST /api/auth/login' } }
  );
 
  const loginOk = check(loginRes, {
    'login ok (200)': (r) => r.status === 200,
  });
 
  if (!loginOk) {
    console.error(`login failed: status=${loginRes.status} body=${loginRes.body}`);
    fail('login failed');
  }
 
  const token = loginRes.json()?.accessToken;
  if (!token) fail('login response missing accessToken');
  return token;
}
 
