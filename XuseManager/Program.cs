using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XuseManager {
    class Program {
        static void Main(string[] args) {
            foreach (string arg in args) {
                if (arg.Contains("_Dump")) {
                    XSM.XuseManager XM = new XSM.XuseManager(arg, true);
                    Console.WriteLine("Repacking: " + System.IO.Path.GetFileName(arg));
                    XM.CreatePackget(true);
                }
                else {
                    XSM.XuseManager XM = new XSM.XuseManager(arg, false);
                    for (int i = 0; i < XM.Packgets.Length; i++) {

                        Console.WriteLine("Extracting: " + XM.Packgets[i]);
                        XM.Extract(i, true);
                    }
                }
            }
            if (args == null || args.Length == 0)
                Console.WriteLine("XuseManager - Drop the game directory to this executable.");
            Console.WriteLine("Press a Key to exit.");
            Console.ReadKey();
        }
    }
}
