# RoutingService

**ASP.NET Core 8** Web API в «продакшен-стиле»: маршруты и матрицы (ETA / расстояния) через **self-hosted OSRM**. Подходит для **локального** запуска, **Docker** или любой среды, где доступен URL OSRM.

## Возможности

- **POST** `/api/routes/route` — маршрут между двумя точками (расстояние, время, опционально геометрия).
- **POST** `/api/routes/matrix` — матрицы времени и расстояния (OSRM **Table**).
- **GET** `/health/live` — liveness (только процесс API).
- **GET** `/health/ready` — readiness (API + проверка OSRM).
- **Swagger UI** на `/swagger` (включён везде, в том числе в Docker).
- **Структурированные JSON-логи** в stdout.
- **Конфигурация** через `appsettings*.json` и переменные окружения (`Routing__Osrm__*`).
- **Слои**: `RoutingService.Api`, `RoutingService.Application`, `RoutingService.Infrastructure`.
- **xUnit**-тесты с моком `HttpMessageHandler`.

## Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (или более новый SDK с таргетом `net8.0`).
- [Docker](https://docs.docker.com/get-docker/) и Docker Compose v2 (для стека одной командой).
- По желанию: [.NET 8 **runtime**](https://dotnet.microsoft.com/download/dotnet/8.0), если нужен запуск тестов без roll-forward (в репозитории в `Directory.Build.props` задано `RollForward=LatestMajor` для случаев, когда установлен только более новый runtime).

## Запуск одной командой (Docker)

Из каталога **`RoutingService`** (где лежит `docker-compose.yml`):

```bash
docker compose up --build
```

### Что происходит при первом запуске (подготовка данных)

Да, сервис **сам готовит данные для выбранного региона**:

1. **`osrm-download`** (образ `curlimages/curl`) скачивает **`.osm.pbf`** в volume, если файла ещё нет. Скачивание вынесено отдельно: образ **`osrm/osrm-backend`** на старом Debian **без** рабочих репозиториев `apt` и без `wget`/`curl`, поэтому поставить их внутри него нельзя.
2. **`osrm-prepare`** запускает **osrm-extract → osrm-partition → osrm-customize** (MLD) и создаёт **`/data/region.osrm`**.
3. Маркер **`.osrm_prepare_done`** — при следующих `up` без **`docker compose down -v`** повторная тяжёлая подготовка не делается.
4. **`osrm`** поднимает **`osrm-routed`**.
5. **`routing-api`** слушает **8080** и ждёт готовности OSRM (healthcheck по порту **5000**).

**По умолчанию** качается **Центральный федеральный округ** (`central-fed-district`) — в основном **Москва и область**, без всей страны. Один граф = один файл `.osm.pbf`.

Ориентировочные размеры архива **`.osm.pbf`** (Geofabrik, порядок величины; точные цифры меняются, смотрите на [странице региона](https://download.geofabrik.de/)):

| Регион | Пример URL | Ориентировочно |
|--------|------------|----------------|
| **ЦФО (дефолт в compose)** — Москва и область | `.../russia/central-fed-district-latest.osm.pbf` | **~0.3–0.8 ГиБ** |
| СЗФО — СПб и северо-запад | `.../russia/northwestern-fed-district-latest.osm.pbf` | **~0.3–0.7 ГиБ** |
| **Вся Россия** | `.../russia-latest.osm.pbf` | **~3–4+ ГиБ** |
| Очень маленький тест (не РФ) | например `.../europe/andorra-latest.osm.pbf` | **несколько МиБ** |

В логах `curl` поле **Total** должно совпадать с ожидаемым порядком (не **сотни байт**). Если видите **~200–500 байт** — это не карта; скрипт отклонит файл. Сброс: **`docker compose down -v`**, проверьте сеть/VPN.

Нужна **вся Россия** — задайте **`OSM_PBF_URL=https://download.geofabrik.de/russia-latest.osm.pbf`** (несколько ГиБ). Два региона без склейки в один PBF одним сервисом не объединяются.

После смены региона с уже собранным volume выполните **`docker compose down -v`**, иначе останутся старые файлы.

Повторные запуски **без** `-v` после успешной подготовки обычно **быстрые**.

Если вы используете **только СЗФО** (без Москвы в графе), проверка готовности API и healthcheck OSRM в `docker-compose` завязаны на **Москву** — маршрут может не найтись. Тогда либо переключите `OSM_PBF_URL` на **всю Россию**, либо вручную поменяйте координаты в healthcheck в `docker-compose.yml` и в `PingAsync` в `OsrmRouteProvider.cs` на точки в **Санкт-Петербурге**.

### Если `docker compose` падает с 403 на `ghcr.io`

В `docker-compose.yml` используется образ **`osrm/osrm-backend`** с **Docker Hub**, а не GitHub Container Registry: у анонимных запросов к `ghcr.io` иногда возвращается **403** при получении токена. Если всё же меняете образ на GHCR и снова видите 403 — выполните `docker login ghcr.io` (нужен PAT GitHub с правом `read:packages`) или используйте VPN/другую сеть.

### Ошибка `Name or service not known (osrm)` или 502 «Routing engine unreachable»

По умолчанию API в Docker подключается к OSRM по **`http://host.docker.internal:5000`**: запрос идёт на **хост**, где порт **5000** проброшен из контейнера `osrm` (так надёжнее, чем DNS-имя `osrm` на Docker Desktop).

- Убедитесь, что в `docker compose ps` сервис **`osrm` в Up** и в логах нет падений, а порт **5000** опубликован (`0.0.0.0:5000->5000`).
- Перезапуск после смены `appsettings` / compose: `docker compose up --build`.
- Если на Linux без `host.docker.internal`, в compose уже добавлено `extra_hosts: host.docker.internal:host-gateway`.
- Явный URL: переменная **`ROUTING_OSRM_BASE_URL`**, например  
  `ROUTING_OSRM_BASE_URL=http://osrm:5000` — если в вашей среде резолвится имя сервиса.
- Локальный **`dotnet run`**: **`Routing__Osrm__BaseUrl=http://localhost:5000`**, **`ASPNETCORE_ENVIRONMENT=Development`**, OSRM с портом 5000 на машине.

### Docker Desktop: `osrm` остановлен, значок AMD64

- **Если `osrm` / `osrm-prepare` в статусе Stopped**, а `routing-api` Running — маршруты **не** заработают: движок на порту 5000 не слушает. Запустите весь стек: **`docker compose up`** из папки проекта или кнопку **Start** у группы `routingservice`, не только API.
- **Предупреждение AMD64 на Mac (M1/M2/M3)** значит, что образ OSRM собран под **Intel**; на Apple Silicon Docker **эмулирует** (обычно работает, но медленнее и тяжелее по CPU). В `docker-compose` для сервисов OSRM задано **`platform: linux/amd64`** явно. Если контейнер падает — смотрите **`docker compose logs osrm`** и **`osrm-prepare`**.

## Адреса после старта

| Что | URL |
|-----|-----|
| Swagger UI | http://localhost:8080/swagger |
| OpenAPI JSON | http://localhost:8080/swagger/v1/swagger.json |
| Liveness | http://localhost:8080/health/live |
| Readiness | http://localhost:8080/health/ready |
| OSRM (опционально, отладка) | http://localhost:5000 |

## Примеры запросов

**Маршрут** (пример по **Москве**; в API — `latitude` / `longitude`; для OSRM порядок lon,lat выставляется внутри сервиса):

```bash
curl -sS -X POST http://localhost:8080/api/routes/route \
  -H "Content-Type: application/json" \
  -d '{
    "origin": { "latitude": 55.7558, "longitude": 37.6173 },
    "destination": { "latitude": 55.7520, "longitude": 37.6156 },
    "profile": "driving"
  }'
```

**Матрица** (три точки в Москве):

```bash
curl -sS -X POST http://localhost:8080/api/routes/matrix \
  -H "Content-Type: application/json" \
  -d '{
    "points": [
      { "latitude": 55.7558, "longitude": 37.6173 },
      { "latitude": 55.7520, "longitude": 37.6156 },
      { "latitude": 55.7300, "longitude": 37.6400 }
    ],
    "profile": "driving"
  }'
```

## Смена региона OSM (`OSM_PBF_URL`)

Задайте **`OSM_PBF_URL`** перед `docker compose up --build` или пропишите в **`.env`** рядом с `docker-compose.yml`:

```bash
# Вся Россия (несколько ГиБ) — только если нужен весь граф страны
export OSM_PBF_URL="https://download.geofabrik.de/russia-latest.osm.pbf"
docker compose up --build
```

Если **`OSM_PBF_URL` не задана**, в `docker-compose` подставляется **ЦФО** (Москва и область, ~сотни МиБ):  
`https://download.geofabrik.de/russia/central-fed-district-latest.osm.pbf`.

## Остановка и сброс

### Остановить контейнеры

```bash
docker compose down
```

### Удалить volumes и начать с нуля

```bash
docker compose down -v
```

Удалятся подготовленные данные OSRM; при следующем `up` снова будет скачивание и полная подготовка.

## Локальный запуск без Docker

1. **OSRM** должен быть доступен по URL из `appsettings.json` → `Routing:Osrm:BaseUrl` (по умолчанию `http://localhost:5000`).
2. При другом хосте/порте задайте переменную, например:

   ```bash
   export Routing__Osrm__BaseUrl="http://localhost:5000"
   cd src/RoutingService.Api
   dotnet run
   ```

3. Откройте http://localhost:8080/swagger (или URL из вывода консоли).

## Конфигурация

### `Routing:Osrm` (см. `appsettings.json`)

| Ключ | Описание |
|------|----------|
| `BaseUrl` | Базовый URL OSRM (без слэша в конце). |
| `TimeoutMs` | Таймаут HTTP-клиента. |
| `DefaultProfile` | Профиль, если в запросе не указан `profile`. |
| `EnableGeometry` | При `true` в ответе маршрута может быть геометрия (упрощённая полилиния). |
| `AllowedProfiles` | Какие профили API разрешены. В **Docker по умолчанию** только **`driving`**, т.к. граф собран с **car** (`car.lua`). |

### Переменные окружения (Docker / Kubernetes)

Вложенные ключи через двойное подчёркивание:

- `Routing__Osrm__BaseUrl=http://host.docker.internal:5000` (по умолчанию в compose; или `http://osrm:5000`, если DNS сервисов работает)
- `ASPNETCORE_ENVIRONMENT=Docker` (подхватит `appsettings.Docker.json`)
- `ASPNETCORE_URLS=http://0.0.0.0:8080`

### Порядок координат в OSRM

В URL OSRM используется **долгота, широта**. В публичных DTO — **широта / долгота**; маппинг делается в инфраструктуре.

## Профили OSRM и образ по умолчанию

В API в DTO допустимы **`driving`**, **`walking`**, **`cycling`**. В **стандартном Docker** датасет собирается с **`/opt/car.lua`**, поэтому в `appsettings.Docker.json` через `AllowedProfiles` включён только **`driving`**. Реалистичный walking/cycling потребует отдельных графов и процессов OSRM — это выходит за рамки этого минимального compose.

## Структура решения

```
RoutingService/
├── RoutingService.slnx
├── Directory.Build.props
├── Dockerfile
├── docker-compose.yml
├── docker/osrm/download.sh
├── docker/osrm/prepare.sh
├── src/
│   ├── RoutingService.Api/
│   ├── RoutingService.Application/
│   └── RoutingService.Infrastructure/
└── tests/
    └── RoutingService.Tests/
```

## Тесты

```bash
dotnet test
```

Если не установлен runtime .NET 8, можно попробовать:

```bash
DOTNET_ROLL_FORWARD=LatestMajor dotnet test
```

## Лицензия

При необходимости см. `LICENSE` в корне монорепозитория.
