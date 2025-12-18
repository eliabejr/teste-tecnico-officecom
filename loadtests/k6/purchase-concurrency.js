import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://host.docker.internal:8000';
const VUS = parseInt(__ENV.VUS || '1000', 10);
const ITERATIONS_PER_VU = parseInt(__ENV.ITERATIONS_PER_VU || '1', 10);
const AMOUNT = Number(__ENV.AMOUNT || '1');

export const options = {
  scenarios: {
    purchases: {
      executor: 'per-vu-iterations',
      vus: VUS,
      iterations: ITERATIONS_PER_VU,
      maxDuration: __ENV.MAX_DURATION || '5m',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1500'],
  },
};

export function setup() {
  const initialBalance = VUS * ITERATIONS_PER_VU * AMOUNT;

  const createAccountRes = http.post(
    `${BASE_URL}/api/accounts`,
    JSON.stringify({ ownerName: 'k6-loadtest', initialBalance }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'POST /api/accounts' } }
  );

  check(createAccountRes, {
    'account created (201)': (r) => r.status === 201,
  });

  const body = createAccountRes.json();
  return { accountId: body?.id };
}

export default function (data) {
  const accountId = data.accountId;

  const purchaseRes = http.post(
    `${BASE_URL}/api/transactions/purchase`,
    JSON.stringify({ accountId, amount: AMOUNT, merchant: `k6-M${__VU}` }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'POST /api/transactions/purchase' } }
  );

  check(purchaseRes, {
    'purchase created (201)': (r) => r.status === 201,
  });

  if (ITERATIONS_PER_VU > 1) sleep(0.01);
}

export function teardown(data) {
  const accountId = data.accountId;
  const expectedTx = VUS * ITERATIONS_PER_VU;

  const balanceRes = http.get(`${BASE_URL}/api/accounts/${accountId}/balance`, {
    tags: { name: 'GET /api/accounts/{id}/balance' },
  });

  check(balanceRes, {
    'balance ok (200)': (r) => r.status === 200,
    'balance is 0': (r) => Number(r.json()?.balance) === 0,
  });

  const statementRes = http.get(
    `${BASE_URL}/api/accounts/${accountId}/statement?page=1&pageSize=100`,
    { tags: { name: 'GET /api/accounts/{id}/statement' } }
  );

  check(statementRes, {
    'statement ok (200)': (r) => r.status === 200,
    'all tx registered': (r) => Number(r.json()?.totalTransactions) === expectedTx,
  });
}
