using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emit = System.Reflection.Emit;
using System.Reflection;

/*
 * The point of an ExecutionContext is to provide a "scope" for variables. The upper scope would allow for global variables, and a level lower might provide for a function's local variables/its symbol table.
 * Of course, if I ever get around to making Minima, classes will also enjoy having their own level of ExecutionContext.
 * An ExecutionContext can have a parent, who in turn can have their own parent and so on. A simple linked list, though multiple "nodes" lead to the same node.
 * It also, unfortunately, has a separate symbol table for arguments - this doesn't make sense in a lot of places, like say, global variables. I suppose you could consider the command line one though...
 * Perhaps that's the way to do things? Still, dunno, feel this class is straying from its intended purpose.
 */

namespace Minim
{
    public class ExecutionContext
    {
        ExecutionContext parent = null;
        Dictionary<String, Emit.LocalBuilder> symbolTable = new Dictionary<String, Emit.LocalBuilder>();
        Dictionary<String, Parameter> pars;
        Parameter[] parsArr;

        public ExecutionContext(ExecutionContext p)
        {
            this.parent = p;
        }

        public Emit.LocalBuilder GetVariable(String name)
        {
            if (symbolTable.ContainsKey(name))
            {
                return symbolTable[name];
            }
            else
            {
                //Attempt to find an upper level
                if (parent != null)
                    return parent.GetVariable(name);
                else
                    return null;
            }
        }

        public void AddVariable(String name, Emit.LocalBuilder variableDec)
        {
            if (symbolTable.ContainsKey(name))
                throw new Exception("Attempt to redeclare existing var - likely a compiler error, not a user error. Please report this bug.");

            symbolTable.Add(name, variableDec);
        }

        public Parameter GetParameter(String name)
        {
            if (pars.ContainsKey(name))
                return pars[name];
            else
                return null;
        }

        public Parameter[] GetParameters()
        {
            return parsArr;
        }

        public int NumParameters
        {
            get { return parsArr.Length; }
        }

        public void SetParameters(Parameter[] pars)
        {
            this.pars = new Dictionary<String, Parameter>();
            int count = 0;
            foreach (Parameter p in pars)
            {
                this.pars.Add(p.Name, p);
                p.Position = count++;
            }
            parsArr = pars;
        }
    }
}
