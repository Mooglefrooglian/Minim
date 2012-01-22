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
    public abstract class Operator : Token
    {
        public abstract void Evaluate(Expression left, Expression right, Emit.ILGenerator ilg, ExecutionContext ec);
        public abstract Type GetEvaluatedType(Expression left, Expression right, ExecutionContext ec);
    }

    [Terminal("+")]
    public class PlusOperator : Operator
    {
        public override void Evaluate(Expression left, Expression right, Emit.ILGenerator ilg, ExecutionContext ec)
        {
            Type t = left.GetEvaluatedType(ec);
            Type t2 = right.GetEvaluatedType(ec);

            if (t != t2)
            {
                throw new NotImplementedException("Type coercion not implemented yet.");
            }

            if (t == typeof(string))
            {
                //Concatenate strings! Fun for all. 
                //TODO: optimize the shit out of this to determine if there is tons of concatenating going on because this is inefficient as hell for something like "lol" + "hi" + 3
                //TODO: classes with operator overloading so I don't need to do special exceptions within the language code itself. I dislike that.
                left.Push(ilg, ec);
                right.Push(ilg, ec);
                ilg.Emit(Emit.OpCodes.Call, typeof(String).GetMethod("Concat", new Type[] { typeof(string), typeof(string) }));
            }
        }

        public override Type GetEvaluatedType(Expression left, Expression right, ExecutionContext ec)
        {
            Type t = left.GetEvaluatedType(ec);
            Type t2 = right.GetEvaluatedType(ec);

            if (t != t2)
            {
                throw new NotImplementedException("Type coercion not implemented yet.");
            }

            return t; //same type because you're adding!
        }
    }

    //Todo: implement them with the Operator abstract class

    [Terminal("-")]
    public class MinusOperator : Operator
    {
        public override void Evaluate(Expression left, Expression right, Emit.ILGenerator ilg, ExecutionContext ec)
        {
            throw new NotImplementedException("Non + operators are not implemented.");
        }
        public override Type GetEvaluatedType(Expression left, Expression right, ExecutionContext ec)
        {
            throw new NotImplementedException("Non + operators are not implemented.");
        }
    }

    [Terminal("*")]
    public class MultiplyOperator : Operator
    {
        public override void Evaluate(Expression left, Expression right, Emit.ILGenerator ilg, ExecutionContext ec)
        {
            throw new NotImplementedException("Non + operators are not implemented.");
        }
        public override Type GetEvaluatedType(Expression left, Expression right, ExecutionContext ec)
        {
            throw new NotImplementedException("Non + operators are not implemented.");
        }
    }

    [Terminal("/")]
    public class DivideOperator : Operator
    {
        public override void Evaluate(Expression left, Expression right, Emit.ILGenerator ilg, ExecutionContext ec)
        {
            throw new NotImplementedException("Non + operators are not implemented.");
        }

        public override Type GetEvaluatedType(Expression left, Expression right, ExecutionContext ec) { throw new NotImplementedException("Non + operators are not implemented."); }
    }
}
