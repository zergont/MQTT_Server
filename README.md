# cg-mqtt-broker

Репозиторий для развёртывания **MQTT broker Mosquitto** на Ubuntu-сервере.  
Вся конфигурация — через **один файл** `cg-broker.yaml`.

Репозиторий: <https://github.com/zergont/MQTT_Server>

---

## Что это и зачем

Mosquitto — MQTT-брокер, который принимает телеметрию от устройств RUT956 и передаёт её декодеру `telemetry2`.  
Этот репозиторий содержит:

- **`cg-broker.yaml`** — единственный конфиг, который нужно редактировать
- **`cg-mosqctl`** — .NET 8 утилита, которая генерирует конфиг Mosquitto из YAML и применяет его
- **Shell-скрипты** — установка, обновление, проверка статуса на Ubuntu

### Схема работы

```
RUT956  ──publish──►  Mosquitto (10.10.10.1:1883)  ──subscribe──►  telemetry2
                          MQTT broker                              декодер
```

| Роль | Описание |
|------|----------|
| **Mosquitto** | MQTT broker (сервер), маршрутизирует сообщения по топикам |
| **RUT956** | Устройства, публикуют сырую телеметрию |
| **telemetry2** | Клиент-декодер, подписывается на RAW → публикует DECODED |

### Топики

| Назначение | Топик |
|------------|-------|
| RAW телеметрия | `cg/v1/telemetry/SN/<sn>` |
| Статус (опционально) | `cg/v1/status/SN/<sn>` |
| Декодированные данные | `cg/v1/decoded/SN/<sn>/pcc/<bserver_id>` |

---

## Безопасность

- Mosquitto слушает **только** `10.10.10.1:1883` (WireGuard интерфейс)
- Наружу порт **не открываем** — доступ только через WireGuard
- Аутентификация **выключена** по умолчанию (`allow_anonymous true`)
- При необходимости можно включить auth/ACL одним флагом в YAML (см. ниже)

---

## Предусловия

- Ubuntu 22.04+ (или другой Debian-based дистрибутив)
- Доступ root (`sudo`)
- IP `10.10.10.1` поднят на сервере (WireGuard интерфейс)
- .NET 8 SDK установлен ([инструкция](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu))

---

## Установка с нуля

```bash
# 1. Клонируем репозиторий
git clone https://github.com/zergont/MQTT_Server.git /opt/cg-mqtt-broker
cd /opt/cg-mqtt-broker

# 2. Копируем и редактируем конфиг
cp cg-broker.example.yaml cg-broker.yaml
nano cg-broker.yaml

# 3. Делаем скрипты исполняемыми и исправляем окончания строк
chmod +x scripts/*.sh
sed -i 's/\r$//' scripts/*.sh

# 4. Устанавливаем
sudo ./scripts/install.sh
```

Скрипт `install.sh` выполнит:

1. `apt install mosquitto mosquitto-clients`
2. `systemctl enable --now mosquitto`
3. Сборку и установку утилиты `cg-mosqctl`
4. Применение конфига из `cg-broker.yaml`
5. Smoke test

---

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

### Включение аутентификации (опционально, на будущее)

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

---

## Обновление конфигурации

```bash
cd /opt/cg-mqtt-broker
git pull
sudo ./scripts/apply.sh
```

Скрипт `apply.sh`:

1. Подтянет изменения из git
2. Валидирует конфиг
3. Создаст бэкап текущих файлов (`*.bak-YYYYMMDD-HHMMSS`)
4. Запишет новый конфиг в `/etc/mosquitto/conf.d/cg.conf`
5. Перезапустит mosquitto

---

## Утилита cg-mosqctl

.NET 8 консольное приложение (`src/CgMosqCtl/`). Читает `cg-broker.yaml`, генерирует и применяет конфиг Mosquitto.

### Команды

```bash
# Валидация конфига
cg-mosqctl check --config cg-broker.yaml

# Генерация файлов в директорию (без применения на сервер)
cg-mosqctl render --config cg-broker.yaml --out out/

# Применить конфиг + перезапустить mosquitto
sudo cg-mosqctl apply --config cg-broker.yaml

# Показать пример конфига
cg-mosqctl print-example-config
```

### Поведение при ошибках

- Если конфиг невалиден — **ничего не применяется**, выводится понятная ошибка
- Перед записью автоматически создаётся backup

### Локальная разработка (Windows / VS2022)

Утилиту можно собрать и запустить в Visual Studio 2022:

1. Открыть `cg-mqtt-broker.sln`
2. Выбрать профиль запуска: **check**, **render** или **print-example-config**
3. Нажать **F5** (или **Ctrl+F5**)

Либо из терминала VS:

```powershell
dotnet run --project src\CgMosqCtl\CgMosqCtl.csproj -- check --config cg-broker.yaml
```

> ⚠️ Команда `apply` на Windows не работает — она вызывает `systemctl`, который есть только на Linux.

---

## Smoke Test

После установки на сервере — проверяем что брокер работает.

### RAW телеметрия

**Терминал 1** — подписка:

```bash
mosquitto_sub -h 10.10.10.1 -t 'cg/v1/telemetry/SN/+' -v
```

**Терминал 2** — публикация:

```bash
mosquitto_pub -h 10.10.10.1 -t 'cg/v1/telemetry/SN/TEST_SN' -m 'hello'
```

Ожидаемый результат:

```
cg/v1/telemetry/SN/TEST_SN hello
```

### Decoded данные

**Терминал 1** — подписка на декодированные данные:

```bash
mosquitto_sub -h 10.10.10.1 -t 'cg/v1/decoded/SN/+' -v
```

**Терминал 2** — публикация:

```bash
mosquitto_pub -h 10.10.10.1 -t 'cg/v1/decoded/SN/TEST_SN' -m '{"power":42}'
```

Ожидаемый результат:

```
cg/v1/decoded/SN/TEST_SN {"power":42}
```

---

## Расположение файлов

| Файл | Путь |
|------|------|
| Конфиг (YAML) | `cg-broker.yaml` (в репозитории) |
| Конфиг Mosquitto | `/etc/mosquitto/conf.d/cg.conf` |
| Логи Mosquitto | `/var/log/mosquitto/mosquitto.log` |
| Persistence | `/var/lib/mosquitto/` |
| Утилита cg-mosqctl | `/usr/local/bin/cg-mosqctl` |

---

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

---

## Типовые ошибки и решения

### Mosquitto не запускается

```bash
mosquitto -c /etc/mosquitto/conf.d/cg.conf -t    # проверить синтаксис конфига
journalctl -u mosquitto -e --no-pager             # посмотреть журнал
```

### Ошибка "Address not available"

IP `10.10.10.1` не поднят на сервере. Проверьте WireGuard:

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
cg-mosqctl check --config cg-broker.yaml                        # валидация
cg-mosqctl render --config cg-broker.yaml --out /tmp/test-out   # посмотреть что сгенерируется
cat /tmp/test-out/cg-mosquitto.conf
```
