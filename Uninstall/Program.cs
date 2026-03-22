using Microsoft.Win32;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;

namespace Uninstall;

internal class Program
{
	private const string DnsCryptProxyFolder = "dnscrypt-proxy";
	private const string DnsCryptProxyExecutableName = "dnscrypt-proxy.exe";
	private const string DnsCryptProxyConfigName = "dnscrypt-proxy.toml";

	private static void Main(string[] args)
	{
		try
		{
			BackupConfigurationFile();
			ClearLocalNetworkInterfaces();
			StopService();
			Thread.Sleep(500);
			UninstallService();
		}
		finally
		{
			Environment.Exit(0);
		}
	}

	/// <summary>
	///		Copy dnscrypt-proxy.toml to tmp folder.
	/// </summary>
	internal static void BackupConfigurationFile()
	{
		try
		{
			string sdcConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleDnsCrypt.exe.config");
			if (!File.Exists(sdcConfig))
				return;
			ExeConfigurationFileMap sdcConfigMap = new()
			{
				ExeConfigFilename = sdcConfig
			};
			Configuration sdcConfigContent =
				ConfigurationManager.OpenMappedExeConfiguration(sdcConfigMap, ConfigurationUserLevel.None);
			if (!sdcConfigContent.HasFile)
				return;
			ClientSettingsSection section = (ClientSettingsSection)sdcConfigContent.GetSection("userSettings/SimpleDnsCrypt.Properties.Settings");
			SettingElement setting = section.Settings.Get("BackupAndRestoreConfigOnUpdate");
			bool backupAndRestoreConfigOnUpdate = Convert.ToBoolean(setting.Value.ValueXml.LastChild.InnerText);
			if (!backupAndRestoreConfigOnUpdate)
				return;
			string config = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DnsCryptProxyFolder,
					DnsCryptProxyConfigName);
			if (!File.Exists(config))
				return;
			string tmp = Path.Combine(Path.GetTempPath(), DnsCryptProxyConfigName + ".bak");
			Console.WriteLine($"backup configuration to {tmp}");
			File.Copy(config, tmp);
		}
		catch (Exception) { }
	}

	/// <summary>
	///		Stop the dnscrypt-proxy service.
	/// </summary>
	internal static void StopService()
	{
		Console.WriteLine("stopping dnscrypt service");
		string dnsCryptProxyExecutablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DnsCryptProxyFolder, DnsCryptProxyExecutableName);
		ExecuteWithArguments(dnsCryptProxyExecutablePath, "-service stop");
	}

	/// <summary>
	///		Uninstall the dnscrypt-proxy service.
	/// </summary>
	internal static void UninstallService()
	{
		Console.WriteLine("removing dnscrypt service");
		string dnsCryptProxyExecutablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DnsCryptProxyFolder, DnsCryptProxyExecutableName);
		ExecuteWithArguments(dnsCryptProxyExecutablePath, "-service uninstall");
		Registry.LocalMachine.DeleteSubKey(@"SYSTEM\CurrentControlSet\Services\EventLog\Application\dnscrypt-proxy", false);
	}

	/// <summary>
	///		Execute process with arguments
	/// </summary>
	internal static void ExecuteWithArguments(string filename, string arguments)
	{
		try
		{
			const int timeout = 9000;
			using (Process process = new())
			{
				process.StartInfo.FileName = filename;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				process.Start();
				if (process.WaitForExit(timeout))
				{
					if (process.ExitCode == 0)
					{
						//do nothing
					}
				}
				else
				{
					// Timed out.
					throw new Exception("Timed out");
				}
			}
		}
		catch (Exception) { }
	}

	/// <summary>
	///		 Clear all network interfaces.
	/// </summary>
	internal static void ClearLocalNetworkInterfaces()
	{
		try
		{
			string[] networkInterfaceBlacklist =
			{
					"Microsoft Virtual",
					"Hamachi Network",
					"VMware Virtual",
					"VirtualBox",
					"Software Loopback",
					"Microsoft ISATAP",
					"Microsoft-ISATAP",
					"Teredo Tunneling Pseudo-Interface",
					"Microsoft Wi-Fi Direct Virtual",
					"Microsoft Teredo Tunneling Adapter",
					"Von Microsoft gehosteter",
					"Microsoft hosted",
					"Virtueller Microsoft-Adapter",
					"TAP"
				};

			List<NetworkInterface> networkInterfaces = [];
			foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (nic.OperationalStatus != OperationalStatus.Up)
				{
					continue;
				}
				foreach (string blacklistEntry in networkInterfaceBlacklist)
				{
					if (nic.Description.Contains(blacklistEntry) || nic.Name.Contains(blacklistEntry))
						continue;
					if (!networkInterfaces.Contains(nic))
					{
						networkInterfaces.Add(nic);
					}
				}
			}

			foreach (NetworkInterface networkInterface in networkInterfaces)
			{
				ExecuteWithArguments("netsh", "interface ipv4 delete dns \"" + networkInterface.Name + "\" all");
				ExecuteWithArguments("netsh", "interface ipv6 delete dns \"" + networkInterface.Name + "\" all");
			}
		}
		catch (Exception)
		{
		}
	}
}
