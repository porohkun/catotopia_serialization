using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Catotopia.Shared;
using Newtonsoft.Json;

namespace Catotopia.Defs
{
    public partial class MainDef : Def
    {
        [JsonIgnore]
        public int IntProp => _intField;
        [JsonProperty]
        private System.Int32 _intField;

        public string StringProp1 { get; private set; }

        [JsonProperty("stringProp_2")]
        private string _stringField;

        [JsonProperty]
        private int[,] _ints;

        [JsonIgnore]
        public IEnumerable<SideDef> Sides => _sides;

        [JsonProperty]
        private SideDef[] _sides;


        //[JsonConstructor]
        //private MainDef(int intField, string stringField, SideDef[] sides)
        //{
        //    _intField = intField;
        //    _stringField = stringField;
        //    //_sides = sides;
        //    _sides = Array.AsReadOnly(sides);
        //    var g = _sides[7];
        //}

        //public MainDef(string json, IResourcesContainer container)
        //{
        //    container.
        //}

        //public MainDef() : base() { }
    }
}
