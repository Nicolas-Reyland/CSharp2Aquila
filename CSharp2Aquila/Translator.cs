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
        private static int _code_depth; // = 0
        private static string addTabs(int n = -1) => new string('\t', n == -1 ? _code_depth : n);
        private static void incrCodeDepth() => _code_depth++;
        private static void decrCodeDepth() => _code_depth = _code_depth == 0 ? 0 : _code_depth - 1;

        public static string translateFromSourceCode(string src_code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(src_code);
            return "/** Automatic translation of CSharp source code to Aquila by https://github.com/Nicolas-Reyland/CSharp2Aquila **/\n\n" + translateAll(tree);
        }

        private static string translateAll(SyntaxTree tree)
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

        private static string injectSourceCode(string source_code, bool add_curly_braces) =>
@"namespace CodeInjection
{
    class Program
    {
        static void Main(string[] args)
" + (add_curly_braces ? "{" : "") + source_code + (add_curly_braces ? "}" : "") + @"
    }
}";

        private static SyntaxList<StatementSyntax> extractSyntaxList(string source_code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source_code);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            var name_space = (NamespaceDeclarationSyntax) root.Members[0];
            var program_class = (ClassDeclarationSyntax) name_space.Members[0];
            var method = (MethodDeclarationSyntax) program_class.Members[0];
            if (method.Body == null) throw new Exception("Body is null for code extraction");
            return method.Body.Statements;
        }

        private static string translateMethodDeclaration(MethodDeclarationSyntax method_declaration)
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
                case ReturnStatementSyntax return_statement_syntax:
                    return translateReturnStatement(return_statement_syntax);
                default:
                    translatorWarning("Unsupported statement", "[unsupported statement] " + statement_syntax);
                    if (Program.verbose) Console.WriteLine("[!] " + statement_syntax.GetType() + " is not supported.\n\tkind: " + statement_syntax.Kind());
                    return translatorWarning("Unknown statement ", statement_syntax.ToString()) + statement_syntax;
            }
        }

        // Statements -> these functions usually add a '\n' at the end
        private static string translateExpressionStatement(ExpressionStatementSyntax expression_statement)
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
                    translatorWarning("Unsupported expresion", "[unsupported usage] " + expression_statement.Expression);
                    if (Program.verbose) Console.WriteLine("[!] Unsupported expression:" +
                                                           "\n\traw: " + expression_statement.Expression +
                                                           "\n\ttype: " + expression_statement.Expression.GetType() +
                                                           "\n\tkind: " + expression_statement.Expression.Kind());
                    break;
            }

            return addTabs() + expression_statement + " // RAW (untouched)\n";
        }

        private static string translateWhileStatement(WhileStatementSyntax while_statement)
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
                content += translateStatement(statement_syntax) + "\n";
            }
            decrCodeDepth();

            return addTabs() + $"while ({condition})\n" + content + addTabs() + "end-while\n";
        }

        private static string translateForStatement(ForStatementSyntax for_statement)
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
            foreach (StatementSyntax statement in statement_syntaxes)
            {
                for_string += translateStatement(statement) + "\n";
            }
            decrCodeDepth();
            for_string += addTabs() + "end-for\n";

            return for_string;
        }

        private static string translateIfStatement(IfStatementSyntax if_statement)
        {
            string condition = ExpressionTranslator.translateExpression(if_statement.Condition);
            string if_string = addTabs() + $"if ({condition})\n";
            incrCodeDepth();
            // extract if content
            string if_source_code = injectSourceCode(if_statement.Statement.ToString(), false);
            SyntaxList<StatementSyntax> statement_syntaxes = extractSyntaxList(if_source_code);
            // add if statements
            foreach (StatementSyntax statement_syntax in statement_syntaxes)
            {
                if_string += translateStatement(statement_syntax) + "\n";
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
                    if_string += translateStatement(statement_syntax) + "\n";
                }
            }
            decrCodeDepth();

            return if_string + addTabs() + "end-if\n";
        }

        private static string translateLocalDeclarationStatement(LocalDeclarationStatementSyntax declaration_statement)
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

        private static string translateReturnStatement(ReturnStatementSyntax return_statement)
        {
            // better like this than using the '=>' (code consistency & readability)
            return addTabs() + "return(" + ExpressionTranslator.translateExpression(return_statement.Expression) + ")";
        }

        public static string translatorWarning(string msg, string warn_msg)
        {
            if (Program.verbose) Console.WriteLine("Warning: " + msg + " :: " + warn_msg);
            return "/** TRANSLATOR WARNING: " + msg + " **/ ";
        }
    }
}