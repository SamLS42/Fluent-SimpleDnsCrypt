using Caliburn.Micro;
using System.Diagnostics;

namespace SimpleDnsCrypt.Helper;

public static class LogManagerHelper
{
	public static ILog Factory()
	{
		StackFrame callerFrame = new(1);
		return LogManager.GetLog(callerFrame.GetMethod().ReflectedType);
	}
}