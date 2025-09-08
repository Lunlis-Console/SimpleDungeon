using Engine.Entities;

namespace Engine
{
    public interface IInteractable
    {
        string Name { get; }
        List<string> GetAvailableActions(Player player);
        void ExecuteAction(Player player, string action);
    }
}
