using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Permissions;
using EnsoulSharp.SDK;
using EnsoulSharp;

namespace _14_Senna
{
    class Program
    {
        static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;

        }
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        static void OnGameLoad()
        {


            Assembly testAssembly = Assembly.Load(rs._14Senna);
            Type calcType = testAssembly.GetType("_14Senna.Senna");

            calcType.InvokeMember("OnLoad",
                            BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.Public,
                            null, null, null);
        }
    }
}
