#!/bin/sh
# Скачивание .osm.pbf отдельным образом с curl — образ OSRM старый и без рабочего apt/wget.
set -eu

cd /data

PBF_URL="${OSM_PBF_URL:-https://download.geofabrik.de/russia/central-fed-district-latest.osm.pbf}"
PBF_FILE="/data/region.osm.pbf"
# Любой нормальный extract Geofabrik > 1 MiB. Сотни байт = почти всегда HTML-ошибка, а не .pbf.
MIN_BYTES="${OSM_PBF_MIN_BYTES:-1048576}"

if [ -f "$PBF_FILE" ] && [ -s "$PBF_FILE" ]; then
  SIZE=$(wc -c < "$PBF_FILE" | tr -d ' ')
  if [ "$SIZE" -ge "$MIN_BYTES" ]; then
    echo "OSM extract already present: $PBF_FILE (${SIZE} bytes)"
    exit 0
  fi
  echo "Removing invalid too-small file (${SIZE} bytes), will re-download..."
  rm -f "$PBF_FILE"
fi

echo "Downloading OSM extract from ${PBF_URL}"
echo "(Wait until curl shows a plausible Total size for your extract — not a few hundred bytes.)"

# Geofabrik: latest → 302 на dated-файл; иногда из Docker ломается IPv6 или HTTP/2 — фиксируем IPv4 и HTTP/1.1.
# Referer + UA снижают шанс отрезания запроса.
curl -fSL \
  --location \
  --max-redirs 15 \
  --http1.1 \
  --ipv4 \
  --connect-timeout 60 \
  --max-time 0 \
  --retry 5 \
  --retry-delay 15 \
  --retry-all-errors \
  -H "User-Agent: curl/OSRM-RoutingService (Docker; +https://www.openstreetmap.org/copyright)" \
  -H "Referer: https://download.geofabrik.de/" \
  -H "Accept: */*" \
  -o "${PBF_FILE}.part" \
  "${PBF_URL}"

SIZE=$(wc -c < "${PBF_FILE}.part" | tr -d ' ')
if [ "$SIZE" -lt "$MIN_BYTES" ]; then
  echo "ERROR: downloaded file is only ${SIZE} bytes — this is not a valid .osm.pbf (expect at least ${MIN_BYTES} bytes)." >&2
  echo "First bytes (often HTML error page):" >&2
  head -c 400 "${PBF_FILE}.part" >&2 || true
  echo >&2
  echo "Try: another mirror, VPN, or a smaller OSM_PBF_URL (e.g. central-fed-district). Remove bad file: docker compose down -v" >&2
  rm -f "${PBF_FILE}.part"
  exit 1
fi

mv "${PBF_FILE}.part" "$PBF_FILE"
echo "Download finished: $PBF_FILE (${SIZE} bytes)"
