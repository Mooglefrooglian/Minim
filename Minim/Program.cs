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
    class Program : Token
    {
        [Rule(@"<Program> ::= ~<nl opt> <FunctionList>")]
        public Program(Sequence<Function> functions)
        {
            foreach (Function f in functions)
                f.GenerateCode();
        }
    }
    class Sequence<T> : Token, IEnumerable<T> where T : Token
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
        public Sequence(T item) : this(item, null)  { }

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

    class Parameter : Token
    {
        private String type;
        private String name;

        [Rule(@"<Parameter> ::= Identifier Identifier")]
        public Parameter(Identifier type, Identifier name)
        {
            this.type = type.Value;
            this.name = name.Value;
        }

        public static Type[] ConvertSequenceToTypeArray(Sequence<Parameter> pars)
        {
            var l = new List<Type>();
            foreach (Parameter p in pars)
                l.Add(TypeChecker.ConvertStringToType(p.type));
            return l.ToArray();
        }
    }

    class TypeChecker
    {
        private static Dictionary<String, Type> typeHash = new Dictionary<String, Type>();

        public static void Init()
        {
            typeHash.Add("void", typeof(void));
            typeHash.Add("String", typeof(string));
            typeHash.Add("Int", typeof(Int32));
        }

        public static Type ConvertStringToType(String type)
        {
            return typeHash[type];
        }
    }

    class Function : Token
    {
        private Sequence<Statement> stmts;
        private Emit.MethodBuilder mb;

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
        }

        [Rule(@"<Function> ::= Identifier Identifier ~<nl> <StatementList> ~';' ~<nl opt>")]
        public Function(Identifier returnType, Identifier name, Sequence<Statement> stmts) : this(returnType, name, new Sequence<Parameter>(), stmts)
        {

        }

        public void GenerateCode()
        {
            var ilg = mb.GetILGenerator();
            foreach (Statement s in stmts)
                s.GenerateCode(ilg);
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
    }


    [Terminal("(EOF)")]
    [Terminal("(Error)")]
    [Terminal("(Whitespace)")]
    [Terminal("(")]
    [Terminal(")")]
    [Terminal(";")]
    [Terminal(",")]
    [Terminal("NewLine")]
    [Terminal("print")]
    class Token : SemanticToken
    {
        [Rule(@"<nl> ::= ~NewLine ~<nl>")]
        [Rule(@"<nl opt> ::= ~NewLine ~<nl opt>")]
        [Rule(@"<nl opt> ::=")]
        public Token() { }
    }

    abstract class Expression : Token
    {
        public abstract void Push(Emit.ILGenerator ilg);
    }

    [Terminal("StringLiteral")]
    class StringLiteral : Expression
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

    [Terminal("Identifier")]
    class Identifier : Expression
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

    abstract class Statement : Token
    {
        public abstract void GenerateCode(Emit.ILGenerator ilg);
    }

    class CallStatement : Statement
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

    class PrintStatement : Statement
    {
        private Expression e;
        [Rule(@"<Statement> ::= ~print ~'(' <Expression> ~')' ~<nl>")]
        public PrintStatement(Expression e) { this.e = e; }
        public override void GenerateCode(Emit.ILGenerator ilg) 
        {
            e.Push(ilg); 
            ilg.Emit(Emit.OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] {typeof(string)} )); 
        }
    }

    class CodeGenerator
    {
        private static String moduleName;
        private static Reflect.AssemblyName name;
        private static Emit.AssemblyBuilder asmb;
        private static Emit.ModuleBuilder modb;
        private static Emit.TypeBuilder typeBuilder;

        public static void Init(String modName)
        {
            moduleName = modName;

            if (IO.Path.GetFileName(moduleName) != moduleName)
            {
                throw new System.Exception("can only output into current directory!");
            }

            name = new Reflect.AssemblyName(IO.Path.GetFileNameWithoutExtension(moduleName));
            asmb = System.AppDomain.CurrentDomain.DefineDynamicAssembly(name, Emit.AssemblyBuilderAccess.Save);
            modb = asmb.DefineDynamicModule(moduleName);
            typeBuilder = modb.DefineType("Foo"); //Normally, you'd define a class name here
        }

        public static Emit.MethodBuilder CreateFunction(String name, Type returnType, Type[] ptypes)
        {
            return typeBuilder.DefineMethod(name, Reflect.MethodAttributes.Static, returnType, ptypes);
        }

        public static void Complete() //Ends the assembly, saves to disk.
        {
            typeBuilder.CreateType();
            modb.CreateGlobalFunctions();
            var main = Function.Get("main");
            if (main == null)
                throw new Exception("Main class not found.");
            asmb.SetEntryPoint(main.MethodBuilder);
            asmb.Save(moduleName);
        }
    }


    class MainProgram
    {
        static void Main(string[] args)
        {
            CompiledGrammar grammar = CompiledGrammar.Load(typeof(Token), "minim0.1.cgt");
            SemanticTypeActions<Token> actions = new SemanticTypeActions<Token>(grammar);
            CodeGenerator.Init("test.exe");
            TypeChecker.Init();

            try
            {
                actions.Initialize(true);
            }
            catch (InvalidOperationException ex)
            {
                Console.Write(ex.Message);
                Console.ReadKey(true);
                return;
            }

            SemanticProcessor<Token> processor = new SemanticProcessor<Token>(new StreamReader(args[0]), actions);
            ParseMessage parseMessage = processor.ParseAll();
            if (parseMessage == ParseMessage.Accept)
            {
                Console.WriteLine("Parsed successfully.");
                Program p = (Program)processor.CurrentToken;
                CodeGenerator.Complete();
            }
            else
            {
                IToken token = processor.CurrentToken;
                Console.WriteLine("Error on line " + token.Position.Line + ".\n" + token.Position.ToString());
                Console.WriteLine(string.Format("{0} {1}", "^".PadLeft((int)(token.Position.Index + 1)), parseMessage));
            }
            
        }
    }
}
