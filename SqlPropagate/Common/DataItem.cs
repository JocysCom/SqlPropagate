using JocysCom.ClassLibrary.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace JocysCom.Sql.Propagate
{
	public class DataItem : SettingsItem, INotifyPropertyChanged
	{
		public string Name { get => _Name; set => SetProperty(ref _Name, value); }
		string _Name;

		public string Value { get => _Value; set => SetProperty(ref _Value, value); }
		string _Value;

		public int Order
		{
			get => _Order;
			set => SetProperty(ref _Order, value);
		}
		int _Order;

		public string StatusText { get => _StatusText; set => SetProperty(ref _StatusText, value); }
		string _StatusText;

		public System.Windows.MessageBoxImage StatusCode { get => _StatusCode; set => SetProperty(ref _StatusCode, value); }
		System.Windows.MessageBoxImage _StatusCode;

		public bool IsChecked
		{
			get => _IsChecked;
			set => SetProperty(ref _IsChecked, value);
		}
		bool _IsChecked;

		[XmlIgnore]
		public object Tag;

	}
}
