// Copyright (c) 2026 ООО «НГ-ЭНЕРГОСЕРВИС». Все права защищены.
// Программный комплекс «Честная Генерация»
// Модуль MQTT-брокера
// Автор: Саввиди Александр Анатольевич | ИНН 4725009270
//
// Данное программное обеспечение является конфиденциальным.
// Несанкционированное копирование, распространение или использование
// без письменного разрешения правообладателя запрещено.

using CgMosqCtl;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

var command = args[0].ToLowerInvariant();

switch (command)
{
    case "check":
        return RunCheck(args);

    case "render":
        return RunRender(args);

    case "apply":
        return RunApply(args);

    case "print-example-config":
        Console.Write(ConfigLoader.GetExampleConfig());
        return 0;

    default:
        Log.Error($"Unknown command: {command}");
        PrintUsage();
        return 1;
}

static int RunCheck(string[] args)
{
    var configPath = GetOption(args, "--config");
    if (configPath == null)
    {
        Log.Error("Missing --config <path>");
        return 1;
    }

    try
    {
        var config = ConfigLoader.Load(configPath);
        var errors = ConfigLoader.Validate(config);
        if (errors.Count > 0)
        {
            Log.Error("Config validation failed:");
            foreach (var e in errors)
                Log.Error($"  - {e}");
            return 1;
        }

        Log.Info("Config is valid.");
        return 0;
    }
    catch (Exception ex)
    {
        Log.Error($"Failed to load config: {ex.Message}");
        return 1;
    }
}

static int RunRender(string[] args)
{
    var configPath = GetOption(args, "--config");
    var outDir = GetOption(args, "--out") ?? "out";

    if (configPath == null)
    {
        Log.Error("Missing --config <path>");
        return 1;
    }

    try
    {
        var config = ConfigLoader.Load(configPath);
        var errors = ConfigLoader.Validate(config);
        if (errors.Count > 0)
        {
            Log.Error("Config validation failed:");
            foreach (var e in errors)
                Log.Error($"  - {e}");
            return 1;
        }

        Applier.RenderToDirectory(config, outDir);
        Log.Info($"Rendered to: {outDir}");
        return 0;
    }
    catch (Exception ex)
    {
        Log.Error($"Failed: {ex.Message}");
        return 1;
    }
}

static int RunApply(string[] args)
{
    var configPath = GetOption(args, "--config");
    if (configPath == null)
    {
        Log.Error("Missing --config <path>");
        return 1;
    }

    try
    {
        var config = ConfigLoader.Load(configPath);
        var errors = ConfigLoader.Validate(config);
        if (errors.Count > 0)
        {
            Log.Error("Config validation failed — nothing applied:");
            foreach (var e in errors)
                Log.Error($"  - {e}");
            return 1;
        }

        Applier.Apply(config);
        Log.Info("Apply completed successfully.");
        return 0;
    }
    catch (Exception ex)
    {
        Log.Error($"Apply failed: {ex.Message}");
        return 1;
    }
}

static string? GetOption(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            return args[i + 1];
    }
    return null;
}

static void PrintUsage()
{
    Console.WriteLine("""
        cg-mosqctl — Mosquitto config manager for cg-mqtt-broker

        Usage:
          cg-mosqctl check  --config <path>            Validate YAML config
          cg-mosqctl render --config <path> [--out dir] Render config files to directory
          cg-mosqctl apply  --config <path>            Apply config and restart mosquitto
          cg-mosqctl print-example-config              Print example YAML config
        """);
}
