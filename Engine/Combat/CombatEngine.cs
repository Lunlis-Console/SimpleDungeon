using System;
using System.Collections.Generic;

namespace Engine.Combat
{
    /// <summary>
    /// CombatEngine с 1-секундной паузой перед и после атаки противника.
    /// - стартовая CurrentSpeed = Agility
    /// - оба накапливают скорость
    /// - при достижении 100 сторона делает действие
    /// - ход (turn) завершается только тогда, когда оба сходили хотя бы раз в этом turn'е
    /// - перед атакой монстра 1s пауза, затем мгновенная атака, затем 1s пауза
    /// - во время пауз ввод игрока блокируется, накопление приостанавливается
    /// </summary>
    public class CombatEngine
    {
        public dynamic Player { get; private set; }
        public dynamic Monster { get; private set; }
        public dynamic WorldRepository { get; private set; }

        private readonly List<string> combatLog = new List<string>();

        public int CurrentTurn { get; private set; } = 0;
        public bool IsCombatOver { get; private set; } = false;

        // Игрок может выполнять действие когда true
        public bool IsPlayerTurnReady { get; private set; } = false;

        // Фаза действия монстра: None / PreAction(wait before) / PostAction(wait after)
        private enum EnemyPhase { None, PreAction, PostAction }
        private EnemyPhase _enemyPhase = EnemyPhase.None;
        private DateTime _enemyPhaseUntilUtc = DateTime.MinValue;

        // Длительности пауз (по умолчанию 1 секунда)
        public TimeSpan EnemyDelayBefore { get; set; } = TimeSpan.FromSeconds(0.5);
        public TimeSpan EnemyDelayAfter { get; set; } = TimeSpan.FromSeconds(0.5);

        // Можно накапливать скорость (обычно оба true)
        private bool _canPlayerFill = true;
        private bool _canMonsterFill = true;

        // Для реализации правила "ход завершается, когда оба сходили хотя бы раз в текущем turn'e"
        private bool _hasPlayerActedThisTurn = false;
        private bool _hasMonsterActedThisTurn = false;

        // Внутренний флаг — уведомили игрока о готовности (чтобы не спамить лог)
        private bool _playerTurnNotified = false;

        // Флаг: была ли уже объявлена "=== ВАШ ХОД ===" в текущем turn'e (чтобы не дублировать в лог)
        private bool _playerAnnouncedThisTurn = false;


        private readonly Random _rng = new Random();

        // Публично: монстр в любой фазе => считается действующим (ввод блокирован)
        public bool IsEnemyActing => _enemyPhase != EnemyPhase.None;

        // коэффициент скорости заполнения (по умолчанию 0.5 — заполняется в 2 раза медленнее)
        public double SpeedFillScale { get; set; } = 0.08;

        // публичный флаг — рендер может показать, что монстр в предзадержке (готовится)
        public bool IsMonsterPreparing => _enemyPhase == EnemyPhase.PreAction;


        #region Конструкторы и инициализация

        public CombatEngine(object playerObj, object monsterObj, object worldRepository)
        {
            if (playerObj == null) throw new ArgumentNullException(nameof(playerObj));
            if (monsterObj == null) throw new ArgumentNullException(nameof(monsterObj));

            Player = playerObj;
            Monster = monsterObj;
            WorldRepository = worldRepository;

            InitializeCommon();
        }

        public CombatEngine(object playerObj, object monsterObj) : this(playerObj, monsterObj, null) { }

        private void InitializeCommon()
        {
            _playerAnnouncedThisTurn = false;
            EnsureNumericProperty(Player, "Agility", 0);
            EnsureNumericProperty(Monster, "Agility", 0);

            // стартовая скорость = ловкость
            int pAgi = Math.Clamp(GetIntSafe(Player, new[] { "Agility" }), 0, 100);
            int mAgi = Math.Clamp(GetIntSafe(Monster, new[] { "Agility" }), 0, 100);
            SetIntSafe(Player, "CurrentSpeed", pAgi);
            SetIntSafe(Monster, "CurrentSpeed", mAgi);

            EnsureNumericProperty(Player, "CurrentHP", GetIntSafe(Player, new[] { "MaximumHP", "MaxHP", "MaxHealth" }));
            EnsureNumericProperty(Monster, "CurrentHP", GetIntSafe(Monster, new[] { "MaximumHP", "MaxHP", "MaxHealth" }));

            _canPlayerFill = true;
            _canMonsterFill = true;
            _enemyPhase = EnemyPhase.None;
            _enemyPhaseUntilUtc = DateTime.MinValue;

            _hasPlayerActedThisTurn = false;
            _hasMonsterActedThisTurn = false;
            _playerTurnNotified = false;

            AddToCombatLog($"Бой начат: {GetName(Player)} против {GetName(Monster)}");
            CurrentTurn = 1;
            AddToCombatLog($"=== ХОД {CurrentTurn} ===");
        }

        #endregion

        #region Update / накопление скорости / фазы монстра

        /// <summary>
        /// Вызывается каждый тик; управляет накоплением, запуском действий и таймерами пауз.
        /// </summary>
        public void UpdateFrame()
        {
            if (IsCombatOver) return;

            DateTime now = DateTime.UtcNow;

            // Если монстр в пред/пост фазе — проверяем окончание паузы и трансферы состояний
            if (_enemyPhase == EnemyPhase.PreAction)
            {
                // Во время паузы накопление и ввод блокированы
                _canPlayerFill = false;
                _canMonsterFill = false;
                if (now < _enemyPhaseUntilUtc)
                {
                    // ещё ждём
                    return;
                }

                // Предзадержка окончена — выполняем атаку
                _enemyPhase = EnemyPhase.None; // временно выключаем, выполнение может сменить на PostAction
                DoMonsterAttackLogic(); // наносит урон и помечает _hasMonsterActedThisTurn

                if (IsCombatOver)
                {
                    // если бой закончился — остаёмся в None
                    _enemyPhase = EnemyPhase.None;
                    _enemyPhaseUntilUtc = DateTime.MinValue;
                    return;
                }

                // Устанавливаем постзадержку
                _enemyPhase = EnemyPhase.PostAction;
                _enemyPhaseUntilUtc = now + EnemyDelayAfter;
                // блок накопления и ввод остаются заблокированными до конца постзадержки
                return;
            }

            if (_enemyPhase == EnemyPhase.PostAction)
            {
                // По правилам: пока идёт постзадержка — блокируем накопление и ввод
                _canPlayerFill = false;
                _canMonsterFill = false;
                if (now < _enemyPhaseUntilUtc)
                {
                    return;
                }

                // Постзадержка закончилась — разрешаем накопление и проверяем завершение хода
                _enemyPhase = EnemyPhase.None;
                _enemyPhaseUntilUtc = DateTime.MinValue;

                // После постзадержки даём возможность накапливать
                _canPlayerFill = true;
                _canMonsterFill = true;

                // Если оба уже сходили — завершить turn
                if (_hasPlayerActedThisTurn && _hasMonsterActedThisTurn && !IsCombatOver)
                {
                    EndTurnAndReset();
                }
                return;
            }

            // Если игрок ожидает своего хода — мы не форсируем его; накопление в это время приостановлено
            if (IsPlayerTurnReady)
            {
                _canPlayerFill = false;
                _canMonsterFill = false;
                return;
            }

            // Накопление скорости у тех, кому разрешено
            if (_canPlayerFill)
            {
                int cur = GetIntSafe(Player, new[] { "CurrentSpeed" });
                int agi = GetIntSafe(Player, new[] { "Agility" });
                int inc = CalculateSpeedIncrement(cur, agi);
                SetIntSafe(Player, "CurrentSpeed", cur + inc);
            }

            if (_canMonsterFill)
            {
                int cur = GetIntSafe(Monster, new[] { "CurrentSpeed" });
                int agi = GetIntSafe(Monster, new[] { "Agility" });
                int inc = CalculateSpeedIncrement(cur, agi);
                SetIntSafe(Monster, "CurrentSpeed", cur + inc);
            }

            ClampSpeed(Player);
            ClampSpeed(Monster);

            // Проверяем, кто достиг 100
            bool playerReady = GetIntSafe(Player, new[] { "CurrentSpeed" }) >= 100;
            bool monsterReady = GetIntSafe(Monster, new[] { "CurrentSpeed" }) >= 100;

            if (playerReady && monsterReady)
            {
                // Одновременное достижение — приоритет по Agility (если равны — игрок)
                int pA = GetIntSafe(Player, new[] { "Agility" });
                int mA = GetIntSafe(Monster, new[] { "Agility" });

                if (pA >= mA)
                {
                    if (!_playerTurnNotified)
                        NotifyPlayerTurn();
                    return;
                }
                else
                {
                    // Запускаем предзадержку монстра
                    StartMonsterPreActionDelay();
                    return;
                }
            }

            if (playerReady)
            {
                if (!_playerTurnNotified)
                    NotifyPlayerTurn();
                return;
            }

            if (monsterReady)
            {
                StartMonsterPreActionDelay();
                return;
            }
        }

        private int CalculateSpeedIncrement(int currentSpeed, int agility)
        {
            // старый multiplier:
            double multiplier = 1.0 + currentSpeed / 100.0;
            double raw = agility * multiplier * SpeedFillScale;
            int inc = (int)Math.Ceiling(raw);
            return Math.Max(1, inc);
        }

        private void ClampSpeed(dynamic who)
        {
            int s = GetIntSafe(who, new[] { "CurrentSpeed" });
            if (s > 100) SetIntSafe(who, "CurrentSpeed", 100);
            if (s < 0) SetIntSafe(who, "CurrentSpeed", 0);
        }

        private void NotifyPlayerTurn()
        {
            IsPlayerTurnReady = true;
            _playerTurnNotified = true;

            // Логируем "=== ВАШ ХОД ===" только один раз за текущий turn
            if (!_playerAnnouncedThisTurn)
            {
                AddToCombatLog("=== ВАШ ХОД ===");
                _playerAnnouncedThisTurn = true;
            }

            // при уведомлении игрока накопление останавливается (в UpdateFrame это учитывается)
        }


        #endregion

        #region Монстр: предзадержка / действие / постзадержка

        /// <summary>
        /// Устанавливает фазу предзадержки перед атакой монстра (пауза до 1s),
        /// блокируя накопление и ввод. Дальше в UpdateFrame произойдёт переход в выполнение атаки.
        /// </summary>
        private void StartMonsterPreActionDelay()
        {
            if (IsCombatOver) return;
            if (_enemyPhase != EnemyPhase.None) return;

            // НЕ добавляем запись в лог — вместо этого выставляем флаг подготовки,
            // чтобы рендер показывал отдельный индикатор.
            _enemyPhase = EnemyPhase.PreAction;
            _enemyPhaseUntilUtc = DateTime.UtcNow + EnemyDelayBefore;

            // блокируем накопление и ввод пока длится предзадержка
            _canPlayerFill = false;
            _canMonsterFill = false;
        }

        /// <summary>
        /// Немедленная логика удара монстра (выполняется после предзадержки).
        /// </summary>
        private void DoMonsterAttackLogic()
        {
            // Потратил заряд
            SetIntSafe(Monster, "CurrentSpeed", 0);

            int mAtk = GetIntSafe(Monster, new[] { "Attack", "Power" });
            int pDef = GetIntSafe(Player, new[] { "Defense", "Defence" });

            int dmg = Math.Max(1, mAtk - pDef);
            int tempDef = GetIntSafe(Player, new[] { "TemporaryDefense" });
            if (tempDef > 0) dmg = Math.Max(0, dmg - tempDef);

            int pHp = GetIntSafe(Player, new[] { "CurrentHP" });
            pHp = Math.Max(0, pHp - dmg);
            SetIntSafe(Player, "CurrentHP", pHp);

            AddToCombatLog($"{GetName(Monster)} атакует {GetName(Player)} и наносит {dmg} урона.");

            // Отметка, что монстр сходил в этом turn'e
            _hasMonsterActedThisTurn = true;

            // уменьшение временных эффектов игрока
            int remTurns = GetIntSafe(Player, new[] { "TemporaryDefenseTurns" });
            if (remTurns > 0)
            {
                remTurns--;
                SetIntSafe(Player, "TemporaryDefenseTurns", remTurns);
                if (remTurns <= 0) SetIntSafe(Player, "TemporaryDefense", 0);
            }

            CheckEndCombat();
            // После этого UpdateFrame установит PostAction фазу (см. выше)
        }

        #endregion

        #region Действия игрока (вызовы из UI)

        public void ProcessPlayerAction_Attack()
        {
            if (IsCombatOver || !IsPlayerTurnReady || IsEnemyActing) return;

            PlayerAction_Attack();
            PostPlayerActionCleanup();
        }

        public void ProcessPlayerAction_Defend()
        {
            if (IsCombatOver || !IsPlayerTurnReady || IsEnemyActing) return;

            PlayerAction_Defend();
            PostPlayerActionCleanup();
        }

        public void ProcessPlayerAction_Spell()
        {
            if (IsCombatOver || !IsPlayerTurnReady || IsEnemyActing) return;

            PlayerAction_Spell();
            PostPlayerActionCleanup();
        }

        public void ProcessPlayerAction_Flee()
        {
            if (IsCombatOver || !IsPlayerTurnReady || IsEnemyActing) return;

            PlayerAction_Flee();
            PostPlayerActionCleanup();
        }

        private void PostPlayerActionCleanup()
        {
            // Игрок потратил заряд
            SetIntSafe(Player, "CurrentSpeed", 0);
            IsPlayerTurnReady = false;
            _playerTurnNotified = false;

            // помечаем, что игрок сходил в текущем turn'e
            _hasPlayerActedThisTurn = true;

            // После действия игрока оба могут снова накапливать
            _canPlayerFill = true;
            _canMonsterFill = true;

            CheckEndCombat();

            // Если оба уже сходили — завершаем turn
            if (_hasPlayerActedThisTurn && _hasMonsterActedThisTurn && !IsCombatOver)
            {
                EndTurnAndReset();
            }
        }

        #endregion

        #region Действия игрока: реализации (простые)

        private void PlayerAction_Attack()
        {
            int pAtk = GetIntSafe(Player, new[] { "Attack", "Power", "Str" });
            int mDef = GetIntSafe(Monster, new[] { "Defense", "Defence" });

            int dmg = Math.Max(1, pAtk - mDef);
            int mHp = GetIntSafe(Monster, new[] { "CurrentHP" });
            mHp = Math.Max(0, mHp - dmg);
            SetIntSafe(Monster, "CurrentHP", mHp);

            AddToCombatLog($"{GetName(Player)} атакует {GetName(Monster)} и наносит {dmg} урона.");
            CheckEndCombat();
        }

        private void PlayerAction_Defend()
        {
            int buff = Math.Max(1, GetIntSafe(Player, new[] { "Agility" }) / 4);
            SetIntSafe(Player, "TemporaryDefense", buff);
            SetIntSafe(Player, "TemporaryDefenseTurns", 1);

            AddToCombatLog($"{GetName(Player)} встал в защиту (+{buff} к защите на 1 ход).");
            CheckEndCombat();
        }

        private void PlayerAction_Spell()
        {
            int power = GetIntSafe(Player, new[] { "MagicPower", "SpellPower" });
            if (power == 0) power = GetIntSafe(Player, new[] { "Agility" }) * 2;
            int mHp = GetIntSafe(Monster, new[] { "CurrentHP" });
            int dmg = Math.Max(1, power);
            mHp = Math.Max(0, mHp - dmg);
            SetIntSafe(Monster, "CurrentHP", mHp);

            AddToCombatLog($"{GetName(Player)} использует заклинание и наносит {dmg} магического урона по {GetName(Monster)}.");
            CheckEndCombat();
        }

        private void PlayerAction_Flee()
        {
            int pAgi = GetIntSafe(Player, new[] { "Agility" });
            int mAgi = GetIntSafe(Monster, new[] { "Agility" });
            int baseChance = 50 + (pAgi - mAgi) * 2;
            baseChance = Math.Clamp(baseChance, 5, 95);
            int roll = _rng.Next(0, 100);
            if (roll < baseChance)
            {
                AddToCombatLog($"{GetName(Player)} успешно убежал из боя!");
                IsCombatOver = true;
            }
            else
            {
                AddToCombatLog($"{GetName(Player)} попытался убежать, но не смог!");
            }
            CheckEndCombat();
        }

        #endregion

        #region Завершение хода

        private void EndTurnAndReset()
        {
            _hasPlayerActedThisTurn = false;
            _hasMonsterActedThisTurn = false;

            _canPlayerFill = true;
            _canMonsterFill = true;

            // сбрасываем флаги объявлений к следующему turn'у
            _playerTurnNotified = false;
            _playerAnnouncedThisTurn = false;

            CurrentTurn++;
            AddToCombatLog($"=== ХОД {CurrentTurn} ===");
        }


        #endregion

        #region Проверки окончания и лог

        private void CheckEndCombat()
        {
            if (IsCombatOver) return;

            int pHp = GetIntSafe(Player, new[] { "CurrentHP" });
            int mHp = GetIntSafe(Monster, new[] { "CurrentHP" });

            if (pHp <= 0 && mHp <= 0)
            {
                AddToCombatLog("Оба персонажа пали. Ничья.");
                IsCombatOver = true;
            }
            else if (mHp <= 0)
            {
                AddToCombatLog($"{GetName(Monster)} повержен! Вы победили.");
                IsCombatOver = true;
                
                // Уведомляем QuestManager о убийстве монстра
                try
                {
                    var questManager = Engine.Core.GameServices.QuestManager;
                    if (questManager != null)
                    {
                        questManager.OnMonsterKilled(Monster, Player);
                        DebugConsole.Log($"CombatEngine: Уведомлен QuestManager об убийстве монстра: {GetName(Monster)}");
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"CombatEngine: Ошибка уведомления QuestManager: {ex.Message}");
                }
            }
            else if (pHp <= 0)
            {
                AddToCombatLog($"{GetName(Player)} пал в бою. Поражение.");
                IsCombatOver = true;
            }
        }

        private void AddToCombatLog(string line)
        {
            combatLog.Add(line);
            if (combatLog.Count > 1000) combatLog.RemoveRange(0, combatLog.Count - 1000);
        }

        public List<string> GetCombatLog()
        {
            return new List<string>(combatLog);
        }

        #endregion

        #region Reflection-like helpers

        private void EnsureNumericProperty(dynamic obj, string prop, int defaultValue)
        {
            try
            {
                var t = obj.GetType();
                var p = t.GetProperty(prop);
                if (p != null)
                {
                    var v = p.GetValue(obj);
                    if (v == null) p.SetValue(obj, defaultValue);
                    return;
                }
                var f = t.GetField(prop);
                if (f != null)
                {
                    var v = f.GetValue(obj);
                    if (v == null) f.SetValue(obj, defaultValue);
                    return;
                }
            }
            catch { }
        }

        private int GetIntSafe(dynamic obj, string[] names)
        {
            if (obj == null) return 0;
            var t = obj.GetType();
            foreach (var name in names)
            {
                try
                {
                    var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (p != null)
                    {
                        var v = p.GetValue(obj);
                        if (v is int i) return i;
                        if (v is long l) return (int)l;
                        if (v != null) { try { return Convert.ToInt32(v); } catch { } }
                    }
                    var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (f != null)
                    {
                        var v = f.GetValue(obj);
                        if (v is int i) return i;
                        if (v is long l) return (int)l;
                        if (v != null) { try { return Convert.ToInt32(v); } catch { } }
                    }
                }
                catch { }
            }
            return 0;
        }

        private void SetIntSafe(dynamic obj, string name, int value)
        {
            if (obj == null) return;
            var t = obj.GetType();
            try
            {
                var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (p != null && (p.PropertyType == typeof(int) || p.PropertyType == typeof(long)))
                {
                    if (p.PropertyType == typeof(long)) p.SetValue(obj, (long)value);
                    else p.SetValue(obj, value);
                    return;
                }
                var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (f != null)
                {
                    if (f.FieldType == typeof(long)) f.SetValue(obj, (long)value);
                    else f.SetValue(obj, value);
                    return;
                }
            }
            catch { }
        }

        private string GetName(dynamic obj)
        {
            var n = GetStringSafe(obj, new[] { "Name", "name" });
            if (string.IsNullOrEmpty(n)) return "Неизвестный";
            return n;
        }

        private string GetStringSafe(dynamic obj, string[] names)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var name in names)
            {
                try
                {
                    var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (p != null && p.PropertyType == typeof(string)) return p.GetValue(obj) as string;
                    var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (f != null && f.FieldType == typeof(string)) return f.GetValue(obj) as string;
                }
                catch { }
            }
            return null;
        }

        #endregion
    }
}
