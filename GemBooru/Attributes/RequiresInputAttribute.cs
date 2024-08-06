namespace GemBooru.Attributes;

public class RequiresInputAttribute(string query, bool allowEmpty = false) : Attribute
{
    public string Query = query;
    public bool AllowEmpty = allowEmpty;
}