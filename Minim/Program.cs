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
    public class Program : Token
    {
        [Rule(@"<Program> ::= ~<nl opt> <FunctionList>")]
        public Program(Sequence<Function> functions)
        {
            foreach (Function f in functions)
                f.GenerateCode();
        }
    }    

    public class MainProgram
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
