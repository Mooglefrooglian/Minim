using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Minim
{
    public class TypeChecker
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
}
