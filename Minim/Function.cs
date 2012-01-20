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
    public class Function : Token
    {
        private Sequence<Statement> stmts;
        private Emit.MethodBuilder mb;
        private ExecutionContext ec;

        [Rule(@"<Function> ::= Identifier Identifier ~'(' <ParameterList> ~')' ~<nl> <StatementList> ~';' ~<nl opt>")]
        public Function(Identifier returnType, Identifier name, Sequence<Parameter> pars, Sequence<Statement> stmts)
        {
            mb = CodeGenerator.CreateFunction(name.Value, TypeChecker.ConvertStringToType(returnType.Value), Parameter.ConvertSequenceToTypeArray(pars));
            this.stmts = stmts;
            if (fns.ContainsKey(name.Value))
            {
                throw new Exception("Function redeclared. Note: overloaded functions are not supported at this time.");
            }
            fns.Add(name.Value, this);
            ec = new ExecutionContext(null);
            ec.SetParameters(pars);
        }

        [Rule(@"<Function> ::= Identifier Identifier ~<nl> <StatementList> ~';' ~<nl opt>")]
        public Function(Identifier returnType, Identifier name, Sequence<Statement> stmts)
            : this(returnType, name, new Sequence<Parameter>(), stmts)
        {

        }

        public void GenerateCode()
        {
            var ilg = mb.GetILGenerator();
            foreach (Statement s in stmts)
                s.GenerateCode(ilg, ec);
        }

        private static Dictionary<String, Function> fns = new Dictionary<String, Function>();
        public static Function Get(String name)
        {
            if (fns.ContainsKey(name))
                return fns[name];
            else
                return null;
        }

        public Emit.MethodBuilder MethodBuilder
        {
            get { return mb; }
            set { mb = value; }
        }

        public ExecutionContext Ec
        {
            get { return ec; }
            set { ec = value; }
        }
    }
}
