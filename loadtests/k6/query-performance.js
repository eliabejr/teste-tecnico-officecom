import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://host.docker.internal:8000';
const VUS = parseInt(__ENV.VUS || '500', 10);
const ITERATIONS_PER_VU = parseInt(__ENV.ITERATIONS_PER_VU || '10', 10);

export const options = {
  scenarios: {
    queries: {
      executor: 'per-vu-iterations',
      vus: VUS,
      iterations: ITERATIONS_PER_VU,
      maxDuration: __ENV.MAX_DURATION || '5m',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    // P95 deve ser menor que 500ms para consultas
    'http_req_duration{name:GET /api/accounts/{id}/balance}': ['p(95)<500'],
    'http_req_duration{name:GET /api/accounts/{id}}': ['p(95)<500'],
    'http_req_duration{name:GET /api/accounts/{id}/statement}': ['p(95)<500'],
  },
};

export function setup() {
  // Criar uma conta para testes
  const createAccountRes = http.post(
    `${BASE_URL}/api/accounts`,
    JSON.stringify({ ownerName: 'k6-query-test', initialBalance: 1000 }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'POST /api/accounts' } }
  );

  check(createAccountRes, {
    'account created (201)': (r) => r.status === 201,
  });

  const body = createAccountRes.json();
  const accountId = body?.id;

  // Criar algumas transações para popular o extrato
  for (let i = 0; i < 50; i++) {
    http.post(
      `${BASE_URL}/api/transactions/deposit`,
      JSON.stringify({ accountId, amount: 1, description: `Setup deposit ${i}` }),
      { headers: { 'Content-Type': 'application/json' }, tags: { name: 'POST /api/transactions/deposit' } }
    );
  }

  return { accountId };
}

export default function (data) {
  const accountId = data.accountId;

  // Teste GET /api/accounts/{id}
  const getAccountRes = http.get(`${BASE_URL}/api/accounts/${accountId}`, {
    tags: { name: 'GET /api/accounts/{id}' },
  });

  check(getAccountRes, {
    'get account ok (200)': (r) => r.status === 200,
  });

  // Teste GET /api/accounts/{id}/balance
  const balanceRes = http.get(`${BASE_URL}/api/accounts/${accountId}/balance`, {
    tags: { name: 'GET /api/accounts/{id}/balance' },
  });

  check(balanceRes, {
    'balance ok (200)': (r) => r.status === 200,
    'balance is valid': (r) => {
      const json = r.json();
      return json && typeof json.balance === 'number';
    },
  });

  // Teste GET /api/accounts/{id}/statement (primeira página)
  const statementRes = http.get(`${BASE_URL}/api/accounts/${accountId}/statement?page=1&pageSize=20`, {
    tags: { name: 'GET /api/accounts/{id}/statement' },
  });

  check(statementRes, {
    'statement ok (200)': (r) => r.status === 200,
    'statement has transactions': (r) => {
      const json = r.json();
      return json && Array.isArray(json.transactions);
    },
  });
}

export function teardown(data) {
  // Limpeza opcional - apenas valida que a conta ainda existe
  const accountId = data.accountId;
  const balanceRes = http.get(`${BASE_URL}/api/accounts/${accountId}/balance`, {
    tags: { name: 'GET /api/accounts/{id}/balance' },
  });

  check(balanceRes, {
    'final balance check ok': (r) => r.status === 200,
  });
}
