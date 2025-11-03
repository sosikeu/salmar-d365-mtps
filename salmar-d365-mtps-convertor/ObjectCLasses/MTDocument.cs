using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class MTDocument
    {
        public List<MTPostedEntry> postedEntries { get; set; }
        public List<MTFakturaXML> fakturaXML { get; set; }
    }
}
