#!/usr/bin/env bash
set -euo pipefail

if rg --hidden --glob '!**/.git/**' --glob '!**/bin/**' --glob '!**/obj/**' \
  '(sk|rk)_(live|test)_[A-Za-z0-9]{20,}|whsec_[A-Za-z0-9]{20,}' .; then
  echo 'Potential Stripe secret detected. Remove it, rotate it, and inject it through the local .env or approved secret store.' >&2
  exit 1
fi
