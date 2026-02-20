function getToken() {
  return localStorage.getItem('accessToken') || '';
}

function setToken(token) {
  if (!token) localStorage.removeItem('accessToken');
  else localStorage.setItem('accessToken', token);
}

async function login(email, password) {
  const r = await fetch('/api/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password })
  });

  if (!r.ok) {
    const t = await r.text();
    throw new Error(t || ('HTTP ' + r.status));
  }

  const data = await r.json();
  const token = data.accessToken || data.AccessToken || '';
  setToken(token);
  return token;
}

async function apiFetch(url, options) {
  const opts = options ? { ...options } : {};
  const method = (opts.method || 'GET').toUpperCase();
  const isApi = typeof url === 'string' && url.startsWith('/api/');

  if (!opts.headers) opts.headers = {};

  if (isApi && method !== 'GET') {
    const token = getToken();
    if (token) opts.headers['Authorization'] = 'Bearer ' + token;
  }

  return fetch(url, opts);
}

window.auth = { getToken, setToken, login };
window.apiFetch = apiFetch;
