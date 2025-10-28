using System.Collections;
using System.Linq;
using System.Text;

public static class ObjectFormatter
{
    public static string FormatProperties(object obj)
    {
        if (obj == null)
            return "[]";

        var type = obj.GetType();
        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var sb = new StringBuilder();
        sb.Append("[");

        bool first = true;
        foreach (var prop in props)
        {
            var value = prop.GetValue(obj);
            if (value == null) continue;

            string formattedValue = null;

            // Hvis property er string
            if (value is string strValue)
            {
                formattedValue = $"\"{strValue}\"";
            }
            // Hvis property er en liste eller array af string
            else if (value is IEnumerable enumerable && value.GetType() != typeof(string))
            {
                var items = enumerable.Cast<object>()
                    .Select(x => x is string s ? $"\"{s}\"" : x.ToString());
                formattedValue = $"{{{string.Join(", ", items)}}}";
            }

            if (formattedValue != null)
            {
                if (!first) sb.Append(", ");
                sb.Append($"{prop.Name} = {formattedValue}");
                first = false;
            }
        }

        sb.Append("]");
        return sb.ToString();
    }
}