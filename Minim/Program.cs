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
        [Rule(@"<StatementList> ::=", typeof(Statement))]
        public Sequence() : this(null, null) { }

        [Rule(@"<ParameterList> ::= <Parameter>", typeof(Parameter))]
        [Rule(@"<FunctionList> ::= <Function>", typeof(Function))]
        [Rule(@"<StatementList> ::= <Statement>", typeof(Statement))]
        public Sequence(T item) : this(item, null)  { }

        [Rule(@"<ParameterList> ::= <Parameter> ~',' <ParameterList>", typeof(Parameter))]
        [Rule(@"<FunctionList> ::= <Function> <FunctionList>", typeof(Function))]
        [Rule(@"<StatementList> ::= <Statement> ~<nl> <StatementList>", typeof(Statement))]
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
            this.type = type.Name;
            this.name = name.Name;
        }

        public static Type[] ConvertSequence(Sequence<Parameter> pars)
        {
            var l = new List<Type>();
            foreach (Parameter p in pars)
                l.Add(p.type);
        }
    }

    class Function : Token
    {
        private String name;
        private Sequence<Parameter> pars;
        private Sequence<Statement> stmts;

        [Rule(@"<Function> ::= ~v Identifier ~'(' <ParameterList> ~')' ~<nl> <StatementList> ~';'")]
        public Function(Identifier name, Sequence<Parameter> pars, Sequence<Statement> stmts)
        {
            this.name = name.Name;
            this.pars = pars;
            this.stmts = stmts;
        }

        public void GenerateCode()
        {
            var emitter = CodeGenerator.CreateFunction(name, pars, "void");
            foreach (Statement s in stmts)
                s.GenerateCode(emitter);
        }

        public String Name
        {
            get { return name; }
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
    [Terminal("v")]
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

        public String Name { get { return name; } }

        public override void Push(Emit.ILGenerator ilg)
        {

        }
    }

    abstract class Statement : Token
    {
        public abstract void GenerateCode(Emit.ILGenerator ilg);
    }

    class PrintStatement : Statement
    {
        private Expression e;
        [Rule(@"<Statement> ::= ~print ~'(' <Expression> ~')'")]
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

        private static Emit.MethodBuilder mainMethod;

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

        public static Emit.ILGenerator CreateFunction(String name, Sequence<Parameter> pars, String returnType)
        {
            var func = typeBuilder.DefineMethod(name, Reflect.MethodAttributes.Static, typeof(void), System.Type.EmptyTypes); //ignore parameters and return type for now TODO
            tempmainglobal = func;
            return func.GetILGenerator();
        }

        private static Emit.MethodBuilder tempmainglobal;

        public static void Complete() //Ends the assembly, saves to disk.
        {
            typeBuilder.CreateType();
            modb.CreateGlobalFunctions();
            asmb.SetEntryPoint(tempmainglobal);
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
                Console.WriteLine(string.Format("{0} {1}", "^".PadLeft((int)(token.Position.Index + 1)), parseMessage));
            }
            
        }
    }
}
