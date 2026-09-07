#!/usr/bin/env bash
set -euo pipefail

# Retry a command, preserving its arguments and final exit code.
# The attempt limit includes the initial execution.
if [[ $# -lt 2 || ! $1 =~ ^[1-9][0-9]*$ ]]; then
    echo "Usage: bash retry.sh <max-attempts> <command> [args...]" >&2
    exit 2
fi

max_attempts=$1
shift

trap 'exit 130' INT
trap 'exit 143' TERM

for ((attempt = 1; attempt <= max_attempts; attempt++)); do
    echo "Attempt ${attempt}/${max_attempts}"
    if "$@"; then
        exit 0
    else
        status=$?
    fi

    # Do not restart commands terminated by a signal.
    if ((status >= 128 || attempt == max_attempts)); then
        exit "$status"
    fi

    echo "Command failed with exit code ${status}; retrying in 1 second." >&2
    sleep 1
done
