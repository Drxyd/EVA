using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ErrorValuesCodeFixes)), Shared]
public class ErrorValuesCodeFixes : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => throw new NotImplementedException();

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override FixAllProvider GetFixAllProvider()
    {
        return base.GetFixAllProvider();
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return base.ToString();
    }

    protected override CodeActionRequestPriority ComputeRequestPriority()
    {
        return base.ComputeRequestPriority();
    }
}