using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class EquipmentItem
    {
        public Equipment Details { get; set; }
        public int Quantity { get; set; }

        public EquipmentItem(Equipment details, int quantity)
        {
            Details = details;
            Quantity = quantity;
        }
    }
}
