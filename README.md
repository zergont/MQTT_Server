# cg-mqtt-broker

Репозиторий для развёртывания **MQTT broker Mosquitto** на Ubuntu с управлением конфигурацией через **один файл** `cg-broker.yaml`.

## Архитектура

```
RUT956 → Mosquitto (10.10.10.1:1883) → telemetry2 (subscribe raw → publish decoded)
```

- **Mosquitto** — MQTT broker (сервер), маршрутизирует сообщения по топикам
- **telemetry2** — клиент-декодер (subscriber/publisher), подписывается на сырые данные и публикует декодированные
- **RUT956** — устройства, публикующие телеметрию

### Топики

| Назначение | Топик |
|------------|-------|
| RAW телеметрия | `cg/v1/telemetry/SN/<sn>` |
| Статус (опционально) | `cg/v1/status/SN/<sn>` |
| Декодированные данные | `cg/v1/decoded/SN/<sn>/pcc/<bserver_id>` |

## Безопасность

- Mosquitto слушает **только** `10.10.10.1:1883` (WireGuard/LAN интерфейс)
- Наружу порт **не открываем** — доступ только через WireGuard
- Аутентификация **выключена** по умолчанию (`allow_anonymous true`)
- При необходимости можно включить auth/ACL одним флагом в YAML

## Предусловия

- Ubuntu 22.04+ (или другой Debian-based дистрибутив)
- Доступ root (sudo)
- IP `10.10.10.1` поднят на сервере (WireGuard интерфейс)
- .NET 8 SDK установлен ([инструкция](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu))

## Установка с нуля

```bash
# 1. Клонируем репозиторий
git clone <repo-url> /opt/cg-mqtt-broker
cd /opt/cg-mqtt-broker

# 2. Редактируем конфиг (при необходимости)
cp cg-broker.example.yaml cg-broker.yaml
nano cg-broker.yaml

# 3. Устанавливаем
sudo ./scripts/install.sh
```

Скрипт `install.sh` выполнит:
1. Установку `mosquitto` и `mosquitto-clients`
2. Включение сервиса mosquitto
3. Сборку и установку утилиты `cg-mosqctl`
4. Применение конфига из `cg-broker.yaml`
5. Smoke test

## Конфигурация

Единственный файл для редактирования — `cg-broker.yaml`:

```yaml
listener:
  bind_ip: "10.10.10.1"   # IP, на котором слушает брокер
  port: 1883               # Порт MQTT

paths:
  mosquitto_conf: "/etc/mosquitto/conf.d/cg.conf"
  log: "/var/log/mosquitto/mosquitto.log"

persistence:
  enabled: true
  location: "/var/lib/mosquitto/"

logging:
  connection_messages: true

security:
  auth_enabled: false       # true — включить аутентификацию
  allow_anonymous: true
  passwd_path: "/etc/mosquitto/passwd"
  acl_path: "/etc/mosquitto/acl"
  users: []                 # Список пользователей (при auth_enabled: true)
```

### Изменение IP/порта

Отредактируйте `listener.bind_ip` и `listener.port` в `cg-broker.yaml`, затем:

```bash
sudo ./scripts/apply.sh
```

### Включение аутентификации (опционально)

```yaml
security:
  auth_enabled: true
  allow_anonymous: false
  users:
    - username: "rut956"
      password: "secret123"
      topics:
        - "cg/v1/telemetry/SN/#"
    - username: "decoder"
      password: "decoderpass"
      topics:
        - "cg/v1/#"
```

## Обновление конфигурации

```bash
cd /opt/cg-mqtt-broker
git pull
sudo ./scripts/apply.sh
```

Скрипт `apply.sh`:
1. Подтянет изменения из git (если в git-репозитории)
2. Валидирует конфиг
3. Создаст бэкап текущих файлов
4. Запишет новый конфиг в `/etc/mosquitto/conf.d/cg.conf`
5. Перезапустит mosquitto

## Утилита cg-mosqctl

### Команды

```bash
# Проверить конфиг (валидация)
cg-mosqctl check --config cg-broker.yaml

# Сгенерировать файлы в директорию (без применения)
cg-mosqctl render --config cg-broker.yaml --out out/

# Применить конфиг и перезапустить mosquitto
sudo cg-mosqctl apply --config cg-broker.yaml

# Показать пример конфига
cg-mosqctl print-example-config
```

### Поведение при ошибках

- Если конфиг невалиден — **ничего не применяется**, выводится понятная ошибка
- Перед записью делается backup (`*.bak-YYYYMMDD-HHMMSS`)

## Smoke Test

В одном терминале — подписка:
```bash
mosquitto_sub -h 10.10.10.1 -t 'cg/v1/telemetry/SN/+' -v
```

В другом — публикация:
```bash
mosquitto_pub -h 10.10.10.1 -t 'cg/v1/telemetry/SN/TEST_SN' -m 'hello'
```

Ожидаемый результат в первом терминале:
```
cg/v1/telemetry/SN/TEST_SN hello
```

## Расположение файлов

| Файл | Путь |
|------|------|
| Конфиг (YAML) | `cg-broker.yaml` (в репозитории) |
| Конфиг Mosquitto | `/etc/mosquitto/conf.d/cg.conf` |
| Логи Mosquitto | `/var/log/mosquitto/mosquitto.log` |
| Persistence | `/var/lib/mosquitto/` |
| Утилита cg-mosqctl | `/usr/local/bin/cg-mosqctl` |

## Проверка статуса

```bash
# Статус сервиса
sudo systemctl status mosquitto

# На каком порту слушает
ss -tlnp | grep mosquitto

# Последние строки лога
tail -f /var/log/mosquitto/mosquitto.log

# Полная проверка (статус + порт + лог + smoke test)
sudo ./scripts/status.sh
```

## Типовые ошибки и решения

### Mosquitto не запускается

```bash
# Проверить конфиг
mosquitto -c /etc/mosquitto/conf.d/cg.conf -t

# Посмотреть журнал
journalctl -u mosquitto -e --no-pager
```

### Ошибка "Address not available"

IP `10.10.10.1` не поднят на сервере. Убедитесь, что WireGuard интерфейс активен:

```bash
ip addr show wg0
```

### Порт занят

```bash
ss -tlnp | grep 1883
```

Другой процесс занимает порт — остановите его или измените порт в `cg-broker.yaml`.

### Конфиг не применяется

```bash
# Проверить валидность
cg-mosqctl check --config cg-broker.yaml

# Посмотреть сгенерированный конфиг
cg-mosqctl render --config cg-broker.yaml --out /tmp/test-out
cat /tmp/test-out/cg-mosquitto.conf
```
