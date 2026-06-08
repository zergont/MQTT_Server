// Copyright (c) 2026 ООО «НГ-ЭНЕРГОСЕРВИС». Все права защищены.
// Программный комплекс «Честная Генерация»
// Модуль MQTT-брокера
// Автор: Саввиди Александр Анатольевич | ИНН 4725009270
//
// Данное программное обеспечение является конфиденциальным.
// Несанкционированное копирование, распространение или использование
// без письменного разрешения правообладателя запрещено.

namespace CgMosqCtl;

public class BrokerConfig
{
    public ListenerConfig Listener { get; set; } = new();
    public PathsConfig Paths { get; set; } = new();
    public PersistenceConfig Persistence { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public SecurityConfig Security { get; set; } = new();
}

public class ListenerConfig
{
    public string BindIp { get; set; } = "10.10.10.1";
    public int Port { get; set; } = 1883;
}

public class PathsConfig
{
    public string MosquittoConf { get; set; } = "/etc/mosquitto/conf.d/cg.conf";
    public string Log { get; set; } = "/var/log/mosquitto/mosquitto.log";
}

public class PersistenceConfig
{
    public bool Enabled { get; set; } = true;
    public string Location { get; set; } = "/var/lib/mosquitto/";
}

public class LoggingConfig
{
    public bool ConnectionMessages { get; set; } = true;
}

public class SecurityConfig
{
    public bool AuthEnabled { get; set; } = false;
    public bool AllowAnonymous { get; set; } = true;
    public string PasswdPath { get; set; } = "/etc/mosquitto/passwd";
    public string AclPath { get; set; } = "/etc/mosquitto/acl";
    public List<UserEntry> Users { get; set; } = new();
}

public class UserEntry
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public List<string> Topics { get; set; } = new();
}
