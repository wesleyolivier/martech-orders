#!/usr/bin/env bash
set -euo pipefail

export PATH="$PATH:/root/.dotnet/tools"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

if [ -z "${SONAR_TOKEN:-}" ]; then
  echo "SONAR_TOKEN is empty."
  echo "Start SonarQube with 'docker compose --profile quality up sonarqube', create a token"
  echo "at http://localhost:9000, then run this service again with SONAR_TOKEN=<token>."
  exit 1
fi

echo "Waiting for SonarQube at ${SONAR_HOST_URL} ..."
for _ in $(seq 1 60); do
  if curl --silent --fail "${SONAR_HOST_URL}/api/system/status" | grep --quiet '"status":"UP"'; then
    echo "SonarQube is up."
    break
  fi
  sleep 5
done

if ! curl --silent --fail "${SONAR_HOST_URL}/api/system/status" | grep --quiet '"status":"UP"'; then
  echo "SonarQube did not become available in time."
  exit 1
fi

apt-get update
apt-get install --yes --no-install-recommends default-jre-headless
rm --recursive --force /var/lib/apt/lists/*

dotnet tool install --global dotnet-sonarscanner
dotnet tool install --global dotnet-coverage

dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.cs.vscoveragexml.reportsPaths=coverage.xml \
  /d:sonar.scanner.scanAll=false

dotnet build MarTech.Orders.sln --configuration Release

dotnet-coverage collect "dotnet test MarTech.Orders.sln --configuration Release --no-build" \
  --output coverage.xml \
  --output-format xml

dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
