using System.Collections.Generic;
using System.Text;

public static class CsvUtility
{
    public static List<string[]> Parse(string text)
    {
        List<string[]> rows = new();
        List<string> row = new();
        StringBuilder field = new();

        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                row.Add(field.ToString());
                field.Clear();
            }
            else if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(field.ToString());
                field.Clear();

                if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0]))
                {
                    rows.Add(row.ToArray());
                }

                row = new List<string>();
            }
            else
            {
                field.Append(c);
            }
        }

        row.Add(field.ToString());

        if (row.Count > 1 || !string.IsNullOrWhiteSpace(row[0]))
        {
            rows.Add(row.ToArray());
        }

        return rows;
    }
}