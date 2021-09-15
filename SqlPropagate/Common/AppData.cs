using JocysCom.ClassLibrary.ComponentModel;

namespace JocysCom.Sql.Propagate
{
	public class AppData : JocysCom.ClassLibrary.Configuration.ISettingsItem
	{
		public bool Enabled { get; set; }

		public bool IsEmpty =>
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

	}
}
