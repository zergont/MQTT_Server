namespace CgMosqCtl;

public static class Log
{
    public static void Info(string message) =>
        Console.WriteLine($"[INFO]  {message}");

    public static void Warn(string message) =>
        Console.WriteLine($"[WARN]  {message}");

    public static void Error(string message) =>
        Console.Error.WriteLine($"[ERROR] {message}");
}
