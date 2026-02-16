using System.Net;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CgMosqCtl;

public static class ConfigLoader
{
    public static BrokerConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Config file not found: {path}");

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var config = deserializer.Deserialize<BrokerConfig>(yaml)
            ?? throw new InvalidOperationException("Failed to parse YAML config (result is null).");

        return config;
    }

    public static List<string> Validate(BrokerConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Listener.BindIp))
            errors.Add("listener.bind_ip is empty.");
        else if (!IPAddress.TryParse(config.Listener.BindIp, out _))
            errors.Add($"listener.bind_ip is not a valid IP address: '{config.Listener.BindIp}'.");

        if (config.Listener.Port < 1 || config.Listener.Port > 65535)
            errors.Add($"listener.port must be 1..65535, got {config.Listener.Port}.");

        if (string.IsNullOrWhiteSpace(config.Paths.MosquittoConf))
            errors.Add("paths.mosquitto_conf is empty.");

        if (string.IsNullOrWhiteSpace(config.Paths.Log))
            errors.Add("paths.log is empty.");

        if (config.Persistence.Enabled && string.IsNullOrWhiteSpace(config.Persistence.Location))
            errors.Add("persistence.location is empty but persistence is enabled.");

        if (config.Security.AuthEnabled)
        {
            if (string.IsNullOrWhiteSpace(config.Security.PasswdPath))
                errors.Add("security.passwd_path is empty but auth is enabled.");
            if (string.IsNullOrWhiteSpace(config.Security.AclPath))
                errors.Add("security.acl_path is empty but auth is enabled.");
        }

        return errors;
    }

    public static string GetExampleConfig()
    {
        return """
            # cg-broker.yaml — единый конфиг для Mosquitto MQTT broker

            listener:
              bind_ip: "10.10.10.1"
              port: 1883

            paths:
              mosquitto_conf: "/etc/mosquitto/conf.d/cg.conf"
              log: "/var/log/mosquitto/mosquitto.log"

            persistence:
              enabled: true
              location: "/var/lib/mosquitto/"

            logging:
              connection_messages: true

            security:
              auth_enabled: false
              allow_anonymous: true
              passwd_path: "/etc/mosquitto/passwd"
              acl_path: "/etc/mosquitto/acl"
              users: []
            """.Replace("            ", "");
    }
}
