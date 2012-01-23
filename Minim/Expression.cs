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

[assembly: RuleTrim("<Value> ::= '(' <Expression> ')'", "<Expression>", SemanticTokenType = typeof(Minim.Token))] //because <Value> can be just an expression with brackets, no sesne making a new class

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
                ilg.Emit(Emit.OpCodes.Ldarg, arg.Position);
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
                if (funcDec.MethodInfo.ReturnType != typeof(void))
                {
                    ilg.Emit(Emit.OpCodes.Call, funcDec.MethodInfo); //Only functions without parameters can be called currently - changes to the grammar are needed to improve on this and will likely become their own type of expression
                    return;
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
                return funcDec.MethodInfo.ReturnType;
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
            //Parse the string - deal with escaped characters.
            this.s = s.Substring(1, s.Length - 2); //Get rid of beginning and ending quote there because of the terminal definition.
            s = this.s;
            StringBuilder escaped = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\')
                {
                    //Escaped character! Deal with it by looking at the next.
                    i++;
                    switch(s[i])
                    {
                        case 'n':
                            escaped.Append('\n');
                            break;
                        case '\\':
                            escaped.Append('\\');
                            break;
                        case 't':
                            escaped.Append('\t');
                            break;
                        case 'r':
                            escaped.Append('\r');
                            break;
                        case '"':
                            escaped.Append('"');
                            break;
                        default:
                            throw new Exception("Unknown escape character.");
                    }
                }
                else
                {
                    escaped.Append(s[i]);
                }
            }
            this.s = escaped.ToString();
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

    public class MathExpression : Expression
    {
        Expression left;
        Expression right;
        Operator o;

        [Rule(@"<Expression> ::= <Expression> '+' <Mult Exp>")] 
        [Rule(@"<Expression> ::= <Expression> '-' <Mult Exp>")]
        [Rule(@"<Mult Exp> ::= <Mult Exp> '*' <Negate Exp>")]
        [Rule(@"<Mult Exp> ::= <Mult Exp> '/' <Negate Exp>")]
        public MathExpression(Expression left, Operator o, Expression right)
        {
            this.left = left;
            this.o = o;
            this.right = right;
        }

        public override Type GetEvaluatedType(ExecutionContext ec)
        {
            return this.o.GetEvaluatedType(left, right, ec);
        }

        public override void Push(Emit.ILGenerator ilg, ExecutionContext ec)
        {
            this.o.Evaluate(left, right, ilg, ec);
        }
    }

    public class NegateExpression : Expression
    {
        Expression e;

        [Rule(@"<Negate Exp> ::= ~'-' <Value>")]
        public NegateExpression(Expression e)
        {
            this.e = e;
        }

        public override void Push(Emit.ILGenerator ilg, ExecutionContext ec)
        {
            throw new NotImplementedException();
        }

        public override Type GetEvaluatedType(ExecutionContext ec)
        {
            throw new NotImplementedException();
        }
    }

    public class FunctionCall : Expression
    {
        Sequence<Expression> alist;
        String funcName;

        [Rule(@"<FunctionCall> ::= Identifier ~'(' <ArgumentList> ~')'")]
        public FunctionCall(Identifier funcName, Sequence<Expression> alist)
        {
            this.funcName = funcName.Value;
            this.alist = alist;
        }

        public override Type GetEvaluatedType(ExecutionContext ec)
        {
            return Function.Get(funcName).MethodInfo.ReturnType;
        }

        public override void Push(Emit.ILGenerator ilg, ExecutionContext ec)
        {
            var f = Function.Get(funcName).MethodInfo;
            var fec = Function.Get(funcName).Ec;
            int count = 0;
            var pars = fec.GetParameters();
            foreach (Expression e in alist)
            {
                if (count >= fec.NumParameters)
                    throw new Exception("Too many arguments to function.");

                if (pars[count++].Type == e.GetEvaluatedType(ec))
                {
                    e.Push(ilg, ec);
                }
                else
                {
                    throw new Exception("Mismatch of argument types - no coercing available currently.");
                }
            }

            if (count < fec.NumParameters)
                throw new Exception("Too few arguments to function.");

            ilg.Emit(Emit.OpCodes.Call, Function.Get(funcName).MethodInfo);
        }
    }
}
