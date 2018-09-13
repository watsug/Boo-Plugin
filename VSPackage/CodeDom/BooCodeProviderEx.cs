using Boo.Lang.CodeDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.CodeDom;
using System.IO;
using Boo.Lang.Compiler.IO;

namespace Boo.Lang.CodeDom
{
    class BooCodeProviderEx : BooCodeProvider
    {
        protected string[] _references;
        public IEnumerable<string> References
        {
            set { _references = value.ToArray(); }
        }

        public BooCodeProviderEx(string[] references)
        {
            _ext = "boo";
            _references = references;
        }

        public override TypeConverter GetConverter(Type type)
        {
            return base.GetConverter(type);
        }

        public CodeCompileUnit Parse(IEnumerable<TextReader> inputs, IEnumerable<string> names)
        {
            BooCodeParser booCodeParser = new BooCodeParser(this._references);
            return booCodeParser.Parse(inputs, names);
        }
    }
}
