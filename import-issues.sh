#!/usr/bin/env bash
# GitLab issue importer
# Usage: GITLAB_TOKEN=<pat> GITLAB_PROJECT_ID=<id> ./import-issues.sh
#
# Prerequisites:
#   - curl
#   - jq  (brew install jq / apt install jq)
#   - A GitLab Personal Access Token with api scope
#   - Your project's numeric ID (found in Settings → General → Project ID)

set -euo pipefail

: "${GITLAB_TOKEN:?Set GITLAB_TOKEN to your Personal Access Token}"
: "${GITLAB_PROJECT_ID:?Set GITLAB_PROJECT_ID to your numeric project ID}"
GITLAB_URL="${GITLAB_URL:-https://gitlab.com}"

ISSUES_FILE="$(dirname "$0")/gitlab-issues.json"
TOTAL=$(jq 'length' "$ISSUES_FILE")
echo "Importing $TOTAL issues to project $GITLAB_PROJECT_ID ..."

for i in $(seq 0 $((TOTAL - 1))); do
  TITLE=$(jq -r ".[$i].title" "$ISSUES_FILE")
  DESC=$(jq -r ".[$i].description" "$ISSUES_FILE")
  LABELS=$(jq -r ".[$i].labels | join(\",\")" "$ISSUES_FILE")
  MILESTONE=$(jq -r ".[$i].milestone" "$ISSUES_FILE")
  WEIGHT=$(jq -r ".[$i].weight" "$ISSUES_FILE")

  echo "  [$((i+1))/$TOTAL] Creating: $TITLE"

  curl -s --fail-with-body \
    --request POST \
    --header "PRIVATE-TOKEN: $GITLAB_TOKEN" \
    --header "Content-Type: application/json" \
    --data "$(jq -n \
      --arg title "$TITLE" \
      --arg desc "$DESC" \
      --arg labels "$LABELS" \
      --arg weight "$WEIGHT" \
      '{title: $title, description: $desc, labels: $labels, weight: ($weight | tonumber)}')" \
    "$GITLAB_URL/api/v4/projects/$GITLAB_PROJECT_ID/issues" \
    | jq -r '"    → #\(.iid) created"'

  sleep 0.3  # stay under GitLab rate limit
done

echo "Done."
