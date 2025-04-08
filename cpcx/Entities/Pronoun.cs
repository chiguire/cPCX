using System.ComponentModel;
using System.Reflection;

namespace cpcx.Entities;

public enum Pronoun
{
    [Description("<Empty>")]
    Empty,
    [Description("He/him")]
    Male,
    [Description("She/her")]
    Female,
    [Description("They/them")]
    Neutral,
    [Description("They/them")]
    Group,
}

static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        FieldInfo field = value.GetType().GetField(value.ToString())!;
        object[] attribs = field.GetCustomAttributes(typeof(DescriptionAttribute), true);
        if(attribs.Length > 0)
        {
            return ((DescriptionAttribute)attribs[0]).Description;
        }
        return string.Empty;
    }
}