namespace ValidationEngine.Resolver;

public sealed class ApplicabilityMatrixReader
{
    public static IReadOnlyList<ApplicabilityEntry> Read(string csvFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvFilePath);

        var lines = File.ReadAllLines(csvFilePath);
        var entries = new List<ApplicabilityEntry>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitCsvLine(line);
            if (fields.Count < 5)
            {
                throw new InvalidDataException($"Applicability matrix line has {fields.Count} field(s); expected at least 5: '{line}'.");
            }

            entries.Add(new ApplicabilityEntry
            {
                StandardFile = fields[0].Trim(),
                MarkerBase = fields[1].Trim(),
                WebAppWebApi = ParseStatus(fields[2]),
                Database = ParseStatus(fields[3]),
                ClassLibrary = ParseStatus(fields[4])
            });
        }

        return entries;
    }

    private static ApplicabilityStatus ParseStatus(string field)
    {
        field = field.Trim();

        return field switch
        {
            "Applies" => ApplicabilityStatus.Applies,
            "Conditional" => ApplicabilityStatus.Conditional,
            "N/A" => ApplicabilityStatus.NotApplicable,
            _ => throw new InvalidDataException($"Unrecognized applicability status '{field}'.")
        };
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        var insideQuotes = false;

        foreach (var character in line)
        {
            if (character == '"')
            {
                insideQuotes = !insideQuotes;
                continue;
            }

            if (character == ',' && !insideQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
                continue;
            }

            currentField.Append(character);
        }

        fields.Add(currentField.ToString());
        return fields;
    }
}
