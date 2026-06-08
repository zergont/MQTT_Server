// Copyright (c) 2026 ООО «НГ-ЭНЕРГОСЕРВИС». Все права защищены.
// Программный комплекс «Честная Генерация»
// Модуль MQTT-брокера
// Автор: Саввиди Александр Анатольевич | ИНН 4725009270
//
// Данное программное обеспечение является конфиденциальным.
// Несанкционированное копирование, распространение или использование
// без письменного разрешения правообладателя запрещено.

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
