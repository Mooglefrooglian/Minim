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
    public class Sequence<T> : Token, IEnumerable<T> where T : Token
    {
        private readonly T item;
        private readonly Sequence<T> next;

        [Rule(@"<ParameterList> ::=", typeof(Parameter))]
        [Rule(@"<FunctionList> ::=", typeof(Function))]
        [Rule(@"<StatementList> ::=", typeof(Statement))]
        [Rule(@"<ArgumentList> ::=", typeof(Expression))]
        public Sequence() : this(null, null) { }

        [Rule(@"<ParameterList> ::= <Parameter>", typeof(Parameter))]
        [Rule(@"<ArgumentList> ::= <Expression>", typeof(Expression))]
        public Sequence(T item) : this(item, null) { }

        [Rule(@"<ParameterList> ::= <Parameter> ~',' <ParameterList>", typeof(Parameter))]
        [Rule(@"<FunctionList> ::= <Function> <FunctionList>", typeof(Function))]
        [Rule(@"<StatementList> ::= <Statement> <StatementList>", typeof(Statement))]
        [Rule(@"<ArgumentList> ::= <Expression> ~',' <ArgumentList>", typeof(Expression))]
        public Sequence(T item, Sequence<T> next)
        {
            this.item = item;
            this.next = next;
        }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            for (Sequence<T> sequence = this; sequence != null; sequence = sequence.next)
            {
                if (sequence.item != null)
                {
                    yield return sequence.item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
