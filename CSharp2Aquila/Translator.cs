using System;
using System.Collections.Generic;
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
            var name_space_content = (NamespaceDeclarationSyntax) root.Members[0];
            var program_class = (ClassDeclarationSyntax) name_space_content.Members[0]; // assume this (that first class is Program)
            
            // convert all functions to a string format & call the 'Main' function at the end in Aquila
            string s = program_class.Members.Select(x => (MethodDeclarationSyntax) x).Aggregate("",
                (current, method) => current + translateMethodDeclaration(method) + "\n");
            
            // call the Main function at the end
            s += "/** Manually call the 'Main' function here **/\n";
            s += "Main()\n";
            
            return s;
        }

        private static string translateType(TypeSyntax type_syntax)
        {
            string s = type_syntax.ToString();

            // basic types
            if (s == "int" || s == "float" || s == "bool")
            {
                return s;
            }

            // enumerable types
            if (s.EndsWith("[]"))
            {
                return "list"; // NOT COOL !
            }

            return "auto";

            //throw new NotImplementedException("Unsupported type: " + s);
        }

        private static string translateMethodDeclaration(MethodDeclarationSyntax method_declaration)
        {
            string name = method_declaration.Identifier.ToString();
            // extract the parameter names (no type checking done there)
            IEnumerable<string> parameters = method_declaration.ParameterList.Parameters.Select(x => x.Identifier.ToString());
            string return_type = translateType(method_declaration.ReturnType);
            
            // add each statement
            if (method_declaration.Body == null) throw new Exception("Body is null !!");
            string statements = method_declaration.Body.Statements.Aggregate("",
                (current, statement) => current + translateStatement(statement) + "\n");

            string function_string = $"function recursive {return_type} {name}(";
            function_string += parameters.Aggregate("", (current, st) => current + st + ", ");

            return function_string + "\n" + statements;
        }

        private static string translateStatement(StatementSyntax statement_syntax)
        {
            switch (statement_syntax)
            {
                case ExpressionStatementSyntax expression_statement_syntax:
                    return translateExpressionStatement(expression_statement_syntax);
                case WhileStatementSyntax while_statement_syntax:
                    return translateWhileStatement(while_statement_syntax);
                case ForStatementSyntax for_statement_syntax:
                    return translateForStatement(for_statement_syntax);
                case IfStatementSyntax if_statement_syntax:
                    return translateIfStatement(if_statement_syntax);
                case LocalDeclarationStatementSyntax local_declaration_statement_syntax:
                    return translateLocalDeclarationStatement(local_declaration_statement_syntax);
                default:
                    throw new NotImplementedException(statement_syntax + "is not supported. Kind: " + statement_syntax.Kind());
            }
        }

        private static string translateExpressionStatement(ExpressionStatementSyntax expression_statement)
        {
            //

            return "";
        }

        private static string translateWhileStatement(WhileStatementSyntax while_statement)
        {
            //

            return "";
        }

        private static string translateForStatement(ForStatementSyntax for_statement)
        {
            //

            return "";
        }

        private static string translateIfStatement(IfStatementSyntax if_statement)
        {
            //

            return "";
        }

        private static string translateLocalDeclarationStatement(LocalDeclarationStatementSyntax local_declaration_statement)
        {
            //

            return "";
        }
    }
}