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
    [Terminal("Identifier")]
    public class Identifier : Expression
    {
        String name;

        public Identifier(String name)
        {
            this.name = name;
        }

        public String Value { get { return name; } }

        public override void Push(Emit.ILGenerator ilg)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class Expression : Token
    {
        public abstract void Push(Emit.ILGenerator ilg);
    }

    [Terminal("StringLiteral")]
    public class StringLiteral : Expression
    {
        private String s;

        public StringLiteral(String s)
        {
            this.s = s.Substring(1, s.Length - 2);
        }

        public override void Push(Emit.ILGenerator ilg)
        {
            ilg.Emit(Emit.OpCodes.Ldstr, s);
        }
    }
}
