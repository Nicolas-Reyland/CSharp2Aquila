using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharp2Aquila
{
    public static class Translator
    {
        /* hm
         * mh
         */

        public static void traverseTree(SyntaxTree tree)
        {
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            // get namespace content originally a 'MemberDeclarationSyntax'
            var name_space_content = (NamespaceDeclarationSyntax) root.Members[0];
            // assume there is only thr 'Program' class (other classes not supported yet?)
            var program_class = (ClassDeclarationSyntax) name_space_content.Members[0];
            // get all the methods in it
            foreach (MemberDeclarationSyntax member in program_class.Members)
            {
                var method = (MethodDeclarationSyntax) member;
                Console.WriteLine("\nName: " + method.Identifier);
                Console.WriteLine("ParameterList: " + method.ParameterList);
                Console.WriteLine("Parameters: " + method.ParameterList.Parameters.Count);
                foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
                {
                    Console.WriteLine("\t name: " + parameter.Identifier);
                    Console.WriteLine("\t type: " + parameter.Type);
                    Console.WriteLine("\t modifiers: " + parameter.Modifiers); // e.g. ref
                }
                // Console.WriteLine("Modifiers: " + method.Modifiers); // -> e.g. static
                Console.WriteLine("ReturnType: " + method.ReturnType);
                Console.WriteLine("Body:");
                foreach (StatementSyntax statement in method.Body.Statements)
                {
                    Console.WriteLine("\tnew list of attr: " + statement.Kind());
                    foreach (var attribute_list in statement.AttributeLists.Select(x => x.Attributes))
                    {
                        Console.WriteLine("\t\tnew attribute list");
                        foreach (AttributeSyntax attribute in attribute_list)
                        {
                            Console.WriteLine("\t\t\tname: " + attribute.Name);
                        }
                    }
                }
            }
        }

        public static string translateAll(SyntaxTree tree)
        {
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

            return "";
        }

        private static string translateMethodDeclaration(MethodDeclarationSyntax method_declaration_syntax)
        {
            //

            return "";
        }
    }
}