using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace JocysCom.Sql.Propagate
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public App()
		{
			// IMPORTANT: Make sure this method don't have any static references to JocysCom library or
			// program tries to load JocysCom.ClassLibrary.dll before AssemblyResolve event is available and fails.
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		}

		#region Get Resources

		private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
		{
			var dllName = e.Name.Contains(",") ? e.Name.Substring(0, e.Name.IndexOf(',')) : e.Name.Replace(".dll", "");
			var unmanaged = dllName == "Microsoft.SqlServer.BatchParser";
			return LoadAssembly(dllName, unmanaged);
		}

		public static Assembly LoadAssembly(string dllName, bool unmanaged = true)
		{
			// Try to get resource as DLL.
			var fileName = dllName + ".dll";
			var sr = GetResourceStream(fileName);
			if (sr == null)
				return null;
			var bytes = new byte[sr.Length];
			sr.Read(bytes, 0, bytes.Length);
			Assembly asm = null;
			if (unmanaged)
			{
				// Summary: There is no way to load a native assembly from memory (bytes) in .NET.
				// You must load from the file on the disk.
				var fi = SaveFileWithChecksum(fileName, bytes);
				asm = Assembly.LoadFrom(fi.FullName);
			}
			else
			{
				// This will fail load assemblies with unmanaged code.
				asm = Assembly.Load(bytes);
			}
			sr.Dispose();
			return asm;
		}

		/// <summary>
		/// Get 32-bit or 64-bit resource depending on x360ce.exe platform.
		/// </summary>
		public static Stream GetResourceStream(string name)
		{
			var path = GetResourcePath(name);
			if (path == null)
				return null;
			var assembly = Assembly.GetEntryAssembly();
			if (assembly == null)
				return null;
			var sr = assembly.GetManifestResourceStream(path);
			return sr;
		}

		/// <summary>
		/// Get 32-bit or 64-bit resource depending on x360ce.exe platform.
		/// </summary>
		public static string GetResourcePath(string name)
		{
			var assembly = Assembly.GetEntryAssembly();
			if (assembly == null)
				return null;
			var names = assembly.GetManifestResourceNames()
				.Where(x => x.EndsWith(name));
			var a = Environment.Is64BitProcess ? ".x64." : ".x86.";
			// Try to get by architecture first.
			var path = names.FirstOrDefault(x => x.Contains(a));
			if (!string.IsNullOrEmpty(path))
				return path;
			// Return first found.
			return names.FirstOrDefault();
		}

		public static FileInfo SaveFileWithChecksum(string name, byte[] bytes)
		{
			var assembly = Assembly.GetEntryAssembly();
			var company = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCompanyAttribute))).Company;
			var product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute))).Product;
			// Get writable application folder.
			var specialFolder = Environment.SpecialFolder.CommonApplicationData;
			var folder = string.Format("{0}\\{1}\\{2}", Environment.GetFolderPath(specialFolder), company, product);
			var hash = ComputeCRC32Checksum(bytes);
			// Put file into sub folder because file name must match with LoadLibrary() argument. 
			var chName = string.Format("{0}.{1:X8}\\{0}", name, hash);
			var fileName = System.IO.Path.Combine(folder, "Temp", chName);
			var fi = new FileInfo(fileName);
			if (fi.Exists)
				return fi;
			if (!fi.Directory.Exists)
				fi.Directory.Create();
			File.WriteAllBytes(fileName, bytes);
			fi.Refresh();
			return fi;
		}

		public static uint ComputeCRC32Checksum(byte[] bytes)
		{
			uint poly = 0xedb88320;
			uint[] table = new uint[256];
			uint temp;
			for (uint i = 0; i < table.Length; ++i)
			{
				temp = i;
				for (int j = 8; j > 0; --j)
					temp = (temp & 1) == 1 ? (temp >> 1) ^ poly : temp >> 1;
				table[i] = temp;
			}
			uint crc = 0xffffffff;
			for (int i = 0; i < bytes.Length; ++i)
				crc = (crc >> 8) ^ table[(byte)(((crc) & 0xff) ^ bytes[i])];
			return ~crc;
		}

		#endregion
	}

}
