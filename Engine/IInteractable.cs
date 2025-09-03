using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public interface IInteractable
    {
        string Name { get; }
        List<string> GetAvailableActions(Player player);
        void ExecuteAction(Player player, string action);
    }
}
