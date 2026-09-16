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
        var flag = new Image { Width = 20, Height = 14, Margin = new Thickness(0, 0, 6, 0) };
        flag.SetBinding(Image.SourceProperty, new Binding("CountryCode") { Converter = new CountryFlagConverter() });
        flag.SetBinding(FrameworkElement.ToolTipProperty, new Binding("CountryCode"));
        panel.Children.Add(flag);
        panel.Children.Add(base.GenerateElement(cell, dataItem));
        return panel;
    }
}
