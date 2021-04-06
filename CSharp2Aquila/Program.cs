using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharp2Aquila
{
    class Program
    {
        static void Main(string[] args)
        {
            // read source code
            var file = new StreamReader(@"C:\Users\Nicolas\Documents\EPITA\Code Vultus\Iris\csharp merge sort.cs");
            string src_code = file.ReadToEnd();
            file.Close();

            // translate source code
            SyntaxTree tree = CSharpSyntaxTree.ParseText(src_code);
            //Translator.traverseTree(tree);
            string source_code = "/** Automatic translation of CSharp source code to Aquila by https://github.com/Nicolas-Reyland/CSharp2Aquila **/\n\n" +
                                 Translator.translateAll(tree);
            
            // write source code
            var sw = new StreamWriter(@"C:\Users\Nicolas\Documents\EPITA\Code Vultus\Iris\csharp merge sort translation.aq");
            sw.Write(source_code);
            sw.Close();
        }
    }
}