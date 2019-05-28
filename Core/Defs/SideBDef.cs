using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Catotopia.Defs
{
    public partial class SideBDef : SideDef
    {
        [JsonIgnore]
        public string StringProp => _stringField;

        [JsonProperty]
        private string _stringField;
    }
}
