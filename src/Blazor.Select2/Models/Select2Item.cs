using System.Collections.Generic;

namespace Select2.Models
{
    public class Select2Item : Select2ItemBase
    {
        public Select2Item(string id, string text, bool disabled, string groupName) : base(text)
        {
            Id = id;
            Disabled = disabled;
            GroupName = groupName;
        }

        public string Id { get; }
        public bool Disabled { get; }
        public bool Selected { get; set; }
        public string Html { get; set; }
        public string GroupName { get; set; }
    }
}
