using Engine.Audio;
using Engine.Core;
using System.Linq;
using System.Text;
using static Engine.Core.MessageSystem;

namespace Engine.UI
{
    public abstract class BaseScreen
    {
        protected readonly EnhancedBufferedRenderer _renderer;
        protected bool _needsRedraw = true;
        protected bool _needsFullRedraw = true; // Первая отрисовка всегда полная

        public virtual int Width => _renderer.Width;
        public virtual int Height => _renderer.Height;

        protected BaseScreen()
        {
            _renderer = GameServices.BufferedRenderer;

        }

        protected void RequestPartialRedraw()
        {
            _needsRedraw = true;
            _needsFullRedraw = false;
            ScreenManager.RequestPartialRedraw();
        }

        protected void RequestFullRedraw()
        {
            _needsRedraw = true;
            _needsFullRedraw = true;
            ScreenManager.RequestFullRedraw();
        }

        protected void ClearScreen()
        {
            _renderer.FillArea(0, 0, Width, Height, ' ', ConsoleColor.White, ConsoleColor.Black);
        }

        protected void ClearArea(int x, int y, int width, int height)
        {
            _renderer.FillArea(x, y, width, height, ' ', ConsoleColor.White, ConsoleColor.Black);
        }

        protected void RenderText(int x, int y, string text, ConsoleColor color = ConsoleColor.White)
        {
            // Обеспечиваем безопасное позиционирование
            x = Math.Max(0, Math.Min(x, Width - 1));
            y = Math.Max(0, Math.Min(y, Height - 1));

            if (!string.IsNullOrEmpty(text) && y < Height)
            {
                _renderer.Write(x, y, text, color);
            }
        }

        protected void RenderCenteredText(int y, string text, ConsoleColor color = ConsoleColor.White)
        {
            if (string.IsNullOrEmpty(text)) return;

            int x = (Width - text.Length) / 2;
            RenderText(Math.Max(0, x), Math.Max(0, y), text, color);
        }

        protected void RenderButton(int x, int y, string text, bool isSelected = false)
        {
            var bgColor = isSelected ? ConsoleColor.DarkGreen : ConsoleColor.DarkGray;
            var fgColor = isSelected ? ConsoleColor.White : ConsoleColor.Gray;

            // Ограничиваем размер кнопки шириной экрана
            int buttonWidth = Math.Min(text.Length + 4, Width - x);
            int buttonHeight = Math.Min(3, Height - y);

            _renderer.FillArea(x, y, buttonWidth, buttonHeight, ' ', fgColor, bgColor);
            RenderText(x + 2, y + 1, text, fgColor);
        }

        public abstract void Render();

        public abstract void HandleInput(ConsoleKeyInfo keyInfo);

        protected virtual bool HandleCommonInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    // звук перемещения
                    SoundSystem.Play("Assets/Sounds/menu_move.wav", 0.5f);
                    return true;

                case ConsoleKey.Enter:
                case ConsoleKey.E:
                    // звук выбора
                    SoundSystem.Play("Assets/Sounds/menu_select.wav", 0.7f);
                    return true;

                case ConsoleKey.Escape:
                    // можно тоже добавить звук отмены, если хочешь:
                    // SoundSystem.Play("Assets/Sounds/menu_back.wav", 0.5f);
                    return true;
            }
            return false;
        }

        protected List<string> WrapText(string text, int maxWidth)
        {
            var lines = new List<string>();

            if (string.IsNullOrEmpty(text))
                return lines;

            // Учитываем границы экрана
            maxWidth = Math.Min(maxWidth, Width - 4);

            var words = text.Split(' ');
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 > maxWidth)
                {
                    lines.Add(currentLine.ToString().Trim());
                    currentLine.Clear();
                }

                if (currentLine.Length > 0)
                    currentLine.Append(' ');

                currentLine.Append(word);
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString().Trim());

            return lines;
        }

        protected void RenderFooter(string instructions, int yOffset = 0)
        {
            int y = Math.Max(0, Math.Min(Height - 3 + yOffset, Height - 1));
            int width = Math.Min(Width, Console.WindowWidth);

            // Очищаем область футера
            _renderer.FillArea(0, y, width, 3, ' ', ConsoleColor.White, ConsoleColor.Black);

            _renderer.Write(0, y, new string('═', width), ConsoleColor.Gray);
            _renderer.Write(2, y + 1, instructions, ConsoleColor.DarkGray);
        }

        protected void RenderHeader(string title, int yOffset = 0, ConsoleColor color = ConsoleColor.Yellow)
        {
            int y = Math.Max(0, yOffset);
            int width = Math.Min(Width, Console.WindowWidth);

            // Очищаем область заголовка
            _renderer.FillArea(0, y, width, 3, ' ', ConsoleColor.White, ConsoleColor.Black);

            _renderer.Write(0, y, new string('═', width), ConsoleColor.Gray);
            RenderCenteredText(y + 1, title, color);
            _renderer.Write(0, y + 2, new string('═', width), ConsoleColor.Gray);
        }

        // Рендер наложения, которое показывается поверх любого экрана (сообщения системы сообщений и т.п.)
        // Поместите этот метод туда, где у вас сейчас рисуются сообщения (BaseScreen / ScreenManager).
        public virtual void RenderOverlay()
        {
            var msgsEnum = MessageSystem.Messages;
            if (msgsEnum == null) return;

            // Приводим к списку для индексированного доступа
            var msgs = msgsEnum as System.Collections.Generic.IList<MessageSystem.MessageData> ?? msgsEnum.ToList();
            if (msgs.Count == 0) return;

            // Параметры позиционирования — под хедером
            int headerHeight = 3;        // подстрой под ваш Header
            int topY = headerHeight + 1; // первая строка для сообщений (под хедером)
            int paddingRight = 2;

            int screenWidth = Console.WindowWidth;
            int screenHeight = Console.WindowHeight;

            int maxLines = Math.Max(0, screenHeight - topY - 2);
            if (maxLines <= 0) return;

            // Выбираем последние N сообщений (в порядке от старого к новому в исходном массиве)
            int startIndex = Math.Max(0, msgs.Count - maxLines);
            var lastMsgs = msgs.Skip(startIndex).ToList(); // порядок: [старое ... новое]

            // Мы хотим рисовать новые сверху, старые снизу.
            // Поэтому перебираем lastMsgs в обратном порядке (новое сначала),
            // а рисуем вниз, начиная с topY.
            int lineY = topY;
            for (int i = lastMsgs.Count - 1; i >= 0; i--)
            {
                var m = lastMsgs[i]; // i = last -> самое новое, i = 0 -> самое старое
                if (m == null || string.IsNullOrEmpty(m.Text)) continue;

                string text = m.Text;
                int maxTextWidth = screenWidth - paddingRight - 1;
                if (maxTextWidth <= 0) break;
                if (text.Length > maxTextWidth) text = text.Substring(0, maxTextWidth);

                ConsoleColor color = GetColorByAlpha(m.Alpha);

                int drawX = Math.Max(0, screenWidth - paddingRight - text.Length);

                try
                {
                    _renderer.Write(drawX, lineY, text, color);
                }
                catch
                {
                    // безопасно пропускаем ошибки рендера
                }

                lineY++;
                if (lineY >= screenHeight - 1) break;
            }
        }
        private ConsoleColor GetColorByAlpha(double alpha)
        {
            if (alpha >= 0.90) return ConsoleColor.White;
            if (alpha >= 0.65) return ConsoleColor.Gray;
            if (alpha >= 0.35) return ConsoleColor.Gray;
            if (alpha > 0.10) return ConsoleColor.DarkGray;
            return ConsoleColor.DarkGray;
        }

        public virtual void Update()
        {
            if (_needsRedraw)
            {
                Render();
                _needsRedraw = false;
                _needsFullRedraw = false;
            }
        }

        public void RequestRedraw()
        {
            _needsRedraw = true;
        }
    }
}