using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Shell;
using Windows.ApplicationModel.Store;

namespace PhoneApp1
{
    class PurchaseInterface
    {
        private string pID;
        private Grid baseGrid;
        private Grid purchaseGrid;
        private Button ok;
        private Button cancel;
        private TextBlock details;
        private Image productImage;
        private bool currentlyRunning;
        public PurchaseInterface()
        {
            currentlyRunning = false;
            purchaseGrid = new Grid();
            ok = new Button();
            cancel = new Button();
            purchaseGrid.Children.Add(cancel);
            purchaseGrid.Children.Add(ok);

            RowDefinition r1 = new RowDefinition();
            RowDefinition r2 = new RowDefinition();
            RowDefinition r3 = new RowDefinition();
            ColumnDefinition c1 = new ColumnDefinition();
            ColumnDefinition c2 = new ColumnDefinition();
            r1.MinHeight = 150;
            r2.MinHeight = 200;
            c1.MinWidth = 240;
            c2.MinWidth = 240;
            purchaseGrid.RowDefinitions.Add(r1);
            purchaseGrid.RowDefinitions.Add(r2);
            purchaseGrid.RowDefinitions.Add(r3);
            purchaseGrid.ColumnDefinitions.Add(c1);
            purchaseGrid.ColumnDefinitions.Add(c2);
            SolidColorBrush colorBrush = new SolidColorBrush();
            purchaseGrid.Height = double.NaN;
            purchaseGrid.Width = double.NaN;
            colorBrush.Color = Colors.Black;
            colorBrush.Opacity = .7;
            purchaseGrid.Background = colorBrush;

            details = new TextBlock();
            details.Width = purchaseGrid.Width;
            details.TextWrapping = TextWrapping.Wrap;
            Grid.SetColumn(details, 0);
            Grid.SetColumnSpan(details, 2);
            Grid.SetRow(details, 1);
            purchaseGrid.Children.Add(details);

            productImage = new Image();
            productImage.Height = 150;
            productImage.Width = 150;
            Grid.SetRow(productImage, 0);
            Grid.SetColumn(productImage, 0);
            Grid.SetColumnSpan(productImage, 2);
            productImage.HorizontalAlignment = HorizontalAlignment.Center;
            productImage.VerticalAlignment = VerticalAlignment.Bottom;
            purchaseGrid.Children.Add(productImage);

            ok.Content = "Purchase";
            cancel.Content = "Cancel";
            ok.Width = 240;
            cancel.Width = 240;
            ok.HorizontalAlignment = HorizontalAlignment.Center;
            ok.VerticalAlignment = VerticalAlignment.Top;
            cancel.HorizontalAlignment = HorizontalAlignment.Center;
            cancel.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(ok, 0);
            Grid.SetRow(ok, 2);
            Grid.SetColumn(cancel, 1);
            Grid.SetRow(cancel, 2);
        }
        public async void invokePurchaceInterface(Grid layout,string productID)
        {
            if (!currentlyRunning)
            {
                try
                {
                    ok.Tap += ok_Tap;
                    cancel.Tap += cancel_Tap;
                    currentlyRunning = true;                   
                    pID = productID;
                    baseGrid = layout;
                    purchaseGrid.Width = layout.Width;   
                    var info = await CurrentApp.LoadListingInformationByProductIdsAsync(new string[] { productID });                 
                    details.Text = info.ProductListings[productID].Description+"\n\nPrice: "+info.ProductListings[productID].FormattedPrice;                    
                    productImage.Source = new BitmapImage(info.ProductListings[productID].ImageUri);
                    layout.Children.Add(purchaseGrid);
                }
                catch (Exception excep)
                {
                    MessageBox.Show("Server communication error! Please try again later.");
                    currentlyRunning = false;
                    closeInterface(false);
                }
            }
        }

        private void cancel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            currentlyRunning = false;
            closeInterface(true);           
        }

        private void ok_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            currentlyRunning = false;
            closeInterface(true);
            PurchaseProduct(pID);
        }
        private async void PurchaseProduct(string productID) 
        {
            try
            {
                await CurrentApp.RequestProductPurchaseAsync(productID, false);                
                CurrentApp.ReportProductFulfillment(productID);
                MessageBox.Show("Purchase successful, thank you!!!");
            }
            catch (Exception excep)
            {
                MessageBox.Show("Purchase failed.");
            }
        }
        private void closeInterface(bool gridAdded)
        {
            if (gridAdded)
            {
                baseGrid.Children.Remove(purchaseGrid);
            }
            ok.Tap -= ok_Tap;
            cancel.Tap -= cancel_Tap;
        }
    }
}
