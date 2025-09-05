namespace Engine
{
    public class InventoryStateCache
    {
        private InventoryRenderData _lastState;
        private int _lastWindowWidth;
        private int _lastWindowHeight;

        public bool NeedsFullRedraw(InventoryRenderData newState)
        {
            return _lastState == null ||
                   _lastWindowWidth != Console.WindowWidth ||
                   _lastWindowHeight != Console.WindowHeight ||
                   !StatesEqual(_lastState, newState);
        }

        public void UpdateState(InventoryRenderData state)
        {
            _lastState = CloneState(state);
            _lastWindowWidth = Console.WindowWidth;
            _lastWindowHeight = Console.WindowHeight;
        }

        private bool StatesEqual(InventoryRenderData state1, InventoryRenderData state2)
        {
            // Простая проверка - можно расширить при необходимости
            return state1.SelectedIndex == state2.SelectedIndex &&
                   state1.Items.Count == state2.Items.Count &&
                   state1.Player.Gold == state2.Player.Gold;
        }

        private InventoryRenderData CloneState(InventoryRenderData state)
        {
            return new InventoryRenderData
            {
                Items = new List<object>(state.Items),
                SelectedIndex = state.SelectedIndex,
                Player = state.Player,
                Title = state.Title
            };
        }
    }
}
