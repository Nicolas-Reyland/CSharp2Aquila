using System;
using System.IO;

namespace CSharp2Aquila
{
    public static class Program
    {
        public static bool verbose = true;
        
        static void usage()
        {
            if (verbose) Console.WriteLine(@"Usage:
(mono) translator.exe ""path-to-input-file"" ""path-to-output-file""
");
        }

        static void translate(string path1, string path2)
        {
            var file = new StreamReader(path1);
            string src_code = file.ReadToEnd();
            file.Close();

            // translate source code
            string new_src_code = Translator.translateFromSourceCode(src_code);

            // write source code
            var sw = new StreamWriter(path2);
            sw.Write(new_src_code);
            sw.Close();
            
            if (verbose) Console.WriteLine($"Successfully written code to \"{path2}\"");
        }
        
        // ReSharper disable once InconsistentNaming
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                if (verbose) Console.WriteLine("The number of given arguments is not 2.");
                usage();
            }
            else
            {
                string path1 = args[0], path2 = args[1];
                if (!File.Exists(path1))
                {
                    throw new FileNotFoundException($"The file \"{path1}\" does not exist.");
                }

                translate(path1, path2);
            }
        }
    }
}