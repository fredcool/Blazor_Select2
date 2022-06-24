using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Select2.Models
{
    public class Select2Options
    {
        [JsonPropertyName("allowClear")]
        public bool AllowClear { get; set; } = false;

        [JsonPropertyName("closeOnSelect")]
        public bool CloseOnSelect { get; set; } = true;

        [JsonPropertyName("debug")]
        public bool Debug { get; set; } = false;

        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; } = false;

        [JsonPropertyName("dropdownAutoWidth")]
        public bool DropdownAutoWidth { get; set; } = false;

        //[JsonPropertyName("dropdownCssClass")]
        //public string DropdownCssClass { get; set; } = "";

        [JsonPropertyName("maximumInputLength")]
        public int MaximumInputLength { get; set; } = 0;

        [JsonPropertyName("maximumSelectionLength")]
        public int MaximumSelectionLength { get; set; } = 0;

        [JsonPropertyName("minimumInputLength")]
        public int MinimumInputLength { get; set; } = 0;

        [JsonPropertyName("minimumResultsForSearch")]
        public int MinimumResultsForSearch { get; set; } = 0;

        [JsonPropertyName("multiple")]
        public bool Multiple { get; set; } = false;

        [JsonPropertyName("placeholder")]
        public string Placeholder { get; set; } = "";

        [JsonPropertyName("selectionCssClass")]
        public string SelectionCssClass { get; set; } = "";

        [JsonPropertyName("selectOnClose")]
        public bool SelectOnClose { get; set; } = false;

        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "";

        [JsonPropertyName("width")]
        public int Width { get; set; } = 0;

        [JsonPropertyName("scrollAfterSelect")]
        public bool ScrollAfterSelect { get; set; } = false;
    }
}
