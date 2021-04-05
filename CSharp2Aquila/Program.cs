using System;
using System.IO;
using static System.Console;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharp2Aquila
{
    class Program
    {
        static void Main(string[] args)
        {
            StreamReader file = new StreamReader(@"C:\Users\Nicolas\Documents\EPITA\Code Vultus\Iris\csharp merge sort.cs");
            string src_code = file.ReadToEnd();
            file.Close();

            SyntaxTree tree = CSharpSyntaxTree.ParseText(src_code);
            Translator.traverseTree(tree);

            //
            
/*
            const string PROGRAM_TEXT = 
@"using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}";
            //
            tree = CSharpSyntaxTree.ParseText(PROGRAM_TEXT);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            //
            WriteLine($"The tree is a {root.Kind()} node.");
            WriteLine($"The tree has {root.Members.Count} elements in it.");
            WriteLine($"The tree has {root.Usings.Count} using statements. They are:");
            foreach (UsingDirectiveSyntax element in root.Usings)
                Console.WriteLine($"\t{element.Name}");
            //
            MemberDeclarationSyntax first_member = root.Members[0];
            WriteLine($"The first member is a {first_member.Kind()}.");
            var hello_world_declaration = (NamespaceDeclarationSyntax)first_member;
            //
            WriteLine($"There are {hello_world_declaration.Members.Count} members declared in this namespace.");
            WriteLine($"The first member is a {hello_world_declaration.Members[0].Kind()}.");
            //
            WriteLine($"There are {hello_world_declaration.Members.Count} members declared in this namespace.");
            WriteLine($"The first member is a {hello_world_declaration.Members[0].Kind()}.");
            //
            var program_declaration = (ClassDeclarationSyntax)hello_world_declaration.Members[0];
            WriteLine($"There are {program_declaration.Members.Count} members declared in the {program_declaration.Identifier} class.");
            WriteLine($"The first member is a {program_declaration.Members[0].Kind()}.");
            var main_declaration = (MethodDeclarationSyntax)program_declaration.Members[0];
            //
            WriteLine($"The return type of the {main_declaration.Identifier} method is {main_declaration.ReturnType}.");
            WriteLine($"The method has {main_declaration.ParameterList.Parameters.Count} parameters.");
            foreach (ParameterSyntax item in main_declaration.ParameterList.Parameters)
                WriteLine($"The type of the {item.Identifier} parameter is {item.Type}.");
            WriteLine($"The body text of the {main_declaration.Identifier} method follows:");
            WriteLine(main_declaration.Body.ToFullString());

            var args_parameter = main_declaration.ParameterList.Parameters[0];
*/
            //
        }
    }
}