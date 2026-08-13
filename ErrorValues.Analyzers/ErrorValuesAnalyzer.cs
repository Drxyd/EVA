#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ErrorValues.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ErrorValues.Analyzers;


[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class ErrorValuesAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor UnusedResultRule = new(
        id: "EVA0001",
        title: "Error result must be handled",
        messageFormat: "The return value of '{0}' returns a ref struct result and must be assigned or checked",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IncompleteSwitchRule = new(
        id: "EVA0002",
        title: "Incomplete error handling switch",
        messageFormat: "Switch statement on '{0}' does not handle variant '{1}'",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnconsumedPayloadRule = new(
        id: "EVA0003",
        title: "Error payload not consumed",
        messageFormat: "Switch case does not access variant '{0}'",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnwrappedReturnRule = new(
        id: "EVA0004",
        title: "Method returns raw error type instead of generated result struct",
        messageFormat: "ErrorValues attribute on '{0}' references '{1}', but '{1}' returns '{2}' instead of 'R{1}<{2}>'",
        category: "Reliability",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        UnusedResultRule, 
        IncompleteSwitchRule, 
        UnconsumedPayloadRule, 
        UnwrappedReturnRule,

        //// DEBUGGING
        EVAInternalLogInfo,
        ////
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze |
            GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeSwitch, OperationKind.Switch);
        context.RegisterOperationAction(AnalyzeSwitchCase, OperationKind.SwitchCase);
        context.RegisterOperationAction(AnalyzeSwitchExpression, OperationKind.SwitchExpression);
        context.RegisterOperationAction(AnalyzeSwitchExpressionArm, OperationKind.SwitchExpressionArm);
        context.RegisterSymbolAction(AnalyzeEnumSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeEnumSymbol(SymbolAnalysisContext context)
    {
        var enum_symbol = (INamedTypeSymbol)context.Symbol;
        if (enum_symbol.TypeKind != TypeKind.Enum)
            return;

        AttributeData? error_state_attribute = enum_symbol.GetAttributes()
            .FirstOrDefault(attribute =>
            attribute.AttributeClass?.ToDisplayString() == typeof(ErrorValuesAttribute).FullName);

        if (error_state_attribute == null 
            || error_state_attribute.ConstructorArguments.IsEmpty)
            return;

        string? method_name = error_state_attribute.ConstructorArguments[0]
            .Value as string;
        if (string.IsNullOrEmpty(method_name))
            return;

        INamedTypeSymbol? containing_type = enum_symbol.ContainingType;
        if (containing_type == null)
            return; // Case where enum is in namespace scope and function has containing type requires that the function be public (Not supported)

        IMethodSymbol? target_method = containing_type.GetMembers(method_name!)
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (target_method == null)
            return; // Error: method doesn't exist

        string expected_return_type_name = $"R{enum_symbol.Name}";
        ITypeSymbol return_type = target_method.ReturnType;

        bool isWrapped = (return_type.Name == expected_return_type_name 
            && return_type.IsRefLikeType) || return_type.Name == typeof(IEVA<>).GetCleanName();

        if (!isWrapped)
        {
            Location location = enum_symbol.Locations.FirstOrDefault() ?? Location.None;
            Diagnostic diagnostic = Diagnostic.Create(UnwrappedReturnRule, location, enum_symbol.Name, method_name, return_type.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void AnalyzeSwitchExpression(OperationAnalysisContext context)
    {
        var switch_expression = (ISwitchExpressionOperation)context.Operation;
        ITypeSymbol? value_type = switch_expression.Value.Type;
        if (value_type is null)
            return;

        INamedTypeSymbol? enum_type = GetTargetEnumSymbol(value_type);
        if (enum_type is null)
            return;

        var enum_variants = enum_type.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsImplicitlyDeclared)
            .ToImmutableDictionary(f => f.Name, f => f);

        bool has_discard = switch_expression.Arms.Any(
            a => a.Guard is IDiscardOperation);

        if (has_discard)
            return;

        HashSet<string> handled_variants = [];
        foreach (ISwitchExpressionArmOperation arm in switch_expression.Arms)
        {
            IEnumerable<string> variant_names = GetVariantFieldSymbolsFromPattern(arm.Pattern)
                .Select(symbol => symbol.Name);
            foreach(string name in variant_names)
            {
                if (name == null)
                    continue;
                IFieldSymbol field_symbol = enum_variants[name];
                handled_variants.Add(name);
            }
        }

        foreach (string variant in enum_variants.Keys)
        {
            if(!handled_variants.Contains(variant))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    IncompleteSwitchRule,
                    switch_expression.Syntax.GetLocation(),
                    enum_type.Name,
                    variant
                ));
            }
        }
    }

    private void AnalyzeSwitchExpressionArm(OperationAnalysisContext context)
    {
        var arm = (ISwitchExpressionArmOperation)context.Operation;

        IEnumerable<IFieldSymbol> variant_symbols = GetVariantFieldSymbolsFromPattern(arm.Pattern);

        IEnumerable<IInvocationOperation> invocations = arm
            .Descendants()
            .OfType<IInvocationOperation>();

        foreach (IFieldSymbol symbol in variant_symbols)
        {
            bool has_called_accessor = invocations.Any(inv =>
                inv.TargetMethod.Name == symbol.Name
            );

            if (!has_called_accessor && VariantHasPayload(symbol))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        UnconsumedPayloadRule,
                        arm.Syntax.GetLocation(),
                        symbol.Name
                ));
            }
        }
    }

    private void AnalyzeSwitch(OperationAnalysisContext context)
    {
        var switch_op = (ISwitchOperation)context.Operation;

        ITypeSymbol? value_type = switch_op.Value.Type;
        if (value_type is null)
            return;

        INamedTypeSymbol? enum_type = GetTargetEnumSymbol(value_type);
        if (enum_type is null)
            return;

        var enum_attr = enum_type.GetAttributes().Where(attribute => attribute.AttributeClass?.Name == typeof(ErrorValuesAttribute).Name);

        if (enum_attr.Count() == 0)
            return;

        var enumVariants = enum_type.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsImplicitlyDeclared)
            .Select(f => f.Name)
            .ToImmutableHashSet();

        bool has_default = switch_op.Cases.Any(
            c => c.Clauses.Any(clause => clause.CaseKind == CaseKind.Default));
        if (has_default)
            return;

        var handled_variants = new HashSet<string>();
        foreach (ISwitchCaseOperation case_op in switch_op.Cases)
        {
            foreach (ICaseClauseOperation clause in case_op.Clauses)
            {
                if (clause is ISingleValueCaseClauseOperation single_value
                    && single_value.Value is IFieldReferenceOperation field_ref)
                {
                    handled_variants.Add(field_ref.Field.Name);
                }

                else if (clause is IRelationalCaseClauseOperation relation)
                {
                    var field_refs = relation
                        .Descendants()
                        .Where(child => 
                            child is IFieldReferenceOperation field_reference && 
                            field_reference.Field.ContainingType?.Name == enum_type.Name)
                        .Cast<IFieldReferenceOperation>();

                    foreach (IFieldReferenceOperation field_reference in field_refs)
                    {
                        handled_variants.Add(field_reference.Field.Name);
                    }
                }
                
                else if (clause is IPatternCaseClauseOperation pattern)
                {

                    var field_refs = pattern
                        .Descendants()
                        .Where(child => 
                            child is IFieldReferenceOperation field_reference && 
                            field_reference.Field.ContainingType?.Name == enum_type.Name)
                        .Cast<IFieldReferenceOperation>(); ;

                    foreach (IFieldReferenceOperation field_reference in field_refs)
                    {
                        handled_variants.Add(field_reference.Field.Name);
                    }
                }
            }
        }

        foreach (string variant in enumVariants)
        {
            if (!handled_variants.Contains(variant))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        IncompleteSwitchRule,
                        switch_op.Syntax.GetLocation(),
                        enum_type.Name,
                        variant
                ));
            }
        }
    }

    private void AnalyzeSwitchCase(OperationAnalysisContext context)
    {
        ISwitchCaseOperation switch_case = (ISwitchCaseOperation)context.Operation;
        ISwitchOperation? switch_op = (ISwitchOperation?) switch_case.Parent;

        if (switch_op == null)
            return;

        ITypeSymbol? value_type = switch_op.Value.Type;
        if (value_type is null)
            return;

        INamedTypeSymbol? enum_type = GetTargetEnumSymbol(value_type);
        if (enum_type is null)
            return;

        var enum_attr = enum_type.GetAttributes().Where(attribute => attribute.AttributeClass?.Name == typeof(ErrorValuesAttribute).Name);

        if (enum_attr.Count() == 0)
            return;

        IEnumerable<IInvocationOperation> invocations = switch_case
            .Descendants()
            .OfType<IInvocationOperation>();

        foreach (ICaseClauseOperation clause in switch_case.Clauses)
        {
            var field_symbols = clause.Descendants()
                .Where(descendant => 
                    descendant is IFieldReferenceOperation)
                .Select(field =>
                    ((IFieldReferenceOperation)field).Field);

            foreach(IFieldSymbol symbol in field_symbols)
            {
                string variant_name = symbol.Name;

                bool accessor_called = invocations.Where(inv =>
                        inv.TargetMethod.Name == variant_name
                    ).Count() > 0;

                if (VariantHasPayload(symbol) && !accessor_called)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            UnconsumedPayloadRule,
                            switch_case.Syntax.GetLocation(),
                            variant_name
                    ));
                }
            }
        }
    }

    private void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;

        ITypeSymbol returnType = invocation.TargetMethod.ReturnType;
        
        if (!IsGeneratedResultStruct(returnType))
            return;

        // Walk up any implicit conversions wrapping the invocation
        IOperation current = invocation;
        while (current.Parent is IConversionOperation conversion)
        {
            current = conversion;
        }

        if (current.Parent is IExpressionStatementOperation)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                UnusedResultRule,
                invocation.Syntax.GetLocation(),
                invocation.TargetMethod.Name
            ));
        }
    }

    private static IEnumerable<IFieldSymbol> GetVariantFieldSymbolsFromPattern(IPatternOperation pattern)
    {
        switch (pattern) // overcomplicated, just query descendants
        {
            // Match base
            case IConstantPatternOperation { Value: IFieldReferenceOperation fieldRef }:
                yield return fieldRef.Field;
                break;

            // Recurse
            case IBinaryPatternOperation binary when binary.OperatorKind == BinaryOperatorKind.Or:
                foreach (IFieldSymbol symbol in GetVariantFieldSymbolsFromPattern(binary.LeftPattern))
                    yield return symbol;
                foreach (IFieldSymbol symbol in GetVariantFieldSymbolsFromPattern(binary.RightPattern))
                    yield return symbol;
                break;

            default:
                yield break;
        }
    }

    private bool VariantHasPayload(IFieldSymbol symbol)
    {
        return symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.IsGenericType == true
            && attribute.AttributeClass.ConstructedFrom.MetadataName == typeof(PayloadAttribute<>).Name
        );
    }

    private static IEnumerable<string?> GetVariantNameFromClause(ICaseClauseOperation clause)
    {
        var field_names = clause.Descendants().Where(descendant => descendant is IFieldReferenceOperation).Select(field => 
            ((IFieldReferenceOperation)field).Field.Name);
        return field_names;
    }

    private INamedTypeSymbol? GetTargetEnumSymbol(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum)
            return (INamedTypeSymbol)type;
        return null;
    }

    private static bool IsGeneratedResultStruct(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Interface && type.Name == typeof(IEVA<>).GetCleanName())
            return true;

        if (!type.IsRefLikeType)
            return false;

        type = type.OriginalDefinition;

        ImmutableArray<AttributeData> attributes  = type.GetAttributes();
        AttributeData? attr_data = attributes.FirstOrDefault(attribute => 
            attribute?.AttributeClass?.ToDisplayString() == typeof(GenerationAttribute).FullName);

        return (attr_data != null);
    }
}


public partial class ErrorValuesAnalyzer
{
    //// DEBUGGING
    private static readonly DiagnosticDescriptor EVAInternalLogInfo = new(
    id: "LOG001",
    title: "Analyzer Debug Log",
    messageFormat: "Internal Log: {0}",
    category: "Debug",
    defaultSeverity: DiagnosticSeverity.Warning,
    isEnabledByDefault: true);

    public void SymbolLog(SymbolAnalysisContext context, string message)
    {
        Location location = context.Symbol.Locations.FirstOrDefault() ?? Location.None;
        Diagnostic diagnostic = Diagnostic.Create(EVAInternalLogInfo, location, message);
        context.ReportDiagnostic(diagnostic);
    }

    public void SymbolRaiseDiagnostic(SymbolAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params string[] parameters)
    {
        Diagnostic diagnostic = Diagnostic.Create(descriptor, location, parameters);
        context.ReportDiagnostic(diagnostic);
    }

    public void OperationLog(OperationAnalysisContext context, string message)
    {
        Location location = context.Operation.Syntax.GetLocation() ?? Location.None;
        Diagnostic diagnostic = Diagnostic.Create(EVAInternalLogInfo, location, message);
        context.ReportDiagnostic(diagnostic);
    }

    // This should be its own project
    // Additionally write logs to an analyzer debug log file
    ////

}