using System;
using System.Linq;
using System.Collections.Generic;
using Engine.Dialogue;
using Engine.Entities;

namespace Engine.UI
{
    public class DialogueScreen : BaseScreen, IDialogueUI
    {
        private readonly NPC _npc;
        private Dialogue.DialogueSystem.DialogueNode _currentNode;
        private int _selectedIndex;
        private Player _player;

        public Dialogue.DialogueSystem.DialogueOption SelectedOption { get; private set; }

        // Передаём player (если у тебя есть глобальный доступ — можно передавать null и брать внутри)
        public DialogueScreen(NPC npc, Dialogue.DialogueSystem.DialogueNode startNode, Player player = null)
        {
            _npc = npc;
            _currentNode = startNode;
            _selectedIndex = 0;
            _player = player ?? TryGetPlayerFromWorld();
        }

        private Player TryGetPlayerFromWorld()
        {
            try
            {
                // Если у тебя есть глобальные синглтоновские провайдеры - поправь этот вызов.
                // Пример: return JsonWorldRepository.Instance.Player или Engine.World.WorldState.Instance.Player
                // Оставляем null-safe - если не доступен, проверки условий вернут false.
                return null;
            }
            catch { return null; }
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader(_npc?.Name ?? "NPC");
            RenderDialogueText();
            RenderOptions();
            RenderFooter("W/S - выбор │ E - ответить │ Q - выйти");

            _renderer.EndFrame();
        }

        private void RenderDialogueText()
        {
            int y = 4;
            var lines = WrapText(_currentNode.Text, Width - 4);

            foreach (var line in lines)
            {
                if (y < Height - 10)
                {
                    _renderer.Write(2, y, line, ConsoleColor.White);
                    y++;
                }
            }

            _renderer.Write(0, y + 1, new string('─', Width), ConsoleColor.DarkGray);
        }

        private void RenderOptions()
        {
            // отладочная печать — покажем сколько опций и их данные
            DebugConsole.Log($"[RenderOptions] CurrentNodeText='{_currentNode?.Text?.Replace("\\n", " ")}' OptionsCount={_currentNode?.Options?.Count ?? 0}");

            if (_currentNode?.Options != null)
            {
                for (int ii = 0; ii < _currentNode.Options.Count; ii++)
                {
                    var o = _currentNode.Options[ii];
                    var nextExists = o.NextNode != null ? "yes" : "no";
                    DebugConsole.Log($"[RenderOptions] Option[{ii}] Text='{o.Text}' IsAvailable={o.IsAvailable} IsVisited={o.IsVisited} NextExists={nextExists} Condition='{o.Condition}' Action='{o.Action}'");
                }
            }


            int startY = Height - 8;

            // Обновляем доступность опций, проверяя condition на текущем игроке
            foreach (var opt in _currentNode.Options)
            {
                try
                {
                    opt.IsAvailable = opt.EvaluateCondition(_player);
                }
                catch
                {
                    opt.IsAvailable = true; // fallback
                }
            }

            var availableOptions = _currentNode.Options.Where(o => o.IsAvailable).ToList();

            // если нет доступных опций — отрисуй уведомление
            if (availableOptions.Count == 0)
            {
                _renderer.Write(2, startY, "(Нет доступных ответов)", ConsoleColor.DarkGray);
                return;
            }

            _renderer.Write(2, startY - 2, "ВЫБЕРИТЕ ОТВЕТ:", ConsoleColor.Cyan);

            for (int i = 0; i < availableOptions.Count; i++)
            {
                bool isSelected = i == _selectedIndex;
                bool isVisited = availableOptions[i].IsVisited;
                int y = startY + i;

                var text = availableOptions[i].Text ?? "(пустой ответ)";

                if (isSelected)
                {
                    _renderer.Write(2, y, "►");
                    _renderer.Write(4, y, text);
                }
                else
                {
                    if (isVisited)
                        _renderer.Write(4, y, text, ConsoleColor.DarkGray);
                    else
                        _renderer.Write(4, y, text, ConsoleColor.Gray);
                }
            }
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            var availableOptions = _currentNode.Options.Where(o => o.IsAvailable).ToList();

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    if (availableOptions.Count > 0)
                    {
                        _selectedIndex = Math.Max(0, _selectedIndex - 1);
                        RequestPartialRedraw();
                    }
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    if (availableOptions.Count > 0)
                    {
                        _selectedIndex = Math.Min(availableOptions.Count - 1, _selectedIndex + 1);
                        RequestPartialRedraw();
                    }
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    if (availableOptions.Count > 0)
                    {
                        var selectedOption = availableOptions[_selectedIndex];
                        SelectedOption = selectedOption;

                        // Отмечаем как посещённую (локально)
                        selectedOption.IsVisited = true;

                        // Выполняем выбор через ExecuteSelection, передаём игрока и this (IDialogueUI).
                        try
                        {
                            selectedOption.ExecuteSelection(_player, this);
                        }
                        catch (Exception ex)
                        {
                            // Если ExecuteSelection у тебя реализует Action через DialogueActions — логируем
                            DebugConsole.Log("Ошибка при выполнении опции диалога: " + ex.Message);
                            // fallback: если NextNode задан — просто переходим
                            if (selectedOption.NextNode != null)
                            {
                                SetCurrentNode(selectedOption.NextNode);
                            }
                            else
                            {
                                CloseDialogue();
                            }
                        }

                        // Если ExecuteSelection не сменил ноду, но NextNode задан — применим переход
                        // (ExecuteSelection обычно либо вызовет ui.SetCurrentNode, либо закроет диалог)
                        RequestFullRedraw();
                    }
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    CloseDialogue();
                    break;
            }
        }

        // IDialogueUI implementation — эти методы вызываются из ExecuteSelection
        public void SetCurrentNode(Dialogue.DialogueSystem.DialogueNode node)
        {
            if (node == null) return;
            _currentNode = node;
            _selectedIndex = 0;
            _currentNode.OnEnter?.Invoke();
            RequestFullRedraw();
        }

        public void CloseDialogue()
        {
            // Закрываем экран диалога
            ScreenManager.PopScreen();
        }
    }
}
