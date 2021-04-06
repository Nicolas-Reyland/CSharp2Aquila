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
        /* TODO:
         * look at Functions.cs - approx. line 535: support all the functions in the 'functions_dict'
         */

        private static int _code_depth = 0;
        private static string addTabs(int n = -1) => new string('\t', n == -1 ? _code_depth : n);
        private static void incrCodeDepth() => _code_depth++;
        private static void decrCodeDepth() => _code_depth = _code_depth == 0 ? 0 : _code_depth - 1;

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
                if (method.Body == null) throw new Exception("Body of method is null");
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
            s += "\n/** Manually call the 'Main' function here **/\nMain()\n";
            
            return s;
        }

        public static string injectSourceCode(string source_code, bool add_curly_braces) =>
@"namespace CodeInjection
{
    class Program
    {
        static void Main(string[] args)
" + (add_curly_braces ? "{" : "") + source_code + (add_curly_braces ? "}" : "") + @"
    }
}";

        public static SyntaxList<StatementSyntax> extractSyntaxList(string source_code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source_code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var name_space = (NamespaceDeclarationSyntax) root.Members[0];
            var program_class = (ClassDeclarationSyntax) name_space.Members[0];
            var method = (MethodDeclarationSyntax) program_class.Members[0];
            if (method.Body == null) throw new Exception("Body is null for code extraction");
            return method.Body.Statements;
        }

        public static string translateMethodDeclaration(MethodDeclarationSyntax method_declaration)
        {
            string name = method_declaration.Identifier.ToString();
            // extract the parameter names (no type checking done there)
            IEnumerable<string> parameters = method_declaration.ParameterList.Parameters.Select(x => x.Identifier.ToString());
            string return_type = SubSyntaxTranslator.translateType(method_declaration.ReturnType);
            
            // add each statement
            if (method_declaration.Body == null) throw new Exception("Body is null !!");
            incrCodeDepth();
            string statements = method_declaration.Body.Statements.Aggregate("",
                (current, statement) => current + translateStatement(statement) + "\n");
            decrCodeDepth();
            
            string function_string = $"function recursive {return_type} {name}(";
            // add arguments if there are any
            var enumerable = parameters as string[] ?? parameters.ToArray();
            if (enumerable.Any())
            {
                function_string += enumerable.Aggregate("", (current, st) => current + st + ", ");
                function_string = function_string.Substring(0, function_string.Length - 2); // remove last ", "
            }
            
            function_string += ")\n";

            return function_string + "\n" + statements + "\nend-function";
        }

        public static string translateStatement(StatementSyntax statement_syntax)
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
                case ReturnStatementSyntax return_statement_syntax:
                    return translateReturnStatement(return_statement_syntax);
                default:
                    Console.WriteLine("[!] " + statement_syntax.GetType() + " is not supported.\n\tkind: " + statement_syntax.Kind());
                    return "/** TRANSLATOR WARNING: Unknown statement :\n\n" + statement_syntax + "\n\n**/";
            }
        }

        // Statements -> these functions usually add a '\n' at the end
        public static string translateExpressionStatement(ExpressionStatementSyntax expression_statement)
        {
            //Console.WriteLine(expression_statement);
            // Console.WriteLine("\t" + expression_statement.AttributeLists.Count);
            // Console.WriteLine("\t" + expression_statement.AllowsAnyExpression);
            /*Console.WriteLine("\t" + expression_statement.Expression.GetType());
            Console.WriteLine("\t" + expression_statement.Expression.Kind());
            Console.WriteLine();*/

            switch (expression_statement.Expression)
            {
                case AssignmentExpressionSyntax assignment:
                    return addTabs() + ExpressionTranslator.translateAssignmentExpression(assignment) + "\n";
                case InvocationExpressionSyntax invocation:
                    return addTabs() + ExpressionTranslator.translateInvocationExpression(invocation) + "\n";
                default:
                    Console.WriteLine("[!] Unsupported expression:\n\t" +
                                      "raw: " + expression_statement.Expression +
                                      "\n\ttype: " + expression_statement.Expression.GetType() +
                                      "\n\tkind: " + expression_statement.Expression.Kind());
                    break;
            }

            return addTabs() + expression_statement + " // RAW (untouched)\n";
        }

        public static string translateWhileStatement(WhileStatementSyntax while_statement)
        {
            string condition = ExpressionTranslator.translateExpression(while_statement.Condition);

            incrCodeDepth();
            // extract while-loop statements
            string while_loop_source_code = injectSourceCode(while_statement.Statement.ToString(), false);
            SyntaxList<StatementSyntax> statement_syntaxes = extractSyntaxList(while_loop_source_code);
            // add statements
            string content = "";
            foreach (StatementSyntax statement_syntax in statement_syntaxes)
            {
                content += addTabs() + translateStatement(statement_syntax) + "\n";
            }
            decrCodeDepth();

            return addTabs() + $"while ({condition})\n" + content + addTabs() + "end-while\n";
        }

        public static string translateForStatement(ForStatementSyntax for_statement)
        {
            string start = SubSyntaxTranslator.handleDeclaration(for_statement.Declaration);
            string stop = ExpressionTranslator.translateExpression(for_statement.Condition);
            string step = ExpressionTranslator.translateExpression(for_statement.Incrementors[0]);

            string for_string = addTabs() + $"for ({start}, {stop}, {step})\n";

            // string content = "";
            incrCodeDepth();
            // extract for-loop content (couldn't find any other way ...) -> did not try SyntaxTree of this for_loop, but idk
            string for_loop_content = injectSourceCode(for_statement.Statement.ToString(), false);
            SyntaxList<StatementSyntax> statement_syntaxes = extractSyntaxList(for_loop_content);
            for_string += addTabs() + "// for-loop content\n";
            foreach (StatementSyntax statement in statement_syntaxes)
            {
                for_string += addTabs() + translateStatement(statement) + "\n";
            }
            decrCodeDepth();
            for_string += addTabs() + "end-for\n";

            return for_string;
        }

        public static string translateIfStatement(IfStatementSyntax if_statement)
        {
            string condition = ExpressionTranslator.translateExpression(if_statement.Condition);
            string if_string = addTabs() + $"if ({condition})\n";
            incrCodeDepth();
            if_string += addTabs() + "// if-content\n";
            // extract if content
            string if_source_code = injectSourceCode(if_statement.Statement.ToString(), false);
            SyntaxList<StatementSyntax> statement_syntaxes = extractSyntaxList(if_source_code);
            // add if statements
            foreach (StatementSyntax statement_syntax in statement_syntaxes)
            {
                if_string += addTabs() + translateStatement(statement_syntax) + "\n";
            }

            // else statement
            if (if_statement.Else != null)
            {
                if_string += addTabs(_code_depth - 1) + "else\n";
                // extract else content
                string else_source_code = injectSourceCode(if_statement.Else.Statement.ToString(), false);
                SyntaxList<StatementSyntax> else_statement_syntaxes = extractSyntaxList(else_source_code);
                // add else content
                foreach (StatementSyntax statement_syntax in else_statement_syntaxes)
                {
                    if_string += addTabs() + translateStatement(statement_syntax) + "\n";
                }
            }
            decrCodeDepth();

            return if_string + addTabs() + "end-if\n";
        }

        public static string translateLocalDeclarationStatement(LocalDeclarationStatementSyntax declaration_statement)
        {
            /*Console.WriteLine("variable decl: " + declaration_statement.Declaration);
            Console.WriteLine("modifiers: " + declaration_statement.Modifiers);
            foreach (VariableDeclaratorSyntax variable_declarator_syntax in declaration_statement.Declaration.Variables)
            {
                Console.WriteLine("\tid: " + variable_declarator_syntax.Identifier);
                Console.WriteLine("\tinit: " + variable_declarator_syntax.Initializer);
            }*/

            return addTabs() + SubSyntaxTranslator.handleDeclaration(declaration_statement.Declaration);
        }
        
        public static string translateReturnStatement(ReturnStatementSyntax return_statement)
        {
            // better like this than using the '=>' (code consistency & readability)
            return addTabs() + "return(" + ExpressionTranslator.translateExpression(return_statement.Expression) + ")";
        }
    }
}