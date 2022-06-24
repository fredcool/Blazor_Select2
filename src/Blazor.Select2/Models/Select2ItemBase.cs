using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Select2.Models
{
    public abstract class Select2ItemBase
    {
        public Select2ItemBase(string text)
        {
            Text = text;
        }
        
        public string Text { get; set; }
    }
}
