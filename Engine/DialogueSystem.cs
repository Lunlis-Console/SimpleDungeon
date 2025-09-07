using System.Text;

namespace Engine
{
    public class DialogueSystem
    {
        public class DialogueNode
        {
            public string Text { get; set; }
            public List<DialogueOption> Options { get; set; }
            public Action OnEnter { get; set; }

            public DialogueNode(string text, Action onEnter = null)
            {
                Text = text;
                Options = new List<DialogueOption>();
                OnEnter = onEnter;
            }
        }

        public class DialogueOption
        {
            public string Text { get; set; }
            public DialogueNode NextNode { get; set; }
            public Action OnSelect { get; set; }
            public bool IsAvailable { get; set; } = true;
            public bool IsVisited { get; set; } = false; // Новое свойство

            public DialogueOption(string text, DialogueNode nextNode = null, Action onSelect = null)
            {
                Text = text;
                NextNode = nextNode;
                OnSelect = onSelect;
            }
        }

    }
}