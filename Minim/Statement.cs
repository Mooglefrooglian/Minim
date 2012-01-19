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
    public abstract class Statement : Token
    {
        public abstract void GenerateCode(Emit.ILGenerator ilg);
    }

    public class CallStatement : Statement
    {
        private Sequence<Expression> alist;
        private String funcName;
        [Rule(@"<Statement> ::= Identifier ~'(' <ArgumentList> ~')' ~<nl>")]
        public CallStatement(Identifier funcName, Sequence<Expression> alist)
        {
            this.funcName = funcName.Value;
            this.alist = alist;
        }
        [Rule(@"<Statement> ::= Identifier ~<nl>")]
        public CallStatement(Identifier funcName) : this(funcName, new Sequence<Expression>()) { }
        public override void GenerateCode(Emit.ILGenerator ilg)
        {
            foreach (Expression e in alist)
                e.Push(ilg);

            ilg.Emit(Emit.OpCodes.Call, Function.Get(funcName).MethodBuilder);
        }
    }

    public class PrintStatement : Statement
    {
        private Expression e;
        [Rule(@"<Statement> ::= ~print ~'(' <Expression> ~')' ~<nl>")]
        public PrintStatement(Expression e) { this.e = e; }
        public override void GenerateCode(Emit.ILGenerator ilg)
        {
            e.Push(ilg);
            ilg.Emit(Emit.OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
        }
    }
    
}
