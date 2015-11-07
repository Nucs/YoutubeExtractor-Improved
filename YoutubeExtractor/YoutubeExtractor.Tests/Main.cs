using System;
using nucs.Monitoring;

namespace YoutubeExtractor.Tests {
    public static class MainClass {
        [STAThread]
        public static void Main() {
            var a = new ClipboardStringMonitor();
            a.Changed+= AOnChanged;
            a.Start();
            Console.WriteLine("Started");
            Console.ReadLine();
        }

        private static void AOnChanged(object item) {
            Console.WriteLine(item);
        }
    }
}