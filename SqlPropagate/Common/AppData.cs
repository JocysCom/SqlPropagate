using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JocysCom.Sql.Propagate
{
	public class AppData : SettingsItem, INotifyPropertyChanged
	{
		public bool Enabled { get; set; }

		public override bool IsEmpty =>
			(Connections?.Count ?? 0) == 0 &&
			(Parameters?.Count ?? 0) == 0 &&
			(Scripts?.Count ?? 0) == 0;

		public SortableBindingList<DataItem> Connections
		{
			get => _Connections = _Connections ?? new SortableBindingList<DataItem>();
			set => _Connections = value;
		}
		private SortableBindingList<DataItem> _Connections;

		public SortableBindingList<DataItem> Parameters
		{
			get => _Parameters = _Parameters ?? new SortableBindingList<DataItem>();
			set => _Parameters = value;
		}
		private SortableBindingList<DataItem> _Parameters;

		public SortableBindingList<DataItem> Scripts
		{
			get => _Scripts = _Scripts ?? new SortableBindingList<DataItem>();
			set => _Scripts = value;
		}
		private SortableBindingList<DataItem> _Scripts;

		public string HelpHeadText { get => _HelpHeadText; set => SetProperty(ref _HelpHeadText, value); }
		string _HelpHeadText;

		public string HelpBodyText { get => _HelpBodyText; set => SetProperty(ref _HelpBodyText, value); }
		string _HelpBodyText;

		public string LogsBodyText { get => _LogsBodyText; set => SetProperty(ref _LogsBodyText, value); }
		string _LogsBodyText;

	}
}
