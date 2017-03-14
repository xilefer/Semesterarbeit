using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace HomeMediaApp
{
    public partial class MainPage : ContentPage
    {

        public ListView DeviceListView {
            get { return ListView1; }
        }

        private ObservableCollection<string> mItems = new ObservableCollection<string>();
        public ObservableCollection<string> Items {
            get { return mItems; }
            set
            {
                if (mItems == value) return;
                mItems = value;
                OnPropertyChanged("Items");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected override void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            Init();
        }


        private void SettingsButton_OnClicked(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Init()
        {
            TopGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30, GridUnitType.Star) });
            TopGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(40, GridUnitType.Star) });
            TopGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30, GridUnitType.Star) });
            Button oButton = new Button() {Text = "Zurück"};
            oButton.SetValue(Grid.ColumnProperty, 0);
            oButton.Clicked += BackButtonOnClicked;
            TopGrid.Children.Add(oButton);
            Label oLabel = new Label() {HorizontalOptions = LayoutOptions.Center, Text = "Willkommen", FontSize = 20 };
            oLabel.SetValue(Grid.ColumnProperty, 1);
            TopGrid.Children.Add(oLabel);
            Button oSettingsButton = new Button() { Text = "Einstellungen"};
            oSettingsButton.SetValue(Grid.ColumnProperty, 2);
            oSettingsButton.Clicked += new EventHandler(SettingsButton_OnClicked);
            TopGrid.Children.Add(oSettingsButton);
            ObservableCollection<string> oTempList = new ObservableCollection<string>();
            for (int i = 0; i < 100; i++)
            {
                oTempList.Add("Item " + i);
            }
            Items = oTempList;
            ListView1.ItemsSource = Items;
            OuterGrid.ForceLayout();
        }

        private void BackButtonOnClicked(object sender, EventArgs eventArgs)
        {
            DisplayAlert("Home Media App", "Zurück-Knop ausgelöst", "OK", "Nicht OK");
        }
    }

    public class OwnListViewItem
    {
        public string Name { get; set; }
    }
}
