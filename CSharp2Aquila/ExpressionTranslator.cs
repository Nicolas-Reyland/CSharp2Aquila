using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharp2Aquila
{
    public static class ExpressionTranslator
    {
        private static Dictionary<string, string> _default_values_per_type = new Dictionary<string, string>
        {
            {
                "int", "0"
            },
            {
                "list", "[]"
            },
            {
                "bool", "false"
            },
            {
                "float", "0f"
            },
        };

        public static string translateToken(SyntaxToken token, bool var_prefix = true)
        {
            /*Console.WriteLine("token: " + token);
            Console.WriteLine("\t" + token.Text);
            Console.WriteLine("\t" + token.Value);
            Console.WriteLine("\t" + token.Value.GetType());
            Console.WriteLine("\t" + token.ValueText);
            Console.WriteLine("\t" + token.TrailingTrivia);
            Console.WriteLine("\t" + token.LeadingTrivia);*/

            if (token.Value == null)
            {
                return token.ToString();
            }

            if (token.Value is int ||
                !var_prefix ||
                token.ValueText == "true" ||
                token.ValueText == "false")
            {
                return token.ValueText;
            }

            return @"$" + token; // assume this is a variable access ?
        }
        
        public static string translateExpression(ExpressionSyntax expression)
        {
            /*Console.WriteLine("expresion: " + expression);
            Console.WriteLine("\t" + expression.Kind());
            Console.WriteLine("\t" + expression.GetType());*/
            // Console.WriteLine("\t" + );
            // Console.WriteLine("\t" + );
            // Console.WriteLine("\t" + );

            switch (expression)
            {
                case IdentifierNameSyntax identifier:
                    return translateIdentifierName(identifier);
                case BinaryExpressionSyntax binary_expression:
                    return translateBinaryExpression(binary_expression);
                case AssignmentExpressionSyntax assignment_expression:
                    return translateAssignmentExpression(assignment_expression);
                case ElementAccessExpressionSyntax element_access:
                    return translateElementAccessExpression(element_access);
                case LiteralExpressionSyntax literal_expression:
                    return translateLiteralExpression(literal_expression);
                case ImplicitArrayCreationExpressionSyntax implicit_array_creation:
                    return translateImplicitArrayCreationExpression(implicit_array_creation);
                case ArrayCreationExpressionSyntax array_creation:
                    return translateArrayCreationExpression(array_creation);
                case MemberAccessExpressionSyntax member_access:
                    return translateMemberAccessExpression(member_access);
                case ParenthesizedExpressionSyntax parenthesized_expression:
                    return translateParenthesizedExpression(parenthesized_expression);
                case PrefixUnaryExpressionSyntax prefix_unary_expression_syntax: // e.g. "-6"
                    return matchOperatorToken(prefix_unary_expression_syntax.OperatorToken) +
                           translateExpression(prefix_unary_expression_syntax.Operand);
                default:
                    Console.WriteLine("[!] Unsupported expression type: " + expression + "\n\tkind: " + expression.Kind() + "\n\t" + expression.GetType());
                    return expression.ToString();
            }
        }

        private static string translateIdentifierName(IdentifierNameSyntax identifier) // maybe change type to SimpleNameSyntax ?
        {
            // maybe there will be more to this, so let's keep it in a function
            return translateToken(identifier.Identifier);
        }

        private static string translateBinaryExpression(BinaryExpressionSyntax binary_expression)
        {
            string left = translateExpression(binary_expression.Left);
            string operator_ = matchOperatorToken(binary_expression.OperatorToken);
            string right = translateExpression(binary_expression.Right);

            return $"{left} {operator_} {right}";
        }

        public static string translateAssignmentExpression(AssignmentExpressionSyntax assignment)
        {
            string left = translateExpression(assignment.Left);
            string right = translateExpression(assignment.Right);
            string operator_ = assignment.OperatorToken.ValueText; // better .Text ?
            //Console.WriteLine(assignment.OperatorToken);

            return $"{left} {operator_} {right}";
        }

        private static string translateElementAccessExpression(ElementAccessExpressionSyntax element_access)
        {
            /*Console.WriteLine("element_access: " + element_access);
            Console.WriteLine("\t" + element_access.Expression);
            Console.WriteLine("\t" + element_access.Expression.Kind());
            Console.WriteLine("\t" + element_access.Expression.GetType());
            Console.WriteLine("\t" + element_access.ArgumentList.Arguments.Count);*/
            // Console.WriteLine("\t" + );

            // multiple args in accessor ? (e.g. a[b, c])
            if (element_access.ArgumentList.Arguments.Count > 1)
            {
                Console.WriteLine("[!] Element access with multiple arguments is unsupported");
                return "/** multiple arguments for element access unsupported **/ " + element_access;
            }
            // normal element access
            string accessed = translateExpression(element_access.Expression);
            string accessor = translateExpression(element_access.ArgumentList.Arguments[0].Expression);

            return $"{accessed}[{accessor}]";
        }

        private static string translateLiteralExpression(LiteralExpressionSyntax literal_expression)
        {
            return translateToken(literal_expression.Token);
        }

        private static string translateImplicitArrayCreationExpression(
            ImplicitArrayCreationExpressionSyntax implicit_array_creation)
        {
            /*Console.WriteLine("implicit array creation: " + implicit_array_creation);
            Console.WriteLine("\t" + implicit_array_creation.Initializer);
            Console.WriteLine("\t" + implicit_array_creation.Initializer.Expressions);
            Console.WriteLine("\t" + implicit_array_creation.Initializer.Expressions.Count);
            Console.WriteLine("\t" + implicit_array_creation.Initializer.Expressions[0]);*/

            string list_value = "";
            int n = implicit_array_creation.Initializer.Expressions.Count; // number of elements in the array
            for (int i = 0; i < n; i++)
            {
                list_value += translateExpression(implicit_array_creation.Initializer.Expressions[i]);
                if (i != n - 1)
                {
                    list_value += ", ";
                }
            }

            return "[" + list_value + "]";
        }
        
        private static string translateArrayCreationExpression(ArrayCreationExpressionSyntax array_creation)
        { // e.g. int[] x = new int[5];
            /*Console.WriteLine("array creation: " + array_creation);
            Console.WriteLine("\t" + array_creation.Type);
            Console.WriteLine("\t" + array_creation.NewKeyword);
            Console.WriteLine("\t" + array_creation.Initializer);
            Console.WriteLine("\t" + array_creation.Initializer?.Expressions.Count);
            
            Console.WriteLine("\t" + array_creation.Type.ElementType);
            Console.WriteLine("\t" + array_creation.Type.RankSpecifiers[0]);
            Console.WriteLine("\t" + array_creation.Type.RankSpecifiers[0].Rank);
            Console.WriteLine("\t" + array_creation.Type.RankSpecifiers[0].Sizes.Count);
            Console.WriteLine("\t" + array_creation.Type.RankSpecifiers[0].Sizes[0]);*/
            // Console.WriteLine("\t" + );

            string type_string = SubSyntaxTranslator.translateType(array_creation.Type.ElementType);
            if (array_creation.Type.RankSpecifiers.Count == 0 || // e.g. string[] (possible ?)
                array_creation.Type.RankSpecifiers[0].Sizes.Count == 0 || // same as previous
                !_default_values_per_type.ContainsKey(type_string)) // unrecognized type
            {
                return Translator.translatorWarning("unsupported rank or type", "[unsupported usage] " + array_creation) + array_creation;
            }

            string default_value = _default_values_per_type[type_string];
            int rank = int.Parse(array_creation.Type.RankSpecifiers[0].Sizes[0].ToString());

            string list_value = "";
            for (int i = 0; i++ < rank;)
            {
                list_value += default_value;
                if (i != rank)
                {
                    list_value += ", ";
                }
            }

            return "[" + list_value + "]";
        }

        private static string translateMemberAccessExpression(MemberAccessExpressionSyntax member_access)
        {
            switch (member_access.Name.ToString())
            {
                case "Length": case "Count":
                    return $"length({translateExpression(member_access.Expression)})";
                default:
                    Console.WriteLine("[!] Unsupported member access: " + member_access);
                    return Translator.translatorWarning("unsupported member access keyword", "[unsupported usage] " + member_access) + member_access;
            }
        }

        private static string translateParenthesizedExpression(ParenthesizedExpressionSyntax parenthesized_expression)
        {
            // Console.WriteLine("in (): " + parenthesized_expression + " -> " + parenthesized_expression.Expression);
            return "(" + translateExpression(parenthesized_expression.Expression) + ")";
        }

        // misc
        public static string translateInvocationExpression(InvocationExpressionSyntax invocation)
        {
            /*Console.WriteLine("invocation expression: " + invocation.Expression.Kind());
            Console.WriteLine("invocation expression: " + invocation.Expression.GetType());
            Console.WriteLine("\t" + invocation.Expression + "\n");*/
            string function_name = "";
            switch (invocation.Expression)
            {
                case IdentifierNameSyntax identifier:
                    function_name = translateToken(identifier.Identifier, false);
                    break;
                case MemberAccessExpressionSyntax member_access:
                    bool special_case;
                    string function;
                    (special_case, function) = SubSyntaxTranslator.supportedBaseFunctions(member_access, invocation.ArgumentList.Arguments);
                    if (special_case)
                    {
                        return function; // as-is
                    }
                    else
                    {
                        function_name = function;
                    }
                    break;
            }
            //
            string arg_list = SubSyntaxTranslator.translateSeparatedSyntaxList(invocation.ArgumentList.Arguments);

            return $"{function_name}({arg_list})";
        }

        private static string matchOperatorToken(SyntaxToken operator_)
        {
            switch (operator_.ValueText)
            {
                case "+": case "-": case "/": case "*": case "%": case "<": case ">": case "^":
                    return operator_.ValueText;
                case "<=":
                    return @"{";
                case ">=":
                    return @"}";
                case "==":
                    return "~";
                case "!=":
                    return ":";
                case "||":
                    return "|";
                case "&&":
                    return "&";
                default:
                    Console.WriteLine("[!] unknown operator: " + operator_.ValueText);
                    return Translator.translatorWarning("unknown operator", "[unsupported usage] " + operator_.ValueText) + operator_.ValueText;
            }
        }
    }
}