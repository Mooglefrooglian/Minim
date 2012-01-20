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
        public abstract void GenerateCode(Emit.ILGenerator ilg, ExecutionContext ec);
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


        public override void GenerateCode(Emit.ILGenerator ilg, ExecutionContext ec)
        {
            var f = Function.Get(funcName).MethodBuilder;
            var fec = Function.Get(funcName).Ec;
            int count = 0;
            foreach (Expression e in alist)
            {
                if (count >= fec.NumParameters)
                    throw new Exception("Too many arguments to function.");

                if (fec.GetParameter(count++).Type == e.GetEvaluatedType(ec))
                {
                    e.Push(ilg, ec);
                }
                else
                {
                    throw new Exception("Mismatch of argument types.");
                }
            }

            if (count < fec.NumParameters)
                throw new Exception("Too few arguments to function.");

            ilg.Emit(Emit.OpCodes.Call, Function.Get(funcName).MethodBuilder);
        }
    }

    public class PrintStatement : Statement
    {
        private Expression e;
        [Rule(@"<Statement> ::= ~print ~'(' <Expression> ~')' ~<nl>")]
        public PrintStatement(Expression e) { this.e = e; }
        public override void GenerateCode(Emit.ILGenerator ilg, ExecutionContext ec)
        {
            e.Push(ilg, ec);
            ilg.Emit(Emit.OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));
        }
    }

    public class AssignmentStatement : Statement
    {
        private Expression e;
        private String varName;

        [Rule(@"<Statement> ::= Identifier ~'=' <Expression> ~<nl>")]
        public AssignmentStatement(Identifier varName, Expression e)
        {
            this.e = e;
            this.varName = varName.Value;
        }

        public override void GenerateCode(Emit.ILGenerator ilg, ExecutionContext ec)
        {
            /*
             * Check for two things:
             * 1) Is it an argument?
             * 2) Is it a local variable (or global)
             */
            var arg = ec.GetParameter(varName);
            if (arg != null)
            {
                //This is an argument - we need to store it in its index.
                ilg.Emit(Emit.OpCodes.Starg, arg.Index);
                return;
            }

            var varDec = ec.GetVariable(varName);
            if (varDec == null)
            {
                //The person wants this to be a local variable. It has not been declared, so we must do it for them.
                varDec = ilg.DeclareLocal(e.GetEvaluatedType(ec));
                ec.AddVariable(varName, varDec);
            }

            e.Push(ilg, ec);
            ilg.Emit(Emit.OpCodes.Stloc, varDec); //Store the result of the expression.
        }
    }
    
}
