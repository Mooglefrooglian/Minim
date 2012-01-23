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
using System.Reflection;

namespace Minim
{
    public class CodeGenerator
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

            //Define the basic functions supplied by the language - notably, print
            new Function("Print", typeof(Console).GetMethod("WriteLine", new Type[] {typeof(string)}));
            new Function("Write", typeof(Console).GetMethod("Write", new Type[] { typeof(string) }));
            new Function("GetLine", typeof(Console).GetMethod("ReadLine", new Type[] { }));
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
            asmb.SetEntryPoint(main.MethodInfo);
            asmb.Save(moduleName);
        }
    }
}
