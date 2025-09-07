// Program.cs — простой json-tool с поддержкой вызова DataExporter (русская версия сообщений и комментариев)
// Поместите этот файл в проект tools/json-tool и соберите (dotnet build).

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Generic;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Использование: dotnet run -- generate|merge|export <путь-к-assembly.dll> <папка-вывода>");
            Console.WriteLine("  generate — сгенерировать шаблоны (не перезаписывает существующие файлы)");
            Console.WriteLine("  merge    — добавить новые поля в существующие JSON (сохраняет .bak)");
            Console.WriteLine("  export   — попытаться вызвать DataExporter из сборки (если есть), иначе ничего не делает");
            return 1;
        }

        string cmd = args[0].ToLowerInvariant();
        string asmPath = args[1];
        string outDir = args[2];

        if (!File.Exists(asmPath))
        {
            Console.WriteLine($"Сборка не найдена: {asmPath}");
            return 2;
        }

        Directory.CreateDirectory(outDir);

        Assembly asm;
        try { asm = Assembly.LoadFrom(asmPath); }
        catch (Exception ex) { Console.WriteLine($"Ошибка при загрузке сборки: {ex.Message}"); return 3; }

        // Команда export — особая: пытаемся найти DataExporter и вызвать метод экспорта
        if (cmd == "export")
        {
            TryRunDataExporter(asm, outDir);
            return 0;
        }

        var suffixes = new[] { "Data", "Dto", "Model", "Info", "Record", "State" };
        var types = asm.GetTypes().Where(t => (t.IsClass || t.IsValueType) && suffixes.Any(s => t.Name.EndsWith(s, StringComparison.OrdinalIgnoreCase))).ToArray();
        if (types.Length == 0) { Console.WriteLine("В сборке не найдены типы, оканчивающиеся на *Data."); return 0; }

        var options = new JsonSerializerOptions { WriteIndented = true };

        foreach (var t in types)
        {
            Console.WriteLine($"Обрабатываю {t.FullName}");
            var template = BuildTemplateNode(t, new HashSet<Type>());

            string filePath = Path.Combine(outDir, t.Name + ".json");

            if (cmd == "generate")
            {
                if (!File.Exists(filePath))
                {
                    File.WriteAllText(filePath, template.ToJsonString(options));
                    Console.WriteLine($"  -> сгенерирован {filePath}");
                }
                else
                {
                    Console.WriteLine($"  -> файл уже существует, пропускаю (используйте merge для добавления новых полей): {filePath}");
                }
            }
            else if (cmd == "merge")
            {
                if (File.Exists(filePath))
                {
                    var existingText = File.ReadAllText(filePath);
                    JsonNode existing;
                    try { existing = JsonNode.Parse(existingText) ?? new JsonObject(); }
                    catch (Exception ex) { Console.WriteLine($"  ! Ошибка парсинга существующего JSON: {ex.Message}"); continue; }

                    var merged = MergeJson(template, existing);
                    BackupFile(filePath);
                    File.WriteAllText(filePath, merged.ToJsonString(options));
                    Console.WriteLine($"  -> объединено и сохранено {filePath}");
                }
                else
                {
                    File.WriteAllText(filePath, template.ToJsonString(options));
                    Console.WriteLine($"  -> создан новый {filePath}");
                }
            }
            else
            {
                Console.WriteLine($"Неизвестная команда: {cmd}");
                return 4;
            }
        }

        Console.WriteLine("Готово.");
        return 0;
    }

    // Попытаться найти класс DataExporter и вызвать публичный статический метод экспорта
    static void TryRunDataExporter(Assembly asm, string outDir)
    {
        try
        {
            var exporterType = asm.GetTypes().FirstOrDefault(t => t.Name.Equals("DataExporter", StringComparison.OrdinalIgnoreCase));
            if (exporterType == null)
            {
                Console.WriteLine("DataExporter не найден в сборке.");
                return;
            }

            // Ищем возможные публичные статические методы: ExportGameDataToJson, Export, ExportToJson
            var methodNames = new[] { "ExportGameDataToJson", "ExportToJson", "Export", "Run" };
            MethodInfo method = null;
            foreach (var name in methodNames)
            {
                method = exporterType.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
                if (method != null) break;
            }

            if (method == null)
            {
                // Попробуем взять любой публичный статический метод без параметров
                method = exporterType.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(m => m.GetParameters().Length == 0);
            }

            if (method == null)
            {
                Console.WriteLine("Не найден подходящий статический метод для вызова у DataExporter.");
                return;
            }

            var pars = method.GetParameters();
            object result = null;
            if (pars.Length == 1 && pars[0].ParameterType == typeof(string))
            {
                Console.WriteLine($"Вызов {method.Name}(" + outDir + ")");
                result = method.Invoke(null, new object[] { outDir });
            }
            else if (pars.Length == 0)
            {
                Console.WriteLine($"Вызов {method.Name}()");
                result = method.Invoke(null, null);
            }
            else
            {
                Console.WriteLine("Найден метод с неподдерживаемой сигнатурой. Ожидается метод без параметров или с единственным string (путь). Пропускаю.");
                return;
            }

            Console.WriteLine("DataExporter успешно вызван (если метод возвратил исключений).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при вызове DataExporter: {ex.Message}");
        }
    }

    static void BackupFile(string path)
    {
        try
        {
            string bak = path + ".bak." + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            File.Copy(path, bak);
        }
        catch { /* игнорируем ошибки бэкапа */ }
    }

    static JsonNode BuildTemplateNode(Type t, HashSet<Type> visited)
    {
        if (visited.Contains(t)) return new JsonObject();
        if (t == typeof(string)) return JsonValue.Create("");
        if (t.IsPrimitive) return JsonValue.Create(Activator.CreateInstance(t));
        if (t.IsEnum) return JsonValue.Create(Enum.GetNames(t).FirstOrDefault() ?? "0");

        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t.IsGenericType)
        {
            var elemType = t.GetGenericArguments()[0];
            var arr = new JsonArray();
            arr.Add(BuildTemplateNode(elemType, new HashSet<Type>(visited)));
            return arr;
        }

        visited.Add(t);
        var obj = new JsonObject();
        var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead);

        foreach (var p in props)
        {
            var pType = p.PropertyType;
            JsonNode child = null;
            if (pType == typeof(string)) child = JsonValue.Create("");
            else if (pType.IsPrimitive) child = JsonValue.Create(GetDefaultValue(pType));
            else if (pType.IsEnum) child = JsonValue.Create(Enum.GetNames(pType).FirstOrDefault() ?? "0");
            else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(pType) && pType.IsGenericType)
            {
                var et = pType.GetGenericArguments()[0];
                var a = new JsonArray();
                a.Add(BuildTemplateNode(et, new HashSet<Type>(visited)));
                child = a;
            }
            else
            {
                child = BuildTemplateNode(pType, new HashSet<Type>(visited));
            }

            obj[p.Name] = child;
        }

        visited.Remove(t);
        return obj;
    }

    static object GetDefaultValue(Type t)
    {
        if (t == typeof(bool)) return false;
        if (t == typeof(char)) return '\0';
        if (t == typeof(byte) || t == typeof(sbyte) || t == typeof(short) || t == typeof(ushort) ||
            t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(ulong))
            return 0;
        if (t == typeof(float) || t == typeof(double) || t == typeof(decimal)) return 0;
        return null;
    }

    static JsonNode MergeJson(JsonNode template, JsonNode existing)
    {
        if (template is JsonObject tObj && existing is JsonObject eObj)
        {
            var result = new JsonObject();
            foreach (var kv in tObj)
            {
                if (eObj.TryGetPropertyValue(kv.Key, out var eVal))
                {
                    if (kv.Value is JsonObject && eVal is JsonObject)
                        result[kv.Key] = MergeJson(kv.Value!, eVal!);
                    else
                        result[kv.Key] = eVal;
                }
                else
                {
                    result[kv.Key] = kv.Value;
                }
            }
            foreach (var kv in eObj)
            {
                if (!tObj.ContainsKey(kv.Key))
                {
                    result[$"_deprecated_{kv.Key}"] = kv.Value;
                }
            }
            return result;
        }

        return existing ?? template;
    }
}

/* README (короткий):

  tools/json-tool — простая утилита для генерации и мерджа JSON-шаблонов из DTO.

  Сборка и запуск:
    dotnet build
    dotnet run --project tools/json-tool/json-tool.csproj -- generate <путь-к-YourGameAssembly.dll> <out-folder>
    dotnet run --project tools/json-tool/json-tool.csproj -- merge    <путь-к-YourGameAssembly.dll> <out-folder>
    dotnet run --project tools/json-tool/json-tool.csproj -- export   <путь-к-YourGameAssembly.dll> <out-folder>

  Пояснения:
    generate — создаёт файлы TypeName.json для всех классов, имя которых оканчивается на Data. Не перезаписывает существующие.
    merge    — добавляет новые поля в существующие JSON, сохраняет .bak копию и помечает удалённые поля как _deprecated_...
    export   — пытается найти в сборке тип DataExporter и вызвать у него статический метод (ExportGameDataToJson / Export / ExportToJson / Run). Этот режим предназначен для вызова вашего существующего экспортёра.

*/
