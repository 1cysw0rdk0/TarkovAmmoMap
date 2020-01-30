using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Reflection;
using System.Drawing;

namespace TarkovAmmoMap {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		public AmmoDict ammo;
		public Dictionary<CaliberData, SolidColorBrush> DisplayData;
		public Dictionary<string, int> HitPointData;
		Random r = new Random();

		public MainWindow() {
			InitializeComponent();

			DisplayData = new Dictionary<CaliberData, SolidColorBrush>();
			HitPointData = new Dictionary<string, int>();

			// User Selects file

			OpenFileDialog openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true) {
				ammo = ReadAmmoCSV(openFileDialog.FileName);
			} else {
				// Error Message?
			}


			// TODO //
			HitPointData.Add("Head", 35);
			HitPointData.Add("Thorax", 80);
			HitPointData.Add("Stomach", 70);
			HitPointData.Add("Legs", 65);
			HitPointData.Add("Arms", 60);


			// Add Bottom Check Boxes

			foreach (String str in "Head Thorax Stomach Legs Arms".Split(' ')) {
				StackPanel newStack = new StackPanel();
				newStack.Orientation = Orientation.Horizontal;

				CheckBox checkBox = new CheckBox();
				checkBox.VerticalAlignment = VerticalAlignment.Center;
				checkBox.IsChecked = false;
				checkBox.Tag = str;
				checkBox.Checked += AddVisualLine;
				checkBox.Unchecked += RemoveVisualLine;

				Label label = new Label();
				label.Content = str;
				label.VerticalAlignment = VerticalAlignment.Center;

				newStack.Children.Add(checkBox);
				newStack.Children.Add(label);

				TargetStack.Children.Add(newStack);
			}


			// Add all loaded caliber data sets

			foreach (CaliberData cal in ammo.ammoDict.Values) {

				// Select color for this caliber
				while (true) {
					SolidColorBrush brush = PickBrush();
					if (!DisplayData.ContainsValue(brush)) {
						DisplayData.Add(cal, brush);
						break;
					}
				}

				// Create checkbox and label for selection
				StackPanel newStack = new StackPanel();
				newStack.Orientation = Orientation.Horizontal;

				CheckBox checkBox = new CheckBox();
				checkBox.VerticalAlignment = VerticalAlignment.Center;
				checkBox.IsChecked = true;
				checkBox.Unchecked += Series_UnChecked;
				checkBox.Checked += Series_Checked;
				checkBox.Tag = cal.Name;

				Label label = new Label();
				label.Content = cal.Name;
				label.VerticalAlignment = VerticalAlignment.Center;

				Ellipse ellipse = new Ellipse();
				DisplayData.TryGetValue(cal, out SolidColorBrush temp);
				ellipse.Fill = temp;
				ellipse.Width = 10;
				ellipse.Height = 10;

				newStack.Children.Add(checkBox);
				newStack.Children.Add(label);
				newStack.Children.Add(ellipse);

				SelectStack.Children.Add(newStack);

				AddCaliberSet(cal);
	
			}

		}


		private void RemoveVisualLine(object sender, RoutedEventArgs e) {
			Axis axis = VisualChartDisplay.AxisX.ElementAt(0);
			CheckBox checkBox = (CheckBox)sender;
			HitPointData.TryGetValue(checkBox.Tag.ToString(), out int hp);
			foreach (AxisSection section in axis.Sections) {
				if (section.Value == hp) {
					axis.Sections.Remove(section);
				}
			}
			
		}

		private void AddVisualLine(object sender, RoutedEventArgs e) {
			Axis axis = VisualChartDisplay.AxisX.ElementAt(0);
			CheckBox checkBox = (CheckBox)sender;
			AxisSection section = new AxisSection();
			HitPointData.TryGetValue(checkBox.Tag.ToString(), out int hp);
			section.Value = hp;
			section.Width = 1;
			section.MinWidth = 1;
			section.SectionWidth = .5;
			section.Name = checkBox.Tag.ToString();
			axis.Sections.Add(section);
			VisualChartDisplay.AxisX[0].MinValue -= 1;
			VisualChartDisplay.AxisX[0].MinValue += 1;
		}


		private SolidColorBrush PickBrush() {
			int counter = 0;
			while (counter < 100) {

				byte[] colors = new byte[3];
				r.NextBytes(colors);
				r.Next();
				r.Next();
				SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(colors[0], colors[1], colors[2]));
				bool match = false;
				foreach (SolidColorBrush value in DisplayData.Values) {
					if (Color.AreClose(brush.Color, value.Color)) {
						match = true;
					} 
				}

				if (!match) {
					return brush;
				}
				counter++;
				
			}
			byte[] colors2 = new byte[3];
			r.NextBytes(colors2);
			r.Next();
			r.Next();
			return new SolidColorBrush(Color.FromRgb(colors2[0], colors2[1], colors2[2]));

		}

		private void Series_Checked(object sender, RoutedEventArgs e) {
			ammo.ammoDict.TryGetValue((string)(((CheckBox)sender).Tag), out CaliberData caliber);
			AddCaliberSet(caliber);
		}

		private void Series_UnChecked(object sender, RoutedEventArgs e) {
			ammo.ammoDict.TryGetValue((string)(((CheckBox)sender).Tag), out CaliberData caliber);
			RemoveCaliberSet(caliber);
		}

		public void RemoveCaliberSet(CaliberData caliber) {
			if (DisplayData.ContainsKey(caliber)) {

				// Get the brush color for this series
				DisplayData.TryGetValue(caliber, out SolidColorBrush brush);

				foreach(ScatterSeries point in VisualChartDisplay.Series) {
					if (point.Fill.Equals(brush)) {
						VisualChartDisplay.Series.Remove(point);
					}
				}
			}
		}

		public void AddCaliberSet(CaliberData caliber) {
			if (DisplayData.ContainsKey(caliber)) {

				// Add each shot as a series so I can choose its name
				// Not a graceful solution but whatevs
				foreach (ShotLoadData shot in caliber.ShotLoads) {
				
					ChartValues<ObservablePoint> newPoint = new ChartValues<ObservablePoint>();
					ScatterSeries newSeries = new ScatterSeries();

					newPoint.Add(new ObservablePoint(shot.FleshDamage, shot.PenetrationDamage));
					newSeries.Values = newPoint;
					newSeries.Title = shot.VariantName;
					DisplayData.TryGetValue(caliber, out SolidColorBrush temp);
					newSeries.Fill = temp;
					VisualChartDisplay.Series.Add(newSeries);

				}			

			}
		}

		public AmmoDict ReadAmmoCSV(string filepath) {
			AmmoDict ammoDict = new AmmoDict();

			using (StreamReader reader = new StreamReader(filepath)) {
				while (!reader.EndOfStream) {

					string[] line = reader.ReadLine().Split(',');
					ShotLoadData shot;

					if (int.TryParse(line[2], out int flesh) && 
						int.TryParse(line[4], out int armor) && 
						int.TryParse(line[3], out int pen) && 
						int.TryParse(line[5], out int frag)) {
						shot = new ShotLoadData(line[0], line[1], flesh, armor, pen, frag);
					} else {
						// Error Message?
						continue;
					}

					if (!ammoDict.ContainsCalliber(shot.Caliber)) {
						ammoDict.AddCaliber(new CaliberData(shot.Caliber));
					}

					ammoDict.ammoDict.TryGetValue(shot.Caliber, out CaliberData caliberData);
					caliberData.AddShot(shot);
					
				}

			}

			return ammoDict;
		}


		/* Data Types */

		public class ShotLoadData {
			public String Caliber { get; }
			public String VariantName { get; }
			public int FleshDamage { get; }
			public int ArmorDamage { get; }
			public int PenetrationDamage { get; }
			public int FragChance { get; }
			public int AvgPrice { get; }

			public ObservablePoint fleshVsPen;

			public ShotLoadData(string caliber, string variant, int flesh, int armor, int pen, int frag) {
				this.Caliber = caliber;
				this.VariantName = variant;
				this.FleshDamage = flesh;
				this.ArmorDamage = armor;
				this.PenetrationDamage = pen;
				this.FragChance = frag;

				fleshVsPen = new ObservablePoint(flesh, pen);
			}

		}

		public class CaliberData {
			
			public ObservableCollection<ShotLoadData> ShotLoads { get; }
			public String Name { get; }

			public void AddShot(ShotLoadData shotLoad) {
				if (!this.ShotLoads.Contains(shotLoad)) {
					this.ShotLoads.Add(shotLoad);
				}
			}

			public CaliberData(string name) {
				this.Name = name;
				this.ShotLoads = new ObservableCollection<ShotLoadData>();
			}
		}

		public class AmmoDict {

			public Dictionary<string, CaliberData> ammoDict { get; }

			public AmmoDict() => this.ammoDict = new Dictionary<string, CaliberData>();

			public void AddCaliber(CaliberData caliberData) {
				if (!this.ammoDict.ContainsValue(caliberData)) {
					this.ammoDict.Add(caliberData.Name, caliberData);
				}
			}

			public bool ContainsCalliber(string name) => this.ammoDict.ContainsKey(name);
		}

		
	}
}
