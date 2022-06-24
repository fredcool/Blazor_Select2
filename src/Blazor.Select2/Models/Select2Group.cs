using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Select2.Models
{
    public class Select2Group : Select2ItemBase
    {
        public Select2Group(string groupName) : base(groupName)
        {

        }

        public List<Select2Item> Children { get; set; } = new List<Select2Item>();
    }
}
