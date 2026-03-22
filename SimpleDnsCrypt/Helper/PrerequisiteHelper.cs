using Caliburn.Micro;
using Microsoft.Win32;
using SimpleDnsCrypt.Config;
using System.Diagnostics;
using System.IO;
using System.Net.Http;

namespace SimpleDnsCrypt.Helper;

public static class PrerequisiteHelper
{
	private static readonly ILog Log = LogManagerHelper.Factory();

	public static bool IsRedistributablePackageInstalled()
	{
		try
		{
			if (Environment.Is64BitProcess)
			{
				//check for 2015 - 2019
				RegistryKey? parametersVc2015to2019x64 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DevDiv\VC\Servicing\14.0\RuntimeMinimum", false);
				if (parametersVc2015to2019x64 != null)
				{
					object? vc2015to2019x64Version = parametersVc2015to2019x64.GetValue("Version");
					if (((string)vc2015to2019x64Version).StartsWith("14"))
					{
						return true;
					}
				}
				//check for 2017
				List<string> paths2017X64 =
				[
					@"Installer\Dependencies\,,amd64,14.0,bundle",
					@"Installer\Dependencies\VC,redist.x64,amd64,14.16,bundle" //changed in 14.16.x
				];
				foreach (string path in paths2017X64)
				{
					RegistryKey? parametersVc2017X64 = Registry.ClassesRoot.OpenSubKey(path, false);
					if (parametersVc2017X64 == null)
						continue;
					object? vc2017X64Version = parametersVc2017X64.GetValue("Version");
					if (vc2017X64Version == null)
						return false;
					if (((string)vc2017X64Version).StartsWith("14"))
					{
						return true;
					}
				}
			}
			else
			{
				//check for 2015 - 2019
				RegistryKey? parametersVc2015to2019x86 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DevDiv\VC\Servicing\14.0\RuntimeMinimum", false);
				if (parametersVc2015to2019x86 != null)
				{
					object? vc2015to2019x86Version = parametersVc2015to2019x86.GetValue("Version");
					if (((string)vc2015to2019x86Version).StartsWith("14"))
					{
						return true;
					}
				}
				//check for 2017
				List<string> paths2017X86 =
				[
					@"Installer\Dependencies\,,x86,14.0,bundle",
					@"Installer\Dependencies\VC,redist.x86,x86,14.16,bundle" //changed in 14.16.x
				];
				foreach (string path in paths2017X86)
				{
					RegistryKey? parametersVc2017X86 = Registry.ClassesRoot.OpenSubKey(path, false);
					if (parametersVc2017X86 == null)
						continue;
					object? vc2017X86Version = parametersVc2017X86.GetValue("Version");
					if (vc2017X86Version == null)
						return false;
					if (((string)vc2017X86Version).StartsWith("14"))
					{
						return true;
					}
				}
			}
			return false;
		}
		catch (Exception exception)
		{
			Log.Error(exception);
			return false;
		}
	}

	public static async Task DownloadAndInstallRedistributablePackage()
	{
		try
		{
			string url = Environment.Is64BitProcess ? Global.RedistributablePackage64 : Global.RedistributablePackage86;
			string path = Path.Combine(Path.GetTempPath(), "VC_redist.exe");
			using (HttpClient client = new())
			{
				Task<byte[]> getDataTask = client.GetByteArrayAsync(url);
				byte[] file = await getDataTask.ConfigureAwait(false);
				if (file != null)
				{
					File.WriteAllBytes(path, file);
				}
			}
			if (File.Exists(path))
			{
				const string arguments = "/install /passive /norestart";
				ProcessStartInfo startInfo = new(path)
				{
					Arguments = arguments,
					UseShellExecute = false
				};
				Process.Start(startInfo);
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception);
		}
	}
}
