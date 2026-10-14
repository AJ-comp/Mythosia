using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Mythosia.Integrity;

namespace Mythosia.Test.Integrity;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.Error.WriteLine("Usage: <approved-snapshot> <evidence-json> <assembly-version> <file-version>");
            return 1;
        }

        try
        {
            return VerifyPackage(args[0], args[1], args[2], args[3]);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Package API verification failed: {exception.Message}");
            return 1;
        }
    }

    private static int VerifyPackage(string approvedPath, string evidencePath,
        string expectedAssemblyVersion, string expectedFileVersion)
    {
        string[] approved = ReadApprovedSnapshot(approvedPath);
        Assembly assembly = typeof(ErrorDetection).Assembly;
        string[] actual = IntegrityPublicApiSnapshot.Create(assembly);
        string[] missing = approved.Except(actual, StringComparer.Ordinal).ToArray();
        string[] added = actual.Except(approved, StringComparer.Ordinal).ToArray();
        bool apiMatches = approved.SequenceEqual(actual, StringComparer.Ordinal);
        AssemblyEvidence integrity = DescribeAssembly(assembly);
        AssemblyEvidence mythosia = DescribeAssembly(Assembly.Load(new AssemblyName("Mythosia")));
        bool versionMatches = string.Equals(integrity.AssemblyVersion, expectedAssemblyVersion, StringComparison.Ordinal)
            && string.Equals(integrity.FileVersion, expectedFileVersion, StringComparison.Ordinal);
        int expectedTypeCount = CountTypes(approved);
        int typeCount = CountTypes(actual);

        var evidence = new
        {
            AssemblyName = integrity.Name,
            AssemblyVersion = integrity.AssemblyVersion,
            FileVersion = integrity.FileVersion,
            AssemblyPath = integrity.Path,
            AssemblySHA256 = integrity.SHA256,
            MythosiaAssemblyName = mythosia.Name,
            MythosiaAssemblyVersion = mythosia.AssemblyVersion,
            MythosiaFileVersion = mythosia.FileVersion,
            MythosiaAssemblyPath = mythosia.Path,
            MythosiaAssemblySHA256 = mythosia.SHA256,
            ApprovedSnapshotPath = Path.GetFullPath(approvedPath),
            ApprovedSnapshotSHA256 = HashFile(approvedPath),
            ExpectedAssemblyVersion = expectedAssemblyVersion,
            ExpectedFileVersion = expectedFileVersion,
            ExpectedEntryCount = approved.Length,
            ExpectedTypeCount = expectedTypeCount,
            EntryCount = actual.Length,
            TypeCount = typeCount,
            ApiMatches = apiMatches,
            VersionMatches = versionMatches,
            MissingEntries = missing,
            AddedEntries = added
        };
        WriteEvidence(evidencePath, evidence);

        if (!apiMatches)
        {
            Console.Error.WriteLine($"Package API differs from the approved snapshot: expected {approved.Length} entries/{expectedTypeCount} types, found {actual.Length} entries/{typeCount} types.");
            ReportEntries("Missing or changed approved entries:", "- ", missing);
            ReportEntries("Added or changed current entries:", "+ ", added);
        }
        if (!versionMatches)
        {
            Console.Error.WriteLine($"Unexpected package assembly/file versions: expected {expectedAssemblyVersion}/{expectedFileVersion}, found {integrity.AssemblyVersion}/{integrity.FileVersion}.");
        }
        if (!apiMatches || !versionMatches) return 1;

        Console.WriteLine($"Public API snapshot matched: {actual.Length} entries, {typeCount} types.");
        return 0;
    }

    private static string[] ReadApprovedSnapshot(string path)
    {
        string[] entries = File.ReadAllLines(path)
            .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (entries.Length == 0 || CountTypes(entries) == 0)
            throw new InvalidDataException("The approved API snapshot contains no API or type entries.");
        return entries;
    }

    private static int CountTypes(string[] entries)
        => entries.Count(line => line.StartsWith("type ", StringComparison.Ordinal));

    private static AssemblyEvidence DescribeAssembly(Assembly assembly)
        => new(assembly.GetName().Name, assembly.GetName().Version?.ToString(),
            FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion,
            assembly.Location, HashFile(assembly.Location));

    private static string HashFile(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void WriteEvidence(string path, object evidence)
    {
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void ReportEntries(string heading, string prefix, string[] entries)
    {
        if (entries.Length == 0) return;
        Console.Error.WriteLine(heading);
        foreach (string entry in entries) Console.Error.WriteLine(prefix + entry);
    }

    private sealed record AssemblyEvidence(string? Name, string? AssemblyVersion,
        string? FileVersion, string Path, string SHA256);
}
