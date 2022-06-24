using System.Collections.Generic;

namespace Select2.Models
{
    internal class Select2Response
    {
        public Select2Response()
        {
            Results = new List<Select2Group>();
            Pagination = new Select2Pagination(false);
        }

        public List<Select2Group> Results { get; set; }
        public Select2Pagination Pagination { get; }
    }
}
