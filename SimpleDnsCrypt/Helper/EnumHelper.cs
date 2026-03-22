using SimpleDnsCrypt.Models;
using System.ComponentModel;
using System.Globalization;

namespace SimpleDnsCrypt.Helper;

public static class EnumHelper
{
	public static string Description(this Enum eValue)
	{
		object[] nAttributes = eValue.GetType().GetField(eValue.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false);
		if (nAttributes.Length != 0)
			return (nAttributes.First() as DescriptionAttribute).Description;

		TextInfo oTi = CultureInfo.CurrentCulture.TextInfo;
		return oTi.ToTitleCase(oTi.ToLower(eValue.ToString()));
	}

	public static IEnumerable<ValueDescription> GetAllValuesAndDescriptions(Type t)
	{
		if (!t.IsEnum)
			throw new ArgumentException("t must be an enum type");

		return Enum.GetValues(t).Cast<Enum>().Select((e) => new ValueDescription() { Value = e, Description = e.Description() }).ToList();
	}
}
