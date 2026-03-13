#!/bin/sh
set -eu

api_url="${LEDGERPULSE_API_HEALTHCHECK_URL:-http://api:8080/health}"
max_attempts="${LEDGERPULSE_API_WAIT_ATTEMPTS:-30}"
sleep_seconds="${LEDGERPULSE_API_WAIT_INTERVAL_SECONDS:-1}"

attempt=1
while [ "$attempt" -le "$max_attempts" ]; do
    if wget -q -O /dev/null "$api_url"; then
        echo "API is reachable at $api_url."
        exit 0
    fi

    echo "Waiting for API at $api_url (attempt $attempt/$max_attempts)..."
    attempt=$((attempt + 1))
    sleep "$sleep_seconds"
done

echo "API did not become reachable at $api_url after $max_attempts attempts." >&2
exit 1
