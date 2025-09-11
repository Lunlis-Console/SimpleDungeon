// ItemComponentJsonConverter.cs — безопасная версия
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonEditor
{
    /// <summary>
    /// Безопасный JsonConverter для полиморфной десериализации IItemComponent.
    /// Идея: один раз (лениво) построить "безопасный" словарь componentName -> concrete Type,
    /// а затем использовать его при десериализации. Не сканируем все сборки без фильтра.
    /// </summary>
    public class ItemComponentJsonConverter : JsonConverter
    {
        // Кеш маппинга (ключ — нижний регистр)
        private static ConcurrentDictionary<string, Type> _map = null;
        private static object _initLock = new object();
        private static Type _ifaceType = null;
        private static bool _initializationAttempted = false;

        // Доп.: можно передать словарь маппинга в конструкторе (опционально)
        private readonly IDictionary<string, Type> _providedMap;

        public ItemComponentJsonConverter() { }

        public ItemComponentJsonConverter(IDictionary<string, Type> explicitMap)
        {
            _providedMap = explicitMap;
            if (_providedMap != null)
            {
                _map = new ConcurrentDictionary<string, Type>(_providedMap, StringComparer.OrdinalIgnoreCase);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            if (objectType == null) return false;

            // Быстрая эвристика: если это интерфейс с именем IItemComponent
            if (objectType.IsInterface && objectType.Name.Equals("IItemComponent", StringComparison.OrdinalIgnoreCase))
                return true;

            // Если мы смогли найти интерфейсный тип — проверяем присваиваемость
            var iface = EnsureInterfaceType();
            if (iface != null)
            {
                return iface.IsAssignableFrom(objectType);
            }

            // В противном случае — конвертер не уверен, но не будет ложно срабатывать
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Загружаем token
            var token = JToken.Load(reader);
            if (token == null || token.Type != JTokenType.Object)
            {
                // fallback
                return token?.ToObject(objectType, serializer);
            }

            var jo = (JObject)token;

            // Получаем имя компонента из поля ComponentType / Type / componentType (толерантно)
            var compProp = jo.Properties()
                .FirstOrDefault(p => string.Equals(p.Name, "ComponentType", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(p.Name, "Type", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(p.Name, "componentType", StringComparison.OrdinalIgnoreCase));

            string compName = compProp?.Value?.ToString();

            // Инициализируем карту (если ещё не)
            EnsureMapInitialized();

            // Если был передан явный map — используем его
            if (_map == null || _map.Count == 0)
            {
                // Если мапинга нет, пробуем простую десериализацию в JObject
                // чтобы избежать падений — возвращаем JObject или выбрасываем контролируемую ошибку
                // (но лучше — вернуть попытку десериализации в objectType)
                try
                {
                    return jo.ToObject(objectType, serializer);
                }
                catch (Exception ex)
                {
                    throw new JsonSerializationException($"Не удалось десериализовать компонент (нет маппинга): {ex.Message}", ex);
                }
            }

            Type concrete = null;

            if (!string.IsNullOrWhiteSpace(compName))
            {
                // Пробуем прямой поиск (ключ хранится в нижнем регистре)
                _map.TryGetValue(compName.ToLowerInvariant(), out concrete);

                if (concrete == null)
                {
                    // Попытка: найти по точному имени типа (без "Component" и т.д.)
                    var keys = _map.Keys.ToList();
                    var lower = compName.ToLowerInvariant();
                    // exact contains match
                    var k = keys.FirstOrDefault(kk => kk.Equals(lower, StringComparison.OrdinalIgnoreCase));
                    if (k != null) _map.TryGetValue(k, out concrete);

                    // contains
                    if (concrete == null)
                    {
                        var candidate = keys.FirstOrDefault(kk => kk.Contains(lower));
                        if (candidate != null) _map.TryGetValue(candidate, out concrete);
                    }

                    // try with "component" suffix
                    if (concrete == null)
                    {
                        var withSuffix = lower;
                        if (!withSuffix.EndsWith("component")) withSuffix += "component";
                        var cand2 = keys.FirstOrDefault(kk => kk.Equals(withSuffix, StringComparison.OrdinalIgnoreCase));
                        if (cand2 != null) _map.TryGetValue(cand2, out concrete);
                    }
                }
            }

            // Если не нашли, попытаемся подобрать тип по эвристике (первый подходящий)
            if (concrete == null)
            {
                concrete = _map.Values.FirstOrDefault();
            }

            if (concrete == null)
            {
                throw new JsonSerializationException($"Не найден конкретный тип для компонента (componentType='{compName}'). Проверьте регистрацию реализаций IItemComponent.");
            }

            // И наконец десериализуем в конкретный тип (делегируем стандартному сериализатору).
            try
            {
                return jo.ToObject(concrete, serializer);
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Ошибка при десериализации компонента в тип {concrete.FullName}: {ex.Message}", ex);
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Делегируем сериализацию
            serializer.Serialize(writer, value);
        }

        // ------------- ВСПОМОГАТЕЛЬНЫЕ --------------

        private void EnsureMapInitialized()
        {
            if (_map != null && _map.Count > 0) return;
            lock (_initLock)
            {
                if (_map != null && _map.Count > 0) return;
                if (_providedMap != null)
                {
                    _map = new ConcurrentDictionary<string, Type>(_providedMap, StringComparer.OrdinalIgnoreCase);
                    return;
                }

                // Попробуем собрать карту безопасно
                try
                {
                    _map = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

                    // Найдём интерфейс IItemComponent один раз
                    var iface = EnsureInterfaceType();
                    if (iface == null)
                    {
                        // не нашли интерфейс — оставим пустую карту, чтобы не ломать приложение
                        _map = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                        return;
                    }

                    // Выбираем сборки для сканирования: исключим системные и динамические
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a =>
                        {
                            try
                            {
                                if (a.IsDynamic) return false;
                                var loc = a.Location;
                                if (string.IsNullOrEmpty(loc)) return false; // часто динамические / in-memory
                                var name = a.GetName().Name ?? "";
                                // исключаем системные/корневые пакеты
                                var badPrefixes = new[] { "System", "Microsoft", "mscorlib", "Newtonsoft", "netstandard", "Accessibility", "WindowsBase", "PresentationFramework" };
                                if (badPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase))) return false;
                                return true;
                            }
                            catch
                            {
                                return false;
                            }
                        })
                        .ToList();

                    // Если нет подходящих сборок — расширяем список аккуратно (без вызова GetTypes на всем)
                    if (!assemblies.Any())
                    {
                        assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).ToList();
                    }

                    foreach (var asm in assemblies)
                    {
                        Type[] types = null;
                        try
                        {
                            // безопасный вызов GetExportedTypes, если не работает — GetTypes внутри try
                            types = asm.GetExportedTypes();
                        }
                        catch
                        {
                            try { types = asm.GetTypes(); } catch { types = Array.Empty<Type>(); }
                        }

                        foreach (var t in types)
                        {
                            try
                            {
                                if (t == null) continue;
                                if (t.IsAbstract || t.IsInterface) continue;
                                if (!iface.IsAssignableFrom(t)) continue;

                                var key = t.Name.ToLowerInvariant();
                                if (!_map.ContainsKey(key))
                                    _map[key] = t;

                                // ещё варианты ключей без суффиксов
                                var keyNoComp = key.EndsWith("component") ? key.Substring(0, key.Length - "component".Length) : null;
                                if (!string.IsNullOrWhiteSpace(keyNoComp))
                                {
                                    if (!_map.ContainsKey(keyNoComp))
                                        _map[keyNoComp] = t;
                                }

                                var keyNoItemComp = key.EndsWith("itemcomponent") ? key.Substring(0, key.Length - "itemcomponent".Length) : null;
                                if (!string.IsNullOrWhiteSpace(keyNoItemComp))
                                {
                                    if (!_map.ContainsKey(keyNoItemComp))
                                        _map[keyNoItemComp] = t;
                                }
                            }
                            catch
                            {
                                // пропустить проблемный тип
                            }
                        }
                    }
                }
                catch
                {
                    // в случае любой непредвиденной проблемы — не падаем, оставляем пустую карту
                    _map = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        private Type EnsureInterfaceType()
        {
            if (_ifaceType != null) return _ifaceType;
            lock (_initLock)
            {
                if (_ifaceType != null) return _ifaceType;
                // Ищем тип интерфейса по именам в загруженных сборках (без агрессивной загрузки)
                try
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (asm.IsDynamic) continue;
                            var types = asm.GetTypes();
                            foreach (var t in types)
                            {
                                if (t.IsInterface && t.Name.Equals("IItemComponent", StringComparison.OrdinalIgnoreCase))
                                {
                                    _ifaceType = t;
                                    return _ifaceType;
                                }
                            }
                        }
                        catch
                        {
                            // пропускаем сборку если не удалось перечислить типы
                            continue;
                        }
                    }
                }
                catch
                {
                    // ignore
                }
                return _ifaceType;
            }
        }
    }
}
