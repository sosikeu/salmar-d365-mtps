using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salmar_d365_mtps_convertor
{
    public class MTMessage
    {
        public string accountId { get; set; }
        public string messageId { get; set; }
        public string messageTime { get; set; }
        public string messageType { get; set; }
        public string messageStatus { get; set; }
        public string messageStatusText { get; set; }
        public List<MTMetadata> metadata { get; set; }
        public string MessageContent { get; set; }

    }
    public class MTMetadata
    {
        public string item1 { get; set; }
        public string item2 { get; set; }
    }
}
