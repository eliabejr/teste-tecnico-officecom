#!/usr/bin/env bash
set -euo pipefail

VUS="${1:-1000}"
ITERATIONS_PER_VU="${2:-1}"

BASE_URL="${BASE_URL:-http://host.docker.internal:8000}"
AMOUNT="${AMOUNT:-1}"
MAX_DURATION="${MAX_DURATION:-5m}"

# Create reports directory
REPORTS_DIR="$(pwd)/loadtests/reports"
mkdir -p "$REPORTS_DIR"

# Generate timestamp (format: YYYYMMDDTHHMMSS)
TIMESTAMP=$(date +"%Y%m%dT%H%M%S")
REPORT_PREFIX="${REPORTS_DIR}/purchase_${TIMESTAMP}"

docker-compose up -d

docker run --rm -i \
  -v "$(pwd)/loadtests/k6:/scripts" \
  -v "$REPORTS_DIR:/reports" \
  grafana/k6 run \
  --out json="/reports/purchase_${TIMESTAMP}.json" \
  -e BASE_URL="$BASE_URL" \
  -e VUS="$VUS" \
  -e ITERATIONS_PER_VU="$ITERATIONS_PER_VU" \
  -e AMOUNT="$AMOUNT" \
  -e MAX_DURATION="$MAX_DURATION" \
  /scripts/purchase-concurrency.js 2>&1 | tee "${REPORT_PREFIX}.log"

echo ""
echo "Reports saved:"
echo "  - Metrics: ${REPORT_PREFIX}.json"
echo "  - Log: ${REPORT_PREFIX}.log"