using Caliburn.Micro;
using SimpleDnsCrypt.Config;
using SimpleDnsCrypt.Models;
using System.IO;
using System.Net.Http;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SimpleDnsCrypt.Helper;

public static class ApplicationUpdater
{
	private static readonly ILog Log = LogManagerHelper.Factory();

	/// <summary>
	/// Check for a new version on the remote server (github.com).
	/// </summary>
	/// <param name="minUpdateType"></param>
	/// <returns></returns>
	public static async Task<RemoteUpdate> CheckForRemoteUpdateAsync(UpdateType minUpdateType = UpdateType.Stable)
	{
		RemoteUpdate? remoteUpdate = new();
		try
		{
			System.Version? currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

			remoteUpdate.CanUpdate = false;
			string remoteUpdateFile = Environment.Is64BitProcess ? Global.ApplicationUpdateUri64 : Global.ApplicationUpdateUri;
			byte[] remoteUpdateData = await DownloadRemoteUpdateFileAsync(remoteUpdateFile).ConfigureAwait(false);

			if (remoteUpdateData != null)
			{
				using (MemoryStream remoteUpdateDataStream = new(remoteUpdateData))
				{
					using (StreamReader remoteUpdateDataStreamReader = new(remoteUpdateDataStream))
					{
						IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
						remoteUpdate = deserializer.Deserialize<RemoteUpdate>(remoteUpdateDataStreamReader);
					}
				}
			}

			if (remoteUpdate != null)
			{
				int status = remoteUpdate.Update.Version.CompareTo(currentVersion);
				// The local version is newer as the remote version
				if (status < 0)
				{
					remoteUpdate.CanUpdate = false;
				}
				//The local version is the same as the remote version
				else if (status == 0)
				{
					remoteUpdate.CanUpdate = false;
				}
				else
				{
					// the remote version is newer as the local version
					if ((int)minUpdateType >= (int)remoteUpdate.Update.Type)
					{
						remoteUpdate.CanUpdate = true;
					}
					else
					{
						remoteUpdate.CanUpdate = false;
					}
				}
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception);
			remoteUpdate.CanUpdate = false;
		}

		return remoteUpdate;
	}

	private static async Task<byte[]> DownloadRemoteUpdateFileAsync(string remoteUpdateFile)
	{
		using (HttpClient client = new())
		{
			Task<byte[]> getDataTask = client.GetByteArrayAsync(remoteUpdateFile);
			return await getDataTask.ConfigureAwait(false);
		}
	}

	public static async Task<byte[]> DownloadRemoteInstallerAsync(Uri uri)
	{
		using (HttpClient client = new())
		{
			Task<byte[]> getDataTask = client.GetByteArrayAsync(uri);
			return await getDataTask.ConfigureAwait(false);
		}
	}

	public static async Task<string> DownloadRemoteSignatureAsync(Uri uri)
	{
		using (HttpClient client = new())
		{
			Task<string> getDataTask = client.GetStringAsync(uri);
			string resolverList = await getDataTask.ConfigureAwait(false);
			return resolverList;
		}
	}
}

internal sealed class UriYamlTypeConverter : IYamlTypeConverter
{
	public bool Accepts(Type type)
	{
		return type == typeof(Uri);
	}

	public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
	{
		string value = ((Scalar)parser.Current).Value;
		parser.MoveNext();
		return new Uri(value);
	}

	public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
	{
		Uri? uri = (Uri)value;
		emitter.Emit(new Scalar(null, null, uri.ToString(), ScalarStyle.Any, true, false));
	}
}
