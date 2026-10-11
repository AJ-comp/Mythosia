using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Mythosia.Integrity;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
[Trait("Category", "Compatibility")]
public sealed class IntegrityPublicApiTests
{
    private const string ApprovedResourceName = "Mythosia.Test.Integrity.PublicApi.approved.txt";

    [Fact]
    public void PublicApi_MatchesApprovedSnapshot()
    {
        using Stream resource = typeof(IntegrityPublicApiTests).Assembly
            .GetManifestResourceStream(ApprovedResourceName)
            ?? throw new InvalidOperationException($"Missing approved API resource: {ApprovedResourceName}.");
        using var reader = new StreamReader(resource);
        string[] approved = reader.ReadToEnd()
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] actual = IntegrityPublicApiSnapshot.Create(typeof(ErrorDetection).Assembly);

        string[] missing = approved.Except(actual, StringComparer.Ordinal).ToArray();
        string[] added = actual.Except(approved, StringComparer.Ordinal).ToArray();
        Assert.True(missing.Length == 0 && added.Length == 0,
            "Mythosia.Integrity public API differs from the reviewed snapshot."
            + "\nMissing or changed approved entries:\n" + string.Join("\n", missing.Select(line => "- " + line))
            + "\nAdded or changed current entries:\n" + string.Join("\n", added.Select(line => "+ " + line))
            + "\nReview the API change explicitly; this test never updates PublicApi.approved.txt.");
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("tr-TR")]
    public void Snapshot_IsIndependentOfCulture(string cultureName)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        string[] expected = IntegrityPublicApiSnapshot.Create(typeof(ErrorDetection).Assembly);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            Assert.Equal(expected, IntegrityPublicApiSnapshot.Create(typeof(ErrorDetection).Assembly));
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}

// Snapshot the externally accessible declarations, rather than assembly versions,
// references, implementation flags or interfaces inherited from the runtime's Enum.
// The approved file is reviewed source data; there is deliberately no updater.
internal static class IntegrityPublicApiSnapshot
{
    private const BindingFlags DeclaredMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
        | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const char GenericArityMarker = '\u0060';

    internal static string[] Create(Assembly assembly)
    {
        var entries = new List<string>();
        foreach (Type type in assembly.GetTypes().Where(IsAccessible))
        {
            entries.Add(DescribeType(type));
            foreach (MemberInfo member in type.GetMembers(DeclaredMembers))
            {
                string? description = DescribeMember(member);
                if (description != null)
                    entries.Add($"member {TypeName(type)} {description} attributes={Attributes(member.GetCustomAttributesData())}");
            }
        }
        return entries.Order(StringComparer.Ordinal).ToArray();
    }

    private static bool IsAccessible(Type type)
    {
        if (!type.IsNested) return type.IsPublic;
        return (type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem)
            && IsAccessible(type.DeclaringType!);
    }

    private static bool IsAccessible(MethodBase method)
        => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

    private static bool IsAccessible(FieldInfo field)
        => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

    private static string DescribeType(Type type)
    {
        string kind = type.IsEnum ? "enum" : type.IsInterface ? "interface"
            : type.IsValueType ? "struct" : "class";
        IEnumerable<Type> inheritedInterfaces = type.BaseType?.GetInterfaces() ?? [];
        string interfaces = string.Join(",", type.GetInterfaces().Except(inheritedInterfaces)
            .Select(TypeName).Order(StringComparer.Ordinal));
        string enumBase = type.IsEnum ? $" underlying={TypeName(Enum.GetUnderlyingType(type))}" : "";
        return $"type {TypeName(type)} kind={kind} visibility={TypeVisibility(type)}"
            + $" abstract={type.IsAbstract} sealed={type.IsSealed} base={TypeName(type.BaseType)}"
            + $" interfaces=[{interfaces}]{enumBase}{GenericParameters(type.GetGenericArguments())}"
            + $" attributes={Attributes(type.GetCustomAttributesData())}";
    }

    private static string TypeVisibility(Type type)
        => type.IsPublic || type.IsNestedPublic ? "public"
            : type.IsNestedFamily ? "protected" : "protected internal";

    private static string? DescribeMember(MemberInfo member) => member switch
    {
        ConstructorInfo constructor when IsAccessible(constructor)
            => $"constructor flags=[{constructor.Attributes}] ({Parameters(constructor.GetParameters())})",
        MethodInfo method when IsAccessible(method)
            => $"method {method.Name}{GenericParameters(method.GetGenericArguments())}"
                + $" flags=[{method.Attributes}] ({Parameters(method.GetParameters())})"
                + $" returns={Parameter(method.ReturnParameter)}",
        FieldInfo field when IsAccessible(field)
            => $"field {TypeName(field.FieldType)} {field.Name} flags=[{field.Attributes}]"
                + (field.IsLiteral ? $" constant={Value(field.GetRawConstantValue())}" : ""),
        PropertyInfo property when property.GetAccessors(true).Any(IsAccessible)
            => $"property {TypeName(property.PropertyType)} {property.Name}"
                + $" flags=[{property.Attributes}] index=({Parameters(property.GetIndexParameters())})"
                + $" required=[{Modifiers(property.GetRequiredCustomModifiers())}]"
                + $" optional=[{Modifiers(property.GetOptionalCustomModifiers())}]",
        EventInfo eventInfo when eventInfo.GetAddMethod(true) is { } add && IsAccessible(add)
            => $"event {TypeName(eventInfo.EventHandlerType)} {eventInfo.Name} flags=[{eventInfo.Attributes}]",
        _ => null
    };

    private static string Parameters(IEnumerable<ParameterInfo> parameters)
        => string.Join(" | ", parameters.Select(Parameter));

    private static string Parameter(ParameterInfo parameter)
        => $"{TypeName(parameter.ParameterType)} {parameter.Name} flags=[{parameter.Attributes}]"
            + (parameter.HasDefaultValue ? $" default={Value(parameter.RawDefaultValue)}" : "")
            + $" required=[{Modifiers(parameter.GetRequiredCustomModifiers())}]"
            + $" optional=[{Modifiers(parameter.GetOptionalCustomModifiers())}]"
            + $" attributes={Attributes(parameter.GetCustomAttributesData())}";

    private static string Modifiers(IEnumerable<Type> types) => string.Join(",", types.Select(TypeName));

    private static string GenericParameters(IEnumerable<Type> arguments)
    {
        string[] parameters = arguments.Where(argument => argument.IsGenericParameter)
            .Select(argument => $"{argument.Name}:flags=[{argument.GenericParameterAttributes}]"
                + $":constraints=[{string.Join(",", argument.GetGenericParameterConstraints().Select(TypeName).Order(StringComparer.Ordinal))}]")
            .ToArray();
        return parameters.Length == 0 ? "" : $" generics=[{string.Join(" | ", parameters)}]";
    }

    private static string TypeName(Type? type)
    {
        if (type == null) return "<none>";
        if (type.IsByRef) return TypeName(type.GetElementType()) + "&";
        if (type.IsPointer) return TypeName(type.GetElementType()) + "*";
        if (type.IsArray) return TypeName(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        if (type.IsGenericParameter) return type.Name;
        if (!type.IsGenericType) return (type.FullName ?? type.Name).Replace('+', '.');

        string name = string.Join(".", (type.GetGenericTypeDefinition().FullName ?? type.Name)
            .Split('+').Select(part =>
            {
                int marker = part.IndexOf(GenericArityMarker);
                return marker < 0 ? part : part[..marker];
            }));
        return name + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">";
    }

    private static string Attributes(IEnumerable<CustomAttributeData> attributes)
        => "[" + string.Join("; ", attributes.Select(attribute =>
            TypeName(attribute.AttributeType) + "("
            + string.Join(",", attribute.ConstructorArguments.Select(AttributeArgument)) + ")"
            + "{" + string.Join(",", attribute.NamedArguments.Select(argument =>
                (argument.IsField ? "field " : "property ") + argument.MemberName
                + "=" + AttributeArgument(argument.TypedValue)).Order(StringComparer.Ordinal)) + "}")
            .Order(StringComparer.Ordinal)) + "]";

    private static string AttributeArgument(CustomAttributeTypedArgument argument)
        => TypeName(argument.ArgumentType) + ":" + (argument.Value is IEnumerable<CustomAttributeTypedArgument> values
            ? "[" + string.Join(",", values.Select(AttributeArgument)) + "]"
            : Value(argument.Value));

    private static string Value(object? value) => value switch
    {
        null => "null",
        string text => JsonSerializer.Serialize(text),
        char character => JsonSerializer.Serialize(character.ToString()),
        Type type => TypeName(type),
        Missing => "<missing>",
        DBNull => "<dbnull>",
        IFormattable formatted => formatted.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "null"
    };
}
