using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace SoodalLife.Api.Features.CatalogImport;

public sealed record CatalogWorkbookData(
    IReadOnlyList<IReadOnlyDictionary<string, string>> Categories,
    IReadOnlyList<IReadOnlyDictionary<string, string>> RequestFields,
    IReadOnlyList<IReadOnlyDictionary<string, string>> FeePolicies);

public sealed class CatalogWorkbookReader
{
    private static readonly string[] RequiredSheets =
    [
        "카테고리 마스터",
        "요청 필수필드",
        "견적수수료 정책",
    ];

    public CatalogWorkbookData Read(string workbookPath)
    {
        if (!File.Exists(workbookPath))
        {
            throw new FileNotFoundException("Catalog workbook was not found.", workbookPath);
        }

        using var archive = ZipFile.OpenRead(workbookPath);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetPaths = ReadSheetPaths(archive);

        foreach (var requiredSheet in RequiredSheets)
        {
            if (!sheetPaths.ContainsKey(requiredSheet))
            {
                throw new InvalidDataException($"Required worksheet '{requiredSheet}' is missing.");
            }
        }

        return new CatalogWorkbookData(
            ReadTable(archive, sheetPaths["카테고리 마스터"], sharedStrings, "ID"),
            ReadTable(archive, sheetPaths["요청 필수필드"], sharedStrings, "필드ID"),
            ReadTable(archive, sheetPaths["견적수수료 정책"], sharedStrings, "정책코드"));
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        var document = LoadDocument(entry);
        return document.Descendants()
            .Where(element => element.Name.LocalName == "si")
            .Select(item => string.Concat(item.Descendants().Where(element => element.Name.LocalName == "t").Select(element => element.Value)))
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> ReadSheetPaths(ZipArchive archive)
    {
        var workbook = LoadDocument(RequireEntry(archive, "xl/workbook.xml"));
        var relationships = LoadDocument(RequireEntry(archive, "xl/_rels/workbook.xml.rels"));
        var targets = relationships.Descendants()
            .Where(element => element.Name.LocalName == "Relationship")
            .ToDictionary(
                element => element.Attribute("Id")!.Value,
                element => element.Attribute("Target")!.Value.TrimStart('/'),
                StringComparer.Ordinal);

        return workbook.Descendants()
            .Where(element => element.Name.LocalName == "sheet")
            .ToDictionary(
                sheet => sheet.Attribute("name")!.Value,
                sheet => targets[sheet.Attributes().Single(attribute => attribute.Name.LocalName == "id").Value],
                StringComparer.Ordinal);
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadTable(
        ZipArchive archive,
        string sheetPath,
        IReadOnlyList<string> sharedStrings,
        string firstHeader)
    {
        var document = LoadDocument(RequireEntry(archive, sheetPath));
        var rows = document.Descendants().Where(element => element.Name.LocalName == "row").ToArray();
        Dictionary<int, string>? headers = null;
        var result = new List<IReadOnlyDictionary<string, string>>();

        foreach (var row in rows)
        {
            var values = ReadRow(row, sharedStrings);
            if (headers is null)
            {
                if (!values.Values.Contains(firstHeader, StringComparer.Ordinal))
                {
                    continue;
                }

                headers = values
                    .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                    .ToDictionary(pair => pair.Key, pair => pair.Value.Trim());
                continue;
            }

            var record = headers.ToDictionary(
                pair => pair.Value,
                pair => values.GetValueOrDefault(pair.Key, string.Empty).Trim(),
                StringComparer.Ordinal);
            if (record.Values.Any(value => !string.IsNullOrWhiteSpace(value)))
            {
                result.Add(record);
            }
        }

        return headers is null
            ? throw new InvalidDataException($"Header '{firstHeader}' was not found in '{sheetPath}'.")
            : result;
    }

    private static Dictionary<int, string> ReadRow(XElement row, IReadOnlyList<string> sharedStrings)
    {
        var result = new Dictionary<int, string>();
        foreach (var cell in row.Elements().Where(element => element.Name.LocalName == "c"))
        {
            var reference = cell.Attribute("r")?.Value
                ?? throw new InvalidDataException("A worksheet cell is missing its reference.");
            var columnIndex = GetColumnIndex(reference);
            var type = cell.Attribute("t")?.Value;
            var rawValue = cell.Elements().FirstOrDefault(element => element.Name.LocalName == "v")?.Value ?? string.Empty;

            result[columnIndex] = type switch
            {
                "s" when int.TryParse(rawValue, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                    && index >= 0 && index < sharedStrings.Count => sharedStrings[index],
                "inlineStr" => string.Concat(cell.Descendants().Where(element => element.Name.LocalName == "t").Select(element => element.Value)),
                _ => rawValue,
            };
        }

        return result;
    }

    private static int GetColumnIndex(string cellReference)
    {
        var index = 0;
        foreach (var character in cellReference.TakeWhile(char.IsLetter))
        {
            index = checked(index * 26 + char.ToUpperInvariant(character) - 'A' + 1);
        }

        return index - 1;
    }

    private static ZipArchiveEntry RequireEntry(ZipArchive archive, string path) =>
        archive.GetEntry(path) ?? throw new InvalidDataException($"Workbook entry '{path}' is missing.");

    private static XDocument LoadDocument(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.None);
    }
}
