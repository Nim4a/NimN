using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using v2rayN.Converters;

namespace v2rayN.Base;

// Retains MyDGTextColumn identity for existing sorting and column persistence.
internal class MyDGCountryColumn : MyDGTextColumn
{
    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var exit = new Image { Width = 20, Height = 14, Margin = new Thickness(0, 0, 6, 0), ToolTip = "Exit (measured)" };
        exit.SetBinding(Image.SourceProperty, new Binding("ExitCountryCode") { Converter = new CountryFlagConverter() });
        panel.Children.Add(exit);
        var endpoint = new Image { Width = 20, Height = 14, Margin = new Thickness(0, 0, 6, 0), ToolTip = "Endpoint / CDN (IP estimate)" };
        endpoint.SetBinding(Image.SourceProperty, new Binding("EndpointCountryCode") { Converter = new CountryFlagConverter() });
        panel.Children.Add(endpoint);
        panel.Children.Add(base.GenerateElement(cell, dataItem));
        return panel;
    }
}
