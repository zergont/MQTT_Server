// Copyright (c) 2026 ООО «НГ-ЭНЕРГОСЕРВИС». Все права защищены.
// Программный комплекс «Честная Генерация»
// Модуль MQTT-брокера
// Автор: Саввиди Александр Анатольевич | ИНН 4725009270
//
// Данное программное обеспечение является конфиденциальным.
// Несанкционированное копирование, распространение или использование
// без письменного разрешения правообладателя запрещено.

using System.Diagnostics;

namespace CgMosqCtl;

public static class Applier
{
    public static void Apply(BrokerConfig config)
    {
        var confContent = Renderer.RenderMosquittoConf(config);
        var confPath = config.Paths.MosquittoConf;

        BackupIfExists(confPath);
        WriteFileSafe(confPath, confContent);
        Log.Info($"Written: {confPath}");

        if (config.Security.AuthEnabled)
        {
            // ACL
            var aclContent = Renderer.RenderAcl(config);
            BackupIfExists(config.Security.AclPath);
            WriteFileSafe(config.Security.AclPath, aclContent);
            Log.Info($"Written: {config.Security.AclPath}");

            // passwd (plain text, then call mosquitto_passwd to hash)
            var usersContent = Renderer.RenderUsersFile(config);
            BackupIfExists(config.Security.PasswdPath);
            WriteFileSafe(config.Security.PasswdPath, usersContent);
            Log.Info($"Written: {config.Security.PasswdPath}");

            HashPasswords(config.Security.PasswdPath);
        }

        RestartMosquitto();
    }

    public static void RenderToDirectory(BrokerConfig config, string outDir)
    {
        Directory.CreateDirectory(outDir);

        var confContent = Renderer.RenderMosquittoConf(config);
        var confPath = Path.Combine(outDir, "cg-mosquitto.conf");
        File.WriteAllText(confPath, confContent);
        Log.Info($"Rendered: {confPath}");

        if (config.Security.AuthEnabled)
        {
            var aclPath = Path.Combine(outDir, "acl");
            File.WriteAllText(aclPath, Renderer.RenderAcl(config));
            Log.Info($"Rendered: {aclPath}");

            var usersPath = Path.Combine(outDir, "users.txt");
            File.WriteAllText(usersPath, Renderer.RenderUsersFile(config));
            Log.Info($"Rendered: {usersPath}");
        }
    }

    private static void BackupIfExists(string path)
    {
        if (!File.Exists(path))
            return;

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var bakPath = $"{path}.bak-{stamp}";
        File.Copy(path, bakPath, overwrite: true);
        Log.Info($"Backup: {path} → {bakPath}");
    }

    private static void WriteFileSafe(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(path, content);
    }

    private static void HashPasswords(string passwdPath)
    {
        Log.Info("Hashing passwords with mosquitto_passwd...");
        var psi = new ProcessStartInfo
        {
            FileName = "mosquitto_passwd",
            Arguments = $"-U \"{passwdPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var proc = Process.Start(psi);
        if (proc == null)
        {
            Log.Error("Failed to start mosquitto_passwd.");
            return;
        }

        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            var err = proc.StandardError.ReadToEnd();
            Log.Error($"mosquitto_passwd failed (exit {proc.ExitCode}): {err}");
        }
        else
        {
            Log.Info("Passwords hashed successfully.");
        }
    }

    private static void RestartMosquitto()
    {
        Log.Info("Restarting mosquitto...");
        var psi = new ProcessStartInfo
        {
            FileName = "systemctl",
            Arguments = "restart mosquitto",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var proc = Process.Start(psi);
        if (proc == null)
        {
            Log.Error("Failed to start systemctl.");
            return;
        }

        proc.WaitForExit();
        if (proc.ExitCode != 0)
        {
            var err = proc.StandardError.ReadToEnd();
            Log.Error($"systemctl restart mosquitto failed (exit {proc.ExitCode}): {err}");
        }
        else
        {
            Log.Info("mosquitto restarted successfully.");
        }
    }
}
