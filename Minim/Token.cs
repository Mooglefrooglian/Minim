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
    [Terminal("(EOF)")]
    [Terminal("(Error)")]
    [Terminal("(Whitespace)")]
    [Terminal("(")]
    [Terminal(")")]
    [Terminal(";")]
    [Terminal(",")]
    [Terminal("NewLine")]
    [Terminal("print")]
    [Terminal("=")]
    public class Token : SemanticToken
    {
        [Rule(@"<nl> ::= ~NewLine ~<nl>")]
        [Rule(@"<nl opt> ::= ~NewLine ~<nl opt>")]
        [Rule(@"<nl opt> ::=")]
        public Token() { }
    }
}
