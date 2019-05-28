using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGen
{
    class Program
    {
        static bool _stopBeforeExit;

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("argument wrong");
            }
            else
            {
                var engine = new CodeGenEngine.Engine(Path.GetFullPath(args[0]));
                engine.MessageSent += MessageSent;
                engine.Run();
            }
            if (_stopBeforeExit)
                Console.ReadLine();
        }

        static void MessageSent(CodeGenEngine.MessageType type, string message)
        {
            if (type == CodeGenEngine.MessageType.Error)
                _stopBeforeExit = true;
            Console.WriteLine($"[{type}] {message}");
        }
    }
}
