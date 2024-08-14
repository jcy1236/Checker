using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static List<string> errors = new List<string>();

    static async Task Main(string[] args)
    {
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(@"C:\Users\jcy12\source\repos\Checker\testScript\testScript.csproj");

        var literals = new List<string>();

        // 명령어 목록 정의
        var commandNames = new HashSet<string> {
            "IO_READ", "IO_READ_DIGITAL", "IO_READ_ANALOG", "IO_WRITE",
            "PARAM_READ_ENUM", "PARAM_READ_DIGITAL", "PARAM_READ_ANALOG", "PARAM_READ_STRING",
            "PARAM_WRITE", "PARAM_WRITE_ENU", "PARAM_WRITE_DIGITAL", "PARAM_WRITE_ANALOG", "PARAM_WRITE_STRING",
        };

        var compilation = await project.GetCompilationAsync();
        foreach (var document in project.Documents)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync();
            var semanticModel = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);
            var invocations = syntaxRoot.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var expression = invocation.Expression;

                if (expression is MemberAccessExpressionSyntax memberAccess &&
                    commandNames.Contains(memberAccess.Name.Identifier.Text))
                {
                    ProcessArguments(invocation, literals, semanticModel, document.FilePath);
                }
                else if (expression is IdentifierNameSyntax identifierName &&
                    commandNames.Contains(identifierName.Identifier.Text))
                {
                    ProcessArguments(invocation, literals, semanticModel, document.FilePath);
                }
            }
        }

        foreach (var error in errors)
        {
            Console.WriteLine(error);
        }
    }

    static void ProcessArguments(InvocationExpressionSyntax invocation, List<string> literals, SemanticModel semanticModel, string filePath)
    {
        var argumentList = invocation.ArgumentList;
        if (argumentList != null && argumentList.Arguments.Count > 0)
        {
            var argument = argumentList.Arguments[0].Expression;
            string? literalValue = null;

            if (argument is LiteralExpressionSyntax literal)
            {
                literalValue = literal.Token.ValueText;
                literals.Add(literalValue);
            }
            else if (argument is IdentifierNameSyntax identifier)
            {
                var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;

                if (symbol != null)
                {
                    literalValue = GetConstantValue(symbol, semanticModel);
                    //string? constantValue = GetConstantValue(symbol, semanticModel);

                    if (literalValue != null)
                    {
                        literals.Add(literalValue);
                    }
                }
            }

            var predefinedVariables = new HashSet<string> { "PM1.dBC.SV.Set", "PM1.dsht.Status" /* 필요한 값 추가 */ };

            if (literalValue != null && !predefinedVariables.Contains(literalValue))
            {
                var location = invocation.GetLocation();
                var lineSpan = location.GetLineSpan();
                var lineNumber = lineSpan.StartLinePosition.Line + 1;
                var columnNumber = lineSpan.StartLinePosition.Character + 1;
                errors.Add($"[VX0001] Error in file '{filePath}', line {lineNumber}, column {columnNumber}: " +
                    $"'{literalValue}' is not a valid predefined variable.");
            }
        }
    }

    static string? GetConstantValue(ISymbol symbol, SemanticModel semanticModel)
    {
        if (symbol is IFieldSymbol fieldSymbol && fieldSymbol.HasConstantValue)
        {
            return fieldSymbol.ConstantValue as string;
        }

        if (symbol is IFieldSymbol globalFieldSymbol && globalFieldSymbol.IsStatic && globalFieldSymbol.HasConstantValue)
        {
            return globalFieldSymbol.ConstantValue as string;
        }

        if (symbol.DeclaringSyntaxReferences.Length > 0)
        {
            var declarationSyntax = symbol.DeclaringSyntaxReferences[0].GetSyntax() as VariableDeclaratorSyntax;
            if (declarationSyntax != null)
            {
                var valueSyntax = declarationSyntax.Initializer?.Value;

                if (valueSyntax != null)
                {
                    // 상수로 평가할 수 있는지 확인
                    if (valueSyntax is LiteralExpressionSyntax literal)
                    {
                        string? constantValue = literal.Token.ValueText;
                        return constantValue;
                    }
                    else
                    {
                        // 상수로 평가되지 않는 경우 추가 처리
                        Console.WriteLine($"Warning: The expression '{valueSyntax}' is not a constant.");
                    }
                }
            }
        }

        return null;
    }

}
