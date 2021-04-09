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

            switch (s)
            {
                // basic types
                case "int": case "float": case "bool":
                    return s;
                // double -> float
                case "double":
                    Translator.translatorWarning("translating \"double\" as \"float\"",
                        "[uncertainty] type approximation");
                    return "float";
                // void -> null
                case "void":
                    return "null";
            }

            // enumerable types
            if (s.EndsWith("[]") || s.StartsWith("List<"))
            {
                return "list"; // NOT COOL !
            }
            
            return Translator.translatorWarning("unknown type \"" + s + "\"", "[unsupported type] type approximation") + "auto";

            //throw new NotImplementedException("Unsupported type: " + s);
        }

        public static (bool, string) supportedBaseFunctions(MemberAccessExpressionSyntax member_access, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            /*Console.WriteLine("member: " + member_access);
            Console.WriteLine("\t" + member_access.Expression);
            Console.WriteLine("\t" + member_access.Name);
            Console.WriteLine("\t" + member_access.OperatorToken);
            Console.WriteLine("\t" + member_access.Expression.Kind());*/

            string func_str,
                argument,
                var_expression;
            string raw_expr = arguments[0].Expression.ToString();
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
                        func_str = "print_value_endl(";
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
                        func_str = "print_value(";
                        func_str += ExpressionTranslator.translateExpression(arguments[0].Expression);

                        return (true, func_str + ")");
                    }
                case "CopyTo":
                    // unsupported use of CopyTo function ?
                    if (arguments.Count != 2) return (false, Translator.translatorWarning("num args should be 2 for 'CopyTo' ?!", "[unsupported usage] " + member_access) + member_access);
                    // extract the various expressions
                    var_expression = ExpressionTranslator.translateExpression(member_access.Expression);
                    var target_var_expression = ExpressionTranslator.translateExpression(arguments[0].Expression);
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
                case "Add":
                    // unsupported usage
                    if (arguments.Count != 1)
                    {
                        return (false, Translator.translatorWarning("unsupported usage of 'Add' function",
                            "[unsupported usage] " + member_access) + member_access);
                    }

                    var_expression = ExpressionTranslator.translateExpression(member_access.Expression); // the list
                    argument = ExpressionTranslator.translateExpression(arguments[0].Expression); // the value

                    return (true, $"append_value({var_expression}, {argument})");
                case "RemoveAt":
                    // unsupported usage
                    if (arguments.Count != 1)
                    {
                        return (false, Translator.translatorWarning("unsupported usage of 'RemoveAt' function",
                            "[unsupported usage] " + member_access) + member_access);
                    }

                    var_expression = ExpressionTranslator.translateExpression(member_access.Expression); // the list
                    argument = ExpressionTranslator.translateExpression(arguments[0].Expression); // the index

                    return (true, $"delete_value_at({var_expression}, {argument})");
                case "Insert":
                    // unsupported usage
                    if (arguments.Count != 2)
                    {
                        return (false, Translator.translatorWarning("unsupported usage of 'Insert' function",
                            "[unsupported usage] " + member_access) + member_access);
                    }

                    var_expression = ExpressionTranslator.translateExpression(member_access.Expression); // the list
                    argument = ExpressionTranslator.translateExpression(arguments[0].Expression); // the index
                    string argument2 = ExpressionTranslator.translateExpression(arguments[1].Expression); // the value

                    return (true, $"insert_value_at({var_expression}, {argument}, {argument2})");
                default:
                    string result = ExpressionTranslator.translateExpression(member_access.Expression) + " /** -> **/ " + member_access.Name;
                    return (false, Translator.translatorWarning("unknown invocation", "[unknown invocation] " + member_access) + result);
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