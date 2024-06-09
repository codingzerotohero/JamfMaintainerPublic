using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JamfMaintainer
{

    public class JsonRoot
    {
        public JsonGroup[] JsonGroups { get; set; }
    }

    public class JsonGroup
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string OwnerID { get; set; }
        public string[] GrepCode { get; set; }
        public string TimeframeBegin { get; set; }
        public string TimeframeBeginSpecified { get; set; }
        public string TimeframeEnd { get; set; }
        public string TimeframeEndSpecified { get; set; }
        public string SourceSystem { get; set; }
        public string RoleType { get; set; }
    }

}
