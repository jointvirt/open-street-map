#!/usr/bin/env bash
set -euo pipefail

cd /data

PBF_FILE="/data/region.osm.pbf"
OSRM_BASE="/data/region"
DONE_FILE="/data/.osrm_prepare_done"

if [[ -f "${DONE_FILE}" && -f "${OSRM_BASE}.osrm" ]]; then
  echo "OSRM data already prepared; skipping extract/partition/customize."
  exit 0
fi

if [[ ! -f "${PBF_FILE}" ]] || [[ ! -s "${PBF_FILE}" ]]; then
  echo "Missing or empty ${PBF_FILE}. Run the osrm-download service first (docker compose)." >&2
  exit 1
fi

echo "Running osrm-extract on ${PBF_FILE}..."
osrm-extract -p /opt/car.lua "${PBF_FILE}"

echo "Running osrm-partition..."
osrm-partition "${OSRM_BASE}.osrm"

echo "Running osrm-customize..."
osrm-customize "${OSRM_BASE}.osrm"

touch "${DONE_FILE}"
echo "OSRM preparation completed successfully."
