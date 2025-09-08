using Engine.Entities;
using System.Xml.Linq;

namespace Engine.UI
{
    public class DialogueScreen : BaseScreen
    {
        private readonly NPC _npc;
        private DialogueSystem.DialogueNode _currentNode;
        private int _selectedIndex;

        public DialogueSystem.DialogueOption SelectedOption { get; private set; }

        public DialogueScreen(NPC npc, DialogueSystem.DialogueNode startNode)
        {
            _npc = npc;
            _currentNode = startNode;
            _selectedIndex = 0;
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader(_npc.Name);
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
            int startY = Height - 8;
            var availableOptions = _currentNode.Options.Where(o => o.IsAvailable).ToList();

            _renderer.Write(2, startY - 2, "ВЫБЕРИТЕ ОТВЕТ:", ConsoleColor.Cyan);

            for (int i = 0; i < availableOptions.Count; i++)
            {
                bool isSelected = i == _selectedIndex;
                bool isVisited = availableOptions[i].IsVisited; // Проверяем пройдена ли опция
                int y = startY + i;

                if (isSelected)
                {
                    _renderer.Write(2, y, "►");

                    if (isVisited)
                    {
                        _renderer.Write(4, y, $"{availableOptions[i].Text}");
                    }
                    else
                    {
                        _renderer.Write(4, y, availableOptions[i].Text);
                    }
                }
                else
                {
                    if (isVisited)
                    {
                        _renderer.Write(4, y, availableOptions[i].Text, ConsoleColor.DarkGray);
                        // Добавить эффект полупрозрачности через специальный метод, если есть
                    }
                    else
                    {
                        _renderer.Write(4, y, availableOptions[i].Text, ConsoleColor.Gray);
                    }
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
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(availableOptions.Count - 1, _selectedIndex + 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    if (availableOptions.Count > 0)
                    {
                        var selectedOption = availableOptions[_selectedIndex];

                        // Отмечаем опцию как пройденную
                        selectedOption.IsVisited = true;

                        selectedOption.OnSelect?.Invoke();

                        if (selectedOption.NextNode != null)
                        {
                            _currentNode = selectedOption.NextNode;
                            _currentNode.OnEnter?.Invoke();
                            _selectedIndex = 0;
                            RequestFullRedraw();
                        }
                        else
                        {
                            ScreenManager.PopScreen();
                        }
                    }
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }
    }
}