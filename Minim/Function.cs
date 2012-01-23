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
using Emit = System.Reflection.Emit;
using System.IO;
using System.Reflection;

namespace Minim
{
    public class Function : Token
    {
        private Sequence<Statement> stmts;
        private MethodInfo mi;
        private ExecutionContext ec;
        private static Dictionary<String, Function> fns = new Dictionary<String, Function>();

        [Rule(@"<Function> ::= Identifier Identifier ~'(' <ParameterList> ~')' ~<nl> <StatementList> ~';' ~<nl opt>")]
        public Function(Identifier returnType, Identifier name, Sequence<Parameter> pars, Sequence<Statement> stmts)
        {
            Type[] tar = Parameter.ConvertSequenceToTypeArray(pars);
            var mb = CodeGenerator.CreateFunction(name.Value, TypeChecker.ConvertStringToType(returnType.Value), tar);
            mi = mb;
            this.stmts = stmts;
            if (fns.ContainsKey(name.Value))
            {
                throw new Exception("Function redeclared. Note: overloaded functions are not supported at this time.");
            }
            fns.Add(name.Value, this);
            int count = 1; //parameters start at index 1 - 0 is the return. not sure on GetParameters() though - more testing is needed, but i don't think it includes the returntype as there is a separate way to get that
            foreach (Parameter p in pars)
            {
                mb.DefineParameter(count++, ParameterAttributes.None, p.Name);
            }
            ec = new ExecutionContext(null);
            ec.SetParameters(pars.ToArray());
        }

        [Rule(@"<Function> ::= Identifier Identifier ~<nl> <StatementList> ~';' ~<nl opt>")]
        public Function(Identifier returnType, Identifier name, Sequence<Statement> stmts)
            : this(returnType, name, new Sequence<Parameter>(), stmts)
        {

        }

        //For use with already-defined functions, like Console.WriteLine. Allows you to alias them for use inside the language.
        public Function(String funcName, MethodInfo mi)
        {
            fns.Add(funcName, this);
            this.mi = mi;
            ec = new ExecutionContext(null);
            var parInfos = mi.GetParameters();
            Parameter[] pars = new Parameter[parInfos.Length];
            for (int i = 0; i < pars.Length; i++)
                pars[i] = new Parameter(parInfos[i].ParameterType, parInfos[i].Name);
            ec.SetParameters(pars);
        }

        public void GenerateCode()
        {
            var ilg = ((Emit.MethodBuilder)mi).GetILGenerator();
            foreach (Statement s in stmts)
                s.GenerateCode(ilg, ec);
        }

        public static Function Get(String name)
        {
            if (fns.ContainsKey(name))
                return fns[name];
            else
                return null;
        }

        public MethodInfo MethodInfo
        {
            get { return mi; }
            set { mi = value; }
        }

        public ExecutionContext Ec
        {
            get { return ec; }
            set { ec = value; }
        }
    }
}
