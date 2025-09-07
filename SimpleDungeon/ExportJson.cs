using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

public static class ReflectionHelpers
{
    // Безопасный GetTypes: при ошибке возвращаем только удачно загруженные типы
    public static IEnumerable<Type> GetTypesSafe(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // ex.Types может содержать null, поэтому фильтруем и приводим к Type
            return ex.Types?.Where(t => t != null).Select(t => t!) ?? Enumerable.Empty<Type>();
        }
    }
}

public static class JsonExporter
{
    /// <summary>
    /// Попытаться выполнить экспорт JSON при наличии аргумента --export-json <outFolder>.
    /// Возвращает:
    ///  -1 : экспорт не запрошен (не найден нужный аргумент)
    ///   0 : успех
    ///   2..5 : различные ошибки
    /// </summary>
    public static int TryExportJson(string[] args)
    {
        if (args == null) return -1;
        if (args.Length >= 2 && args[0].Equals("--export-json", StringComparison.OrdinalIgnoreCase))
        {
            string outFolder = args[1];
            Directory.CreateDirectory(outFolder);
            try
            {
                // Ищем тип DataExporter среди загруженных сборок текущего домена
                var exporterType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypesSafe())
                    .FirstOrDefault(t => string.Equals(t.Name, "DataExporter", StringComparison.OrdinalIgnoreCase));

                if (exporterType == null)
                {
                    Console.WriteLine("DataExporter не найден в загруженных сборках текущего процесса.");
                    return 3;
                }

                // Ищем метод: сначала строго ExportGameDataToJson(string) или ExportGameDataToJson()
                var method = exporterType.GetMethod("ExportGameDataToJson", BindingFlags.Public | BindingFlags.Static)
                             ?? exporterType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                    .FirstOrDefault(m => m.Name.IndexOf("Export", StringComparison.OrdinalIgnoreCase) >= 0
                                                        && m.GetParameters().Length <= 1);

                if (method == null)
                {
                    Console.WriteLine("Не найден подходящий статический метод экспорта в DataExporter.");
                    return 4;
                }

                var pars = method.GetParameters();
                if (pars.Length == 1 && pars[0].ParameterType == typeof(string))
                {
                    method.Invoke(null, new object[] { outFolder });
                }
                else if (pars.Length == 0)
                {
                    method.Invoke(null, null);
                }
                else
                {
                    Console.WriteLine("Найдена неподдерживаемая сигнатура метода экспорта. Ожидается () или (string).");
                    return 5;
                }

                Console.WriteLine("Экспорт JSON завершён успешно.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка при экспорте JSON: " + ex);
                return 2;
            }
        }

        return -1;
    }
}
