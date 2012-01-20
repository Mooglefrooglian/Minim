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
    public abstract class Expression : Token
    {
        public abstract void Push(Emit.ILGenerator ilg, ExecutionContext ec);
        public abstract Type GetEvaluatedType(ExecutionContext ec);
    }

    [Terminal("Identifier")]
    public class Identifier : Expression
    {
        String name;

        public Identifier(String name)
        {
            this.name = name;
        }

        public String Value { get { return name; } }

        public override void Push(Emit.ILGenerator ilg, ExecutionContext ec)
        {
            /*
             * Check for three things:
             * 1) Is it an argument?
             * 2) Is it a local variable (or global)
             * 3) Is it a function call without parantheses?
             */
            var arg = ec.GetParameter(name);
            if (arg != null)
            {
                //This is an argument - push its index onto the stack!
                ilg.Emit(Emit.OpCodes.Ldarg, arg.Index);
                return;
            }

            var varDec = ec.GetVariable(name);
            if (varDec != null)
            {
                //This is a local variable! Push it onto the stack.
                ilg.Emit(Emit.OpCodes.Ldloc, varDec);
                return;
            }

            var funcDec = Function.Get(name);
            if (funcDec != null)
            {
                if (funcDec.MethodBuilder.ReturnType != typeof(void))
                {
                    ilg.Emit(Emit.OpCodes.Call, funcDec.MethodBuilder); //Only functions without parameters can be called currently - changes to the grammar are needed to improve on this and will likely become their own type of expression
                }
                else
                {
                    throw new Exception("Attempting to use a void type as an expression.");
                }
            }
            //At this point, we have an unrecognized identifier

            throw new Exception("Use of undeclared identifier in expression.");
        }

        public override Type GetEvaluatedType(ExecutionContext ec)
        {
            var arg = ec.GetParameter(name);
            if (arg != null)
            {
                return arg.Type;
            }

            var varDec = ec.GetVariable(name);
            if (varDec != null)
            {
                return varDec.LocalType;
            }

            var funcDec = Function.Get(name);
            if (funcDec != null)
            {
                return funcDec.MethodBuilder.ReturnType;
            }

            //At this point, we have an unrecognized identifier

            throw new Exception("Use of undeclared identifier in expression.");
        }
    }

    [Terminal("StringLiteral")]
    public class StringLiteral : Expression
    {
        private String s;

        public StringLiteral(String s)
        {
            this.s = s.Substring(1, s.Length - 2);
        }

        public override void Push(Emit.ILGenerator ilg, ExecutionContext ec)
        {
            ilg.Emit(Emit.OpCodes.Ldstr, s);
        }

        public override Type GetEvaluatedType(ExecutionContext ec)
        {
            return typeof(string);
        }
    }
}
