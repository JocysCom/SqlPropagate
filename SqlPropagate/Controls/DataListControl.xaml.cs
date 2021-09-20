using JocysCom.ClassLibrary.ComponentModel;
using JocysCom.ClassLibrary.Controls;
using JocysCom.ClassLibrary.Controls.Themes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace JocysCom.Sql.Propagate.Controls
{
	/// <summary>
	/// Interaction logic for DataListControl.xaml
	/// </summary>
	public partial class DataListControl : UserControl
	{
		public DataListControl()
		{
			InitializeComponent();
			ScanProgressPanel.Visibility = Visibility.Collapsed;
			if (ControlsHelper.IsDesignMode(this))
				return;
			var dataItems = new SortableBindingList<DataItem>();
			SetDataItems(dataItems);
			// Configure converter.
			var gridFormattingConverter = MainDataGrid.Resources.Values.Cast<ItemFormattingConverter>().First();
			gridFormattingConverter.ConvertFunction = _MainDataGridFormattingConverter_Convert;
		}

		public void SetDataItems(SortableBindingList<DataItem> dataItems)
		{
			if (DataItems != null)
				DataItems.ListChanged -= DataItems_ListChanged;
			DataItems = dataItems;
			DataItems.ListChanged += DataItems_ListChanged;
			Reorder();
			MainDataGrid.ItemsSource = dataItems;
			if (DataItems.Count > 0)
				MainDataGrid.SelectedIndex = 0;
		}

		private void Tasks_ListChanged(object sender, ListChangedEventArgs e)
			=> UpdateUpdateButton();

		bool selectionsUpdating = false;
		private void DataItems_ListChanged(object sender, ListChangedEventArgs e)
		{
			ControlsHelper.BeginInvoke(() =>
			{
				UpdateControlsFromList();
				if (e.ListChangedType == ListChangedType.ItemChanged)
				{
					if (!selectionsUpdating && e.PropertyDescriptor?.Name == nameof(DataItem.IsChecked))
					{
						selectionsUpdating = true;
						var selectedItems = MainDataGrid.SelectedItems.Cast<DataItem>().ToList();
						// Get updated item.
						var item = (DataItem)MainDataGrid.Items[e.NewIndex];
						if (selectedItems.Contains(item))
						{
							// Update other items to same value.
							selectedItems.Remove(item);
							foreach (var selecetdItem in selectedItems)
								if (selecetdItem.IsChecked != item.IsChecked)
									selecetdItem.IsChecked = item.IsChecked;
						}
						selectionsUpdating = false;
					}
					if (e.PropertyDescriptor?.Name == nameof(DataItem.Order))
					{
						// Reorder script items.
						var sorted = DataItems.OrderBy(x => x.Order).ToArray();
						for (int i = 0; i < sorted.Length; i++)
						{
							var item = sorted[i];
							var index = DataItems.IndexOf(item);
							if (index != i)
							{
								DataItems.Remove(item);
								DataItems.Insert(i, item);
							}
						}
					}
				}
			});
		}

		void UpdateControlsFromList()
		{
			var list = DataItems;
			switch (DataType)
			{
				case DataItemType.Connection:
					HeaderLabel.Content = "Connections";
					break;
				case DataItemType.Script:
					HeaderLabel.Content = "Scripts";
					break;
				case DataItemType.Parameter:
					HeaderLabel.Content = "Parameters";
					break;
				default:
					break;
			}
		}

		object _MainDataGridFormattingConverter_Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var sender = (FrameworkElement)values[0];
			var template = (FrameworkElement)values[1];
			var cell = (DataGridCell)(template ?? sender).Parent;
			var value = values[2];
			var item = (DataItem)cell.DataContext;
			// Format StatusCodeColumn value.
			if (cell.Column == StatusCodeColumn)
			{
				switch (item.StatusCode)
				{
					case MessageBoxImage.Error:
						return Icons.Current[Icons.Icon_Error];
					case MessageBoxImage.Question:
						return Icons.Current[Icons.Icon_Question];
					case MessageBoxImage.Warning:
						return Icons.Current[Icons.Icon_Warning];
					case MessageBoxImage.Information:
						return Icons.Current[Icons.Icon_Information];
					default:
						return null;
				}
			}
			else if (cell.Column == ValueColumn)
			{
				if (DataType == DataItemType.Script)
				{
					cell.Foreground = System.IO.File.Exists(item.Value)
						? System.Windows.Media.Brushes.DarkRed
						: System.Windows.SystemColors.WindowTextBrush;
				}
			}
			return value;
		}

		public SortableBindingList<DataItem> DataItems { get; set; }

		#region ■ Properties

		[Category("Main"), DefaultValue(DataItemType.None)]
		public DataItemType DataType
		{
			get => _ProjectControlType;
			set { _ProjectControlType = value; UpdateType(); }
		}
		private DataItemType _ProjectControlType;

		void UpdateType()
		{
			switch (DataType)
			{
				case DataItemType.Connection:
					NameColumn.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
					ShowColumns(IsCheckedColumn, OrderColumn, NameColumn);
					ShowButtons(AddButton, RemoveButton, EditButton);
					break;
				case DataItemType.Parameter:
					ShowColumns(IsCheckedColumn, OrderColumn, NameColumn, ValueColumn);
					ShowButtons(AddButton, RemoveButton);
					break;
				case DataItemType.Script:
					ShowColumns(IsCheckedColumn, OrderColumn, ValueColumn);
					ShowButtons(AddButton, RemoveButton, ExecuteButton);
					break;
				default:
					break;
			}
			// Re-attach events and update header.
			UpdateControlsFromList();
		}

		public void ShowColumns(params DataGridColumn[] args)
		{
			var all = MainDataGrid.Columns.ToArray();
			foreach (var control in all)
				control.Visibility = args.Contains(control) ? Visibility.Visible : Visibility.Collapsed;
		}

		public void ShowButtons(params Button[] args)
		{
			var all = new Button[] { AddButton, RemoveButton, EditButton, ExecuteButton };
			foreach (var control in all)
				control.Visibility = args.Contains(control) ? Visibility.Visible : Visibility.Collapsed;
		}

		#endregion

		private void MainDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
			=> UpdateUpdateButton();

		void UpdateUpdateButton()
		{
		}

		/// <summary>
		/// Convert List to DataTable. Can be used to pass data into stored procedures. 
		/// </summary>
		public static DataTable ConvertToTable<T>(IEnumerable<T> list)
		{
			if (list == null) return null;
			var table = new DataTable();
			var props = typeof(T).GetProperties().Where(x => x.CanRead).ToArray();
			foreach (var prop in props)
				table.Columns.Add(prop.Name, prop.PropertyType);
			var values = new object[props.Length];
			foreach (T item in list)
			{
				for (int i = 0; i < props.Length; i++)
					values[i] = props[i].GetValue(item, null);
				table.Rows.Add(values);
			}
			return table;
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ControlsHelper.IsDesignMode(this))
				return;
		}

		public List<DataItem> GetCheckedOrSelectedReferences(out bool containsChecked)
		{
			containsChecked = DataItems.Any(x => x.IsChecked);
			var items = containsChecked
				? DataItems.Where(x => x.IsChecked).ToList()
				: MainDataGrid.SelectedItems.Cast<DataItem>().ToList();
			return items;
		}

		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			var items = MainDataGrid.SelectedItems.Cast<DataItem>().ToArray();
			foreach (var item in items)
				DataItems.Remove(item);
			Reorder();
		}

		public void Reorder()
		{
			// Reorder script items.
			var sorted = DataItems.OrderBy(x => x.Order).ToArray();
			for (int i = 0; i < sorted.Length; i++)
			{
				var item = sorted[i];
				item.Order = i;
			}
		}

	}
}