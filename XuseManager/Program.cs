using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XuseManager {
    class Program {
        static void Main(string[] args) {
            foreach (string arg in args) {
                XSM.XuseManager XM = new XSM.XuseManager(arg);
                for (int i = 0; i < XM.Packgets.Length; i++) {
                    Console.WriteLine("Extracting: " + XM.Packgets[i]);
                    XM.Extract(i);
                }
            }
            if (args == null || args.Length == 0) {
                Console.WriteLine("XuseManager - Drop the game directory to this executable.");
                Console.ReadKey();
            }
        }
    }
}
