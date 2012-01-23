using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IO = System.IO;
using bsn.GoldParser.Grammar;
using bsn.GoldParser.Semantic;
using bsn.GoldParser.Xml;
using bsn.GoldParser.Parser;
using Reflect = System.Reflection;
using Emit = System.Reflection.Emit;
using System.IO;

namespace Minim
{
    public class Parameter : Token
    {
        private Type type;
        private String name;
        private int position;

        [Rule(@"<Parameter> ::= Identifier Identifier")]
        public Parameter(Identifier type, Identifier name)
        {
            this.type = TypeChecker.ConvertStringToType(type.Value);
            this.name = name.Value;
        }

        public Parameter(Type type, String name)
        {
            this.type = type;
            this.name = name;
        }

        public String Name
        {
            get { return name; }
        }

        public Type Type
        {
            get { return type; }
        }

        public static Type[] ConvertSequenceToTypeArray(Sequence<Parameter> pars)
        {
            var l = new List<Type>();
            foreach (Parameter p in pars)
                l.Add(p.type);
            return l.ToArray();
        }

        public int Position
        {
            get { return position; }
            set { position = value; }
        }
    }
}
