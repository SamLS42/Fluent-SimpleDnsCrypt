using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SimpleDnsCrypt.Extensions;

public static class Base64UrlExtensions
{
	/// <summary>
	/// Encodes string to base64url, as specified by rfc4648 (https://tools.ietf.org/html/rfc4648#section-5)
	/// </summary>
	/// <see cref="https://gist.github.com/dariusdamalakas/b9570c36481aea6dd24d#file-base64urlextensions"/>
	/// <returns></returns>
	public static string ToBase64Url(this string str)
	{
		return str == null ? throw new ArgumentNullException(nameof(str)) : Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(str));
	}

	public static string FromBase64UrlToString(this string base64Url)
	{
		byte[] bytes = Base64UrlEncoder.DecodeBytes(base64Url);
		return Encoding.UTF8.GetString(bytes);
	}

	public static byte[] FromBase64Url(this string base64Url)
	{
		return Base64UrlEncoder.DecodeBytes(base64Url);
	}
}
