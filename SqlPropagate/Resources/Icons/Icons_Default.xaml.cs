using System.Windows;

namespace JocysCom.Sql.Propagate
{
	partial class Icons_Default : ResourceDictionary
	{
		public Icons_Default()
		{
			InitializeComponent();
		}

		public static Icons_Default Current => _Current = _Current ?? new Icons_Default();
		private static Icons_Default _Current;

		public const string Icon_data = nameof(Icon_data);
		public const string Icon_data_scroll = nameof(Icon_data_scroll);
		public const string Icon_gearwheel = nameof(Icon_gearwheel);
		public const string Icon_scroll = nameof(Icon_scroll);

	}
}
