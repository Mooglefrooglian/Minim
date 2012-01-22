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
        private FunctionCall fc;

        [Rule(@"<Statement> ::= <FunctionCall> ~<nl>")]
        public CallStatement(FunctionCall fc)
        {
            this.fc = fc;
        }

        [Rule(@"<Statement> ::= Identifier ~<nl>")]
        public CallStatement(Identifier funcName) 
        {
            this.fc = new FunctionCall(funcName, new Sequence<Expression>());
        }


        public override void GenerateCode(Emit.ILGenerator ilg, ExecutionContext ec)
        {
            this.fc.Push(ilg, ec);
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
                ilg.Emit(Emit.OpCodes.Starg, arg.Position);
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

    public class ReturnStatement : Statement
    {
        Expression returnExp;

        [Rule(@"<Statement> ::= ~'^' <Expression> ~<nl>")]
        public ReturnStatement(Expression e)
        {
            this.returnExp = e;
        }

        [Rule(@"<Statement> ::= ~'^' ~<nl>")]
        public ReturnStatement() : this(null) { }

        public override void GenerateCode(Emit.ILGenerator ilg, ExecutionContext ec)
        {
            if (this.returnExp != null)
                this.returnExp.Push(ilg, ec);

            ilg.Emit(Emit.OpCodes.Ret);
        }
    }
    
}
