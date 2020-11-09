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
using Supremes;


namespace TarkovAmmoMap {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {

		public AmmoDict ammo;
		public Dictionary<CaliberData, SolidColorBrush> DisplayData;
		public Dictionary<string, int> HitPointData;
		Random r = new Random();
		public string[] ArmorClasses { get; set; }
		public SeriesCollection SeriesCollection { get; set; }

		public MainWindow() {
			InitializeComponent();

			DisplayData = new Dictionary<CaliberData, SolidColorBrush>();
			HitPointData = new Dictionary<string, int>();


			// User Selects file

			//OpenFileDialog openFileDialog = new OpenFileDialog();
			//if (openFileDialog.ShowDialog() == true) {
			//	ammo = ReadAmmoCSV(openFileDialog.FileName);
			//} else {
			//	// Error Message?
			//}

			ammo = ReadAmmoWeb("https://escapefromtarkov.gamepedia.com/Ballistics");

			if (ammo == null) {

			}

			HitPointData.Add("Head", 35);
			HitPointData.Add("Thorax", 85);
			HitPointData.Add("Stomach", 70);
			HitPointData.Add("Legs", 65);
			HitPointData.Add("Arms", 60);


			// Add Bottom Check Boxes
			foreach (String str in HitPointData.Keys) {
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

			

			// Prevent loading if no data present, i.e. no internet connection
			if (ammo == null) {
				return;
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
		
					newPoint.Add(shot.fleshVsPen);
					newSeries.Values = newPoint;
					newSeries.Title = shot.VariantName;
					DisplayData.TryGetValue(caliber, out SolidColorBrush temp);
					newSeries.Fill = temp;
					VisualChartDisplay.Series.Add(newSeries);

			}			

			}
		}

		//public AmmoDict ReadAmmoCSV(string filepath) {
		//	AmmoDict ammoDict = new AmmoDict();

		//	using (StreamReader reader = new StreamReader(filepath)) {
		//		while (!reader.EndOfStream) {

		//			string[] line = reader.ReadLine().Split(',');
		//			ShotLoadData shot;

		//			if (int.TryParse(line[2], out int flesh) && 
		//				int.TryParse(line[4], out int armor) && 
		//				int.TryParse(line[3], out int pen) && 
		//				int.TryParse(line[5], out int frag)) {
		//				shot = new ShotLoadData(line[0], line[1], flesh, armor, pen, frag);
		//			} else {
		//				// Error Message?
		//				continue;
		//			}

		//			// Adds caliber to ammoDict if it does not exist
		//			if (!ammoDict.ContainsCalliber(shot.Caliber)) {
		//				ammoDict.AddCaliber(new CaliberData(shot.Caliber));
		//			}

		//			// Get the current caliber, then add the new shot to it
		//			ammoDict.ammoDict.TryGetValue(shot.Caliber, out CaliberData caliberData);
		//			caliberData.AddShot(shot);
					
		//		}

		//	}

		//	return ammoDict;
		//}

		public AmmoDict ReadAmmoWeb(string uri) {
			// Declarations
			AmmoDict ammoDict = new AmmoDict();
			Uri ballisticsURI = new Uri(uri);

			// Load webpage into parser
			Supremes.Nodes.Document ammoPage;
			try {
				ammoPage = Dcsoup.Parse(ballisticsURI, 5000);
			} catch(Exception e) {
				return null;
			}

			// Navigate to the table tag
			// TODO kinda jank, fix maybe?
			Supremes.Nodes.Element globalWrapper = ammoPage.Body.Children.ElementAt(2);
			Supremes.Nodes.Element pageWrapper = globalWrapper.Children.ElementAt(2);
			Supremes.Nodes.Element content = pageWrapper.Children.ElementAt(0);
			Supremes.Nodes.Element bodyContent = content.Children.ElementAt(4);
			Supremes.Nodes.Element contentText = bodyContent.Children.ElementAt(3);
			Supremes.Nodes.Element parserOutput = contentText.Children.ElementAt(1);
			Supremes.Nodes.Element table = parserOutput.Children.ElementAt(31).Children.ElementAt(0);

			// Prep to parse relevant data
			//   rowCounter keeps track of number of rows by caliber
			//     reference the rowspan="16" to set counter, then the next ?15-16? rows are of that caliber
			//   rows is an IList containing the rows of the table INCLUDES THE LEFT COLUMN
			int offset = 3;
			int rowCounter = 0;
			Supremes.Nodes.Elements rows = table.Children;

			// Parse all relevant data
			//   if rowCounter is 0, set up new caliber set
			//   parse fields from table
			//   setup objects and add to dict
			//     reference ReadAmmoCSV

			string currentCaliber = "";
			foreach (Supremes.Nodes.Element row in rows) {

				// Skip the table header
				if (offset > 0) {
					offset -= 1;
					continue;
				}

				// First of the caliber set, requires special parsing
				if (rowCounter == 0) {
					rowCounter -= 1;

					// Set rowCounter to the rowspan of the first element, which should be the caliber name
					if (row.Children.ElementAt(0).Attributes.Count > 0) {
						rowCounter = int.Parse(row.Children.ElementAt(0).Attributes.ElementAt(0).Value) - 1;
					} else {
						rowCounter = 0;
					}

					// Navigate to the a tag inside the rowspan and get its text value, which should be the caliber name
					//Supremes.Nodes.Element caliberData = row.Children.ElementAt(0).Children.ElementAt(0);
					Supremes.Nodes.Element caliberData = row.Children.ElementAt(0);
					currentCaliber = caliberData.Text;

					// Add the caliber name as a string to the addmoDict
					ammoDict.AddCaliber(new CaliberData(currentCaliber));

					//System.Console.Out.WriteLine("Found Caliber: " + currentCaliber);
					string variant = row.Children.ElementAt(1).Children.ElementAt(0).OwnText;

					// Parse Flesh Damage
					string fleshS = row.Children.ElementAt(2).OwnText;
					int flesh = -1;
					if (fleshS.Contains('x')) {
						flesh = int.Parse(row.Children.ElementAt(2).Attributes.ElementAt(0).Value);
					} else {
						flesh = int.Parse(fleshS);
					}

					// Parse Armor Damage %
					int armor = int.Parse(row.Children.ElementAt(4).OwnText);

					// Parse Armor Pen
					int pen = int.Parse(row.Children.ElementAt(3).OwnText);

					// Parse Acc Adjustment
					int acc = 0;
					string accS = row.Children.ElementAt(5).Text;
					if (accS != "") {
						if (accS.Contains('+')) {
							acc = int.Parse(accS.Replace('+', ' '));
						} else {
							acc = int.Parse(accS.Replace('-', ' ')) * -1;
						}
					}

					// Parse Rec Adjustment
					int rec = 0;
					string recS = row.Children.ElementAt(6).Text;
					if (recS != "") {
						if (recS.Contains('+')) {
							rec = int.Parse(recS.Replace('+', ' '));
						} else {
							rec = int.Parse(recS.Replace('-', ' ')) * -1;
						}
					}
					
					// Parse frag chance
					int frag = int.Parse(row.Children.ElementAt(7).OwnText.Replace('%', ' ').Trim());

					// Parse pen effectiveness in order
					int c1 = int.Parse(row  .Children.ElementAt(8).OwnText);
					int c2 = int.Parse(row.Children.ElementAt(9).OwnText);
					int c3 = int.Parse(row.Children.ElementAt(10).OwnText);
					int c4 = int.Parse(row.Children.ElementAt(11).OwnText);
					int c5 = int.Parse(row.Children.ElementAt(12).OwnText);
					int c6 = int.Parse(row.Children.ElementAt(13).OwnText);


					ShotLoadData shot = new ShotLoadData(currentCaliber, variant, flesh, armor, pen, frag, acc, rec, c1, c2, c3, c4, c5, c6);

					ammoDict.ammoDict.TryGetValue(shot.Caliber, out CaliberData caliber);
					caliber.AddShot(shot);


				} else {
					rowCounter -= 1;

					// Handles all other entries
					string variant = row.Children.ElementAt(0).Children.ElementAt(0).OwnText;

					// Parse Flesh Damage
					string fleshS = row.Children.ElementAt(1).OwnText;
					int flesh = -1;
					if (fleshS.Contains('x')) {
						flesh = int.Parse(row.Children.ElementAt(1).Attributes.ElementAt(0).Value);
					} else {
						flesh = int.Parse(fleshS);
					}

					// Parse Armor Damage %
					int armor = int.Parse(row.Children.ElementAt(3).OwnText);

					// Parse Armor Pen
					int pen = int.Parse(row.Children.ElementAt(2).OwnText);

					// Parse frag chance
					int frag = int.Parse(row.Children.ElementAt(6).OwnText.Replace('%', ' ').Trim());

					// Parse Acc Adjustment
					int acc = 0;
					string accS = row.Children.ElementAt(4).Text;
					if (accS != "") {
						if (accS.Contains('+')) {
							acc = int.Parse(accS.Replace('+', ' '));
						} else {
							acc = int.Parse(accS.Replace('-', ' ')) * -1;
						}
					}

					// Parse Rec Adjustment
					int rec = 0;
					string recS = row.Children.ElementAt(5).Text;
					if (recS != "") {
						if (recS.Contains('+')) {
							rec = int.Parse(recS.Replace('+', ' '));
						} else {
							rec = int.Parse(recS.Replace('-', ' ')) * -1;
						}
					}

					// Parse pen effectiveness in order
					int c1 = int.Parse(row.Children.ElementAt(7).OwnText);
					int c2 = int.Parse(row.Children.ElementAt(8).OwnText);
					int c3 = int.Parse(row.Children.ElementAt(9).OwnText);
					int c4 = int.Parse(row.Children.ElementAt(10).OwnText);
					int c5 = int.Parse(row.Children.ElementAt(11).OwnText);
					int c6 = int.Parse(row.Children.ElementAt(12).OwnText);

					ShotLoadData shot = new ShotLoadData(currentCaliber, variant, flesh, armor, pen, frag, acc, rec, c1, c2, c3, c4, c5, c6);

					ammoDict.ammoDict.TryGetValue(shot.Caliber, out CaliberData caliber);
					caliber.AddShot(shot);
				}
			}
			return ammoDict;
		}

		private void VisualChartDisplay_DataClick(object sender, ChartPoint chartPoint) {
			// Get the name of shotload  
			// chartPoint.SeriesView.Title
			Grid.SetColumnSpan(VisualChartDisplay, 1);
			
			
			
			foreach (CaliberData caliberSet in ammo.ammoDict.Values) {
				foreach (ShotLoadData shot in caliberSet.ShotLoads) {
					if (shot.VariantName == chartPoint.SeriesView.Title) {
						//System.Console.Out.WriteLine("Found " + shot.VariantName);
						DetailName.Content = shot.VariantName;
						DetailsFleshDamage.Value = shot.FleshDamage;
						DetailsArmorPen.Value = shot.PenetrationDamage;
						DetailsArmorDam.Value = shot.ArmorDamage;

						// TODO Remove when BSG fixes <20 pen frag bug
						if (shot.PenetrationDamage >= 20) {
							DetailsFragChance.Value = shot.FragChance;
						} else {
							DetailsFragChance.Value = 0;
						}

						PenDisplayTitle.Content = shot.VariantName + " v. Armor";

						PenDisplayChart.AxisY.ElementAt(0).Labels = new[] { "Class 6", "Class 5", "Class 4", "Class 3", "Class 2", "Class 1", };


						PenDisplayChart.Series =  new SeriesCollection {
							new RowSeries {
								Title = shot.VariantName,
								Values = new ChartValues<int> { shot.Class6, shot.Class5, shot.Class4, shot.Class3, shot.Class2, shot.Class1 }
							}
						};
					}
				}
			}

			DetailsGrid.Visibility = Visibility.Visible;
		}





		/* End of Implementation */
		/* Data Types */

		public class ShotLoadData {
			public String Caliber { get; }
			public String VariantName { get; }
			public int FleshDamage { get; }
			public int ArmorDamage { get; }
			public int PenetrationDamage { get; }
			public int FragChance { get; }
			public int Accuracy { get; }
			public int Recoil { get; }

			public int Class1 { get; }
			public int Class2 { get; }
			public int Class3 { get; }
			public int Class4 { get; }
			public int Class5 { get; }
			public int Class6 { get; }
			public int AvgPrice { get; }

			public ObservablePoint fleshVsPen;

			public ShotLoadData(string caliber, string variant, int flesh, int armor, int pen, int frag, int acc, int rec, int c1, int c2, int c3, int c4, int c5, int c6) {
				this.Caliber = caliber;
				this.VariantName = variant;
				this.FleshDamage = flesh;
				this.ArmorDamage = armor;
				this.PenetrationDamage = pen;
				this.FragChance = frag;

				this.Accuracy = acc;
				this.Recoil = rec;
				this.Class1 = c1;
				this.Class2 = c2;
				this.Class3 = c3;
				this.Class4 = c4;
				this.Class5 = c5;
				this.Class6 = c6;

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

		private void Reset_Click(object sender, RoutedEventArgs e) {
			VisualChartDisplay.AxisX[0].MinValue = 0;
			VisualChartDisplay.AxisX[0].MaxValue = 250;
			VisualChartDisplay.AxisY[0].MinValue = 0;
			VisualChartDisplay.AxisY[0].MaxValue = 90;
		}

		private void Toggle_All(object sender, RoutedEventArgs e) {
			foreach (StackPanel caliber in SelectStack.Children) {
				((CheckBox)caliber.Children[0]).IsChecked = !((CheckBox)caliber.Children[0]).IsChecked;
			}
		}

	}
}
