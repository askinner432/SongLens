using System.Globalization;

namespace SongMetainfoBrowser.App;

internal enum AdvancedSearchMatchMode
{
    AllRules,
    AnyRule
}

internal enum AdvancedSearchFieldType
{
    Text,
    Number,
    Date
}

internal enum AdvancedSearchOperator
{
    Contains,
    DoesNotContain,
    Is,
    IsNot,
    StartsWith,
    EndsWith,
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    On,
    NotOn,
    Before,
    OnOrBefore,
    After,
    OnOrAfter
}

internal sealed class AdvancedSearchRule
{
    public required string FieldKey { get; init; }
    public required AdvancedSearchOperator Operator { get; init; }
    public string? ValueText { get; init; }
    public decimal? NumberValue { get; init; }
    public DateTime? DateValue { get; init; }
}

internal sealed class AdvancedSearchQuery
{
    public required AdvancedSearchMatchMode MatchMode { get; init; }
    public required IReadOnlyList<AdvancedSearchRule> Rules { get; init; }
}

internal sealed class SavedAdvancedSearch
{
    public required string Name { get; init; }
    public required AdvancedSearchQuery Query { get; init; }
}

internal sealed class AdvancedSearchFieldDefinition
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required AdvancedSearchFieldType FieldType { get; init; }
    public required Func<SongMetadata, IEnumerable<string>> ValueSelector { get; init; }
}

internal static class AdvancedSearchCatalog
{
    private static readonly IReadOnlyList<AdvancedSearchFieldDefinition> _fields =
    [
        new()
        {
            Key = "song",
            Label = "Song",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => YieldSingle(metadata.FileName)
        },
        new()
        {
            Key = "title",
            Label = "Title",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => YieldSingle(metadata.Title)
        },
        new()
        {
            Key = "artist",
            Label = "Artist",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => YieldSingle(metadata.Artist)
        },
        new()
        {
            Key = "year",
            Label = "Year",
            FieldType = AdvancedSearchFieldType.Number,
            ValueSelector = metadata => YieldSingle(metadata.Year)
        },
        new()
        {
            Key = "dateCreated",
            Label = "Date Created",
            FieldType = AdvancedSearchFieldType.Date,
            ValueSelector = metadata => YieldSingle(metadata.DateCreated)
        },
        new()
        {
            Key = "lastModified",
            Label = "Last Modified",
            FieldType = AdvancedSearchFieldType.Date,
            ValueSelector = metadata => YieldSingle(metadata.LastModified)
        },
        new()
        {
            Key = "tempo",
            Label = "Tempo",
            FieldType = AdvancedSearchFieldType.Number,
            ValueSelector = metadata => YieldSingle(metadata.Tempo)
        },
        new()
        {
            Key = "keySignature",
            Label = "Key Signature",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => YieldSingle(metadata.KeySignature)
        },
        new()
        {
            Key = "timeSignature",
            Label = "Time Signature",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => YieldSingle(metadata.TimeSignature)
        },
        new()
        {
            Key = "trackCount",
            Label = "Track Count",
            FieldType = AdvancedSearchFieldType.Number,
            ValueSelector = metadata => YieldSingle(metadata.TrackCount)
        },
        new()
        {
            Key = "sampleRate",
            Label = "Sample Rate",
            FieldType = AdvancedSearchFieldType.Number,
            ValueSelector = metadata => YieldSingle(metadata.SampleRate)
        },
        new()
        {
            Key = "bitDepth",
            Label = "Bit Depth",
            FieldType = AdvancedSearchFieldType.Number,
            ValueSelector = metadata => YieldSingle(metadata.BitDepth)
        },
        new()
        {
            Key = "comment",
            Label = "Comment",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => YieldSingle(metadata.Comment)
        },
        new()
        {
            Key = "notes",
            Label = "Notes",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => YieldSingle(metadata.NotesText)
        },
        new()
        {
            Key = "savedIn",
            Label = "Saved In",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => YieldSingle(SongGeneratorDisplay.GetSearchQualifier(metadata.Generator))
        },
        new()
        {
            Key = "path",
            Label = "Path",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => YieldSingle(metadata.Path)
        },
        new()
        {
            Key = "folderName",
            Label = "Folder Name",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => YieldSingle(Path.GetFileName(metadata.Folder))
        },
        new()
        {
            Key = "trackName",
            Label = "Track Name",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => metadata.TrackInstruments.Select(track => track.TrackName)
        },
        new()
        {
            Key = "instrument",
            Label = "Instrument",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => metadata.TrackInstruments.Select(track => track.InstrumentName ?? "")
        },
        new()
        {
            Key = "trackNote",
            Label = "Track Note",
            FieldType = AdvancedSearchFieldType.Text,
            ValueSelector = metadata => metadata.TrackInstruments.Select(track => track.TrackNote ?? "")
        }
    ];

    private static readonly IReadOnlyDictionary<string, AdvancedSearchFieldDefinition> _fieldsByKey =
        _fields.ToDictionary(field => field.Key, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<AdvancedSearchFieldDefinition> Fields => _fields;

    public static AdvancedSearchFieldDefinition GetField(string key)
    {
        if (_fieldsByKey.TryGetValue(key, out var field))
        {
            return field;
        }

        throw new KeyNotFoundException($"Unknown advanced search field: {key}");
    }

    public static IReadOnlyList<AdvancedSearchOperator> GetOperators(AdvancedSearchFieldType fieldType)
    {
        return fieldType switch
        {
            AdvancedSearchFieldType.Text =>
            [
                AdvancedSearchOperator.Contains,
                AdvancedSearchOperator.DoesNotContain,
                AdvancedSearchOperator.Is,
                AdvancedSearchOperator.IsNot,
                AdvancedSearchOperator.StartsWith,
                AdvancedSearchOperator.EndsWith
            ],
            AdvancedSearchFieldType.Number =>
            [
                AdvancedSearchOperator.Equal,
                AdvancedSearchOperator.NotEqual,
                AdvancedSearchOperator.GreaterThan,
                AdvancedSearchOperator.GreaterThanOrEqual,
                AdvancedSearchOperator.LessThan,
                AdvancedSearchOperator.LessThanOrEqual
            ],
            AdvancedSearchFieldType.Date =>
            [
                AdvancedSearchOperator.On,
                AdvancedSearchOperator.NotOn,
                AdvancedSearchOperator.Before,
                AdvancedSearchOperator.OnOrBefore,
                AdvancedSearchOperator.After,
                AdvancedSearchOperator.OnOrAfter
            ],
            _ => Array.Empty<AdvancedSearchOperator>()
        };
    }

    public static string GetOperatorLabel(AdvancedSearchOperator op)
    {
        return op switch
        {
            AdvancedSearchOperator.Contains => "contains",
            AdvancedSearchOperator.DoesNotContain => "does not contain",
            AdvancedSearchOperator.Is => "is",
            AdvancedSearchOperator.IsNot => "is not",
            AdvancedSearchOperator.StartsWith => "starts with",
            AdvancedSearchOperator.EndsWith => "ends with",
            AdvancedSearchOperator.Equal => "=",
            AdvancedSearchOperator.NotEqual => "!=",
            AdvancedSearchOperator.GreaterThan => ">",
            AdvancedSearchOperator.GreaterThanOrEqual => ">=",
            AdvancedSearchOperator.LessThan => "<",
            AdvancedSearchOperator.LessThanOrEqual => "<=",
            AdvancedSearchOperator.On => "on",
            AdvancedSearchOperator.NotOn => "not on",
            AdvancedSearchOperator.Before => "before",
            AdvancedSearchOperator.OnOrBefore => "on or before",
            AdvancedSearchOperator.After => "after",
            AdvancedSearchOperator.OnOrAfter => "on or after",
            _ => op.ToString()
        };
    }

    private static IEnumerable<string> YieldSingle(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            yield return value;
        }
    }
}

internal static class AdvancedSongSearch
{
    public static SearchResult? GetMatch(SongMetadata metadata, AdvancedSearchQuery query)
    {
        if (query.Rules.Count == 0)
        {
            return null;
        }

        SearchResult? firstMatch = null;
        var anyMatched = false;

        foreach (var rule in query.Rules)
        {
            var field = AdvancedSearchCatalog.GetField(rule.FieldKey);
            var matched = TryMatchRule(metadata, field, rule, out var matchedValue);

            if (matched)
            {
                anyMatched = true;
                firstMatch ??= new SearchResult
                {
                    Metadata = metadata,
                    MatchField = field.Label,
                    MatchValue = matchedValue
                };

                if (query.MatchMode == AdvancedSearchMatchMode.AnyRule)
                {
                    return firstMatch;
                }
            }
            else if (query.MatchMode == AdvancedSearchMatchMode.AllRules)
            {
                return null;
            }
        }

        return query.MatchMode == AdvancedSearchMatchMode.AllRules
            ? firstMatch
            : anyMatched ? firstMatch : null;
    }

    private static bool TryMatchRule(SongMetadata metadata, AdvancedSearchFieldDefinition field, AdvancedSearchRule rule, out string matchedValue)
    {
        matchedValue = "";
        return field.FieldType switch
        {
            AdvancedSearchFieldType.Text => MatchText(field.ValueSelector(metadata), rule, out matchedValue),
            AdvancedSearchFieldType.Number => MatchNumber(field.ValueSelector(metadata), rule, out matchedValue),
            AdvancedSearchFieldType.Date => MatchDate(field.ValueSelector(metadata), rule, out matchedValue),
            _ => false
        };
    }

    private static bool MatchText(IEnumerable<string> values, AdvancedSearchRule rule, out string matchedValue)
    {
        matchedValue = "";
        var query = rule.ValueText?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var candidates = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        if (candidates.Length == 0)
        {
            return false;
        }

        var predicate = rule.Operator switch
        {
            AdvancedSearchOperator.Contains => new Func<string, bool>(value => value.Contains(query, StringComparison.OrdinalIgnoreCase)),
            AdvancedSearchOperator.DoesNotContain => value => !value.Contains(query, StringComparison.OrdinalIgnoreCase),
            AdvancedSearchOperator.Is => value => string.Equals(value, query, StringComparison.OrdinalIgnoreCase),
            AdvancedSearchOperator.IsNot => value => !string.Equals(value, query, StringComparison.OrdinalIgnoreCase),
            AdvancedSearchOperator.StartsWith => value => value.StartsWith(query, StringComparison.OrdinalIgnoreCase),
            AdvancedSearchOperator.EndsWith => value => value.EndsWith(query, StringComparison.OrdinalIgnoreCase),
            _ => _ => false
        };

        var requiresAllValuesToMatch = rule.Operator is AdvancedSearchOperator.DoesNotContain or AdvancedSearchOperator.IsNot;
        if (requiresAllValuesToMatch)
        {
            if (!candidates.All(predicate))
            {
                return false;
            }

            matchedValue = FormatMatchValue(candidates[0]);
            return true;
        }

        var match = candidates.FirstOrDefault(predicate);
        if (match is null)
        {
            return false;
        }

        matchedValue = FormatMatchValue(match);
        return true;
    }

    private static bool MatchNumber(IEnumerable<string> values, AdvancedSearchRule rule, out string matchedValue)
    {
        matchedValue = "";
        if (rule.NumberValue is null)
        {
            return false;
        }

        foreach (var value in values)
        {
            if (!TryParseNumber(value, out var numericValue))
            {
                continue;
            }

            if (!CompareNumber(numericValue, rule.NumberValue.Value, rule.Operator))
            {
                continue;
            }

            matchedValue = numericValue.ToString(CultureInfo.CurrentCulture);
            return true;
        }

        return false;
    }

    private static bool MatchDate(IEnumerable<string> values, AdvancedSearchRule rule, out string matchedValue)
    {
        matchedValue = "";
        if (rule.DateValue is null)
        {
            return false;
        }

        var expectedDate = rule.DateValue.Value.Date;
        foreach (var value in values)
        {
            if (!DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsedDate))
            {
                continue;
            }

            var candidateDate = parsedDate.Date;
            if (!CompareDate(candidateDate, expectedDate, rule.Operator))
            {
                continue;
            }

            matchedValue = DateTimeDisplay.Format(parsedDate);
            return true;
        }

        return false;
    }

    private static bool TryParseNumber(string? value, out decimal numericValue)
    {
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out numericValue)
            || decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out numericValue);
    }

    private static bool CompareNumber(decimal candidate, decimal expected, AdvancedSearchOperator op)
    {
        return op switch
        {
            AdvancedSearchOperator.Equal => candidate == expected,
            AdvancedSearchOperator.NotEqual => candidate != expected,
            AdvancedSearchOperator.GreaterThan => candidate > expected,
            AdvancedSearchOperator.GreaterThanOrEqual => candidate >= expected,
            AdvancedSearchOperator.LessThan => candidate < expected,
            AdvancedSearchOperator.LessThanOrEqual => candidate <= expected,
            _ => false
        };
    }

    private static bool CompareDate(DateTime candidate, DateTime expected, AdvancedSearchOperator op)
    {
        return op switch
        {
            AdvancedSearchOperator.On => candidate == expected,
            AdvancedSearchOperator.NotOn => candidate != expected,
            AdvancedSearchOperator.Before => candidate < expected,
            AdvancedSearchOperator.OnOrBefore => candidate <= expected,
            AdvancedSearchOperator.After => candidate > expected,
            AdvancedSearchOperator.OnOrAfter => candidate >= expected,
            _ => false
        };
    }

    private static string FormatMatchValue(string value)
    {
        var singleLineValue = value.ReplaceLineEndings(" ").Trim();
        return singleLineValue.Length <= 120
            ? singleLineValue
            : $"{singleLineValue[..117]}...";
    }
}
