using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharp2Aquila
{
    public static class SubSyntaxTranslator
    {
        // Other (more global objects) -> these functions usually don't add a '\n' at the end
        public static string handleDeclaration(VariableDeclarationSyntax variable_declaration)
        {
            string type_string = translateType(variable_declaration.Type);
            string var_name = ExpressionTranslator.translateToken(variable_declaration.Variables[0].Identifier, false);
            string value = ExpressionTranslator.translateExpression(variable_declaration.Variables[0].Initializer?.Value);

            return $"decl {type_string} {var_name} ({value})";
        }

        public static string translateType(TypeSyntax type_syntax)
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

        public static (bool, string) supportedBaseFunctions(MemberAccessExpressionSyntax member_access, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            /*Console.WriteLine("member: " + member_access);
            Console.WriteLine("\t" + member_access.Expression);
            Console.WriteLine("\t" + member_access.Name);
            Console.WriteLine("\t" + member_access.OperatorToken);
            Console.WriteLine("\t" + member_access.Expression.Kind());*/

            string func_str, raw_expr = arguments[0].Expression.ToString();
            switch (member_access.Name.ToString())
            {
                case "WriteLine":
                    // special case: remove double quotes
                    if (arguments.Count > 1)
                    {
                        return (false, Translator.translatorWarning("only index '0' is supported for the function 'CopyTo'", "") + member_access);
                    }

                    if (raw_expr.StartsWith("\""))
                    {
                        if (!raw_expr.EndsWith("\""))
                        {
                            return (false, Translator.translatorWarning("starting with double quotes, but not ending with double quotes ... (unsupported)", "[unsupported usage] " + member_access) + member_access);
                        }
                        func_str = "print_str_endl(";
                        func_str += raw_expr.Substring(1, raw_expr.Length - 2); // remove first and last chars ('"' and '"')

                        return (true, func_str + ")");
                    }
                    else
                    {
                        func_str = Translator.translatorWarning("should add manually a 'print_endl()' call after this", "[missing element]") + " print(";
                        func_str += ExpressionTranslator.translateExpression(arguments[0].Expression);

                        return (true, func_str + ")");
                    }
                case "Write":
                    // same as previous
                    if (arguments.Count > 1)
                    {
                        return (false, Translator.translatorWarning("only index '0' is supported for the function 'CopyTo'", "[unsupported usage]") + member_access);
                    }

                    if (raw_expr.StartsWith("\""))
                    {
                        if (!raw_expr.EndsWith("\""))
                        {
                            return (false, Translator.translatorWarning("starting with double quotes, but not ending with double quotes ... (unsupported)", "[unsupported usage] " + member_access) + member_access);
                        }
                        func_str = "print_str(";
                        func_str += raw_expr.Substring(1, raw_expr.Length - 2); // remove first and last chars ('"' and '"')

                        return (true, func_str + ")");
                    }
                    else
                    {
                        func_str = "print(";
                        func_str += ExpressionTranslator.translateExpression(arguments[0].Expression);

                        return (true, func_str + ")");
                    }
                case "CopyTo":
                    // unsupported use of CopyTo function ?
                    if (arguments.Count != 2) return (false, Translator.translatorWarning("num args should be 2 for 'CopyTo' ?!", "[unsupported usage] " + member_access) + member_access);
                    // extract the various expressions
                    string var_expression = ExpressionTranslator.translateExpression(member_access.Expression);
                    string target_var_expression = ExpressionTranslator.translateExpression(arguments[0].Expression);
                    string index = ExpressionTranslator.translateExpression(arguments[1].Expression);
                    // custom index ?
                    if (index != "0")
                    {
                        // manual copy of the elements ? idk
                        return (false, Translator.translatorWarning("only index '0' is supported for the function 'CopyTo'", "[unsupported usage] " + member_access) + member_access);
                    }
                    // copy the whole list
                    func_str = $"{target_var_expression} = copy_list({var_expression})";
                    
                    return (true, func_str);
                default:
                    return (false, member_access.ToString());
            }
        }

        public static string translateSeparatedSyntaxList(SeparatedSyntaxList<ArgumentSyntax> args)
        {
            string args_string = "";
            foreach (ArgumentSyntax argument_syntax in args)
            {
                string arg_string = ExpressionTranslator.translateExpression(argument_syntax.Expression);
                if (arg_string.EndsWith("\n"))
                    arg_string = args_string.Substring(0, arg_string.Length - 1); // remove the last '\n'
                args_string += arg_string + ", ";
            }

            return args.Count > 0 ? args_string.Substring(0, args_string.Length - 2) : args_string; // - 2 : remove last ", "
        }
    }
}