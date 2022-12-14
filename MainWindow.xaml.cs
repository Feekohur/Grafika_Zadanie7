using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Linq;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;

namespace Grafika_Zadanie7;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            Bitmap bitmap = new Bitmap(openFileDialog.FileName);
            BitmapI.Source = ConvertBitmapToImage(bitmap);
        }
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        BitmapImage bitmapImage = (BitmapImage)BitmapI.Source;
        Bitmap bitmap = new Bitmap(bitmapImage.StreamSource);
        HashSet<(int x, int y)> maxRegion = GetMaxGreenRegion();
        //Bitmap newBitmap = new Bitmap(bitmap.Width, bitmap.Height);
        int greenPixelsCount = 0;
        for (int x = 0; x < bitmap.Width; x++)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                Color color = bitmap.GetPixel(x, y);
                if (maxRegion.Contains((x, y)))
                {
                    bitmap.SetPixel(x, y, Color.Green);
                    greenPixelsCount++;
                }
                else if (IsGreen(color))
                    greenPixelsCount++;
                else
                    bitmap.SetPixel(x, y, Color.Black);
            }
        }
        BitmapI.Source = ConvertBitmapToImage(bitmap);
        int pixelCount = bitmap.Width * bitmap.Height;
        float percentPixels = (float)greenPixelsCount / pixelCount;
        Percent.Text = $"{percentPixels * 100f}%";
        float maxRegion_ofAll = (float)maxRegion.Count / pixelCount;
        float maxRegion_ofGreen = (float)maxRegion.Count / greenPixelsCount;
        Group.Text = $"{maxRegion.Count}px (all: {maxRegion_ofAll*100}%, green: {maxRegion_ofGreen*100}%)" ;
    }

    public bool IsGreen(Color color)
    {
        // Threshold based on HSV.
        float r = color.R / 255f;    
        float g = color.G / 255f;    
        float b = color.B / 255f;
        float c_min = MathF.Min(MathF.Min(r, g), b);
        float c_max = MathF.Max(MathF.Max(r, g), b);
        float delta = c_max - c_min;

        // Value check.
        const float valLowerBound = .2f;
        if (c_max is < valLowerBound)
            return false;

        // Saturation check.
        const float satLowerBound = .2f;
        float sat = c_max is 0 ? 0 : delta / c_max;
        if (sat is < satLowerBound)
            return false;

        // Hue check.
        float hue = delta is 0 ? 0
            : c_max == r ? ((g - b) / delta) % 6
            : c_max == g ? ((b - g) / delta) + 2
            : c_max == b ? ((r - g) / delta) + 4
            : throw new InvalidOperationException();

        const float hueLowerBound = 60 / 60f;
        const float hueUpperBound = 145 / 60f;
        if (hue is < hueLowerBound or > hueUpperBound)
            return false;

        return true;
    }

    /// <summary> Make sure to filter out all non green pixels at this point. </summary>
    public HashSet<(int, int)> GetMaxGreenRegion()
    {
        BitmapImage bitmapImage = (BitmapImage)BitmapI.Source;
        Bitmap bitmap = new(bitmapImage.StreamSource);

        HashSet<(int, int)> maxRegion = new();
        HashSet<(int, int)> visited = new();
        var inBounds = (int x, int y) => (x is >=0 && x < bitmap.Width) && (y is >=0 && y < bitmap.Height);
        for (int x =0; x < bitmap.Width; x++)
            for (int y = 0; y <bitmap.Height; y++)
            {
                if (visited.Contains((x, y)) || IsGreen(bitmap.GetPixel(x, y)) is false)
                    continue;
                
                Stack<(int x, int y)> currRegionToVisit = new();
                currRegionToVisit.Push((x, y));
                HashSet<(int x, int y)> currRegion = new() { (x, y) };
                while(currRegionToVisit.Count > 0)
                {
                    (int x, int y) curr = currRegionToVisit.Pop();
                    if (visited.Contains((curr.x, curr.y)) || IsGreen(bitmap.GetPixel(curr.x, curr.y)) is false)
                        continue;
                    visited.Add((curr.x, curr.y));
                    currRegion.Add((curr.x, curr.y));

                    //foreach ((int x, int y) pix in { (curr.x - 1, curr.y), (curr.x - 1, curr.y), (curr.x - 1, curr.y), (curr.x - 1, curr.y) })
                    //    if (inBounds(pix.x, pix.y) || visited.Contains((pix.x, pix.y)))
                    //        currRegionToVisit.Push((pix.x, pix.y));

                    int xx, yy;
                    (xx, yy) = (curr.x - 1, curr.y);
                    if(inBounds(xx, yy))
                        currRegionToVisit.Push((xx, yy));
                    (xx, yy) = (curr.x, curr.y + 1);
                    if (inBounds(xx, yy))
                        currRegionToVisit.Push((xx, yy));
                    (xx, yy) = (curr.x + 1, curr.y);
                    if (inBounds(xx, yy))
                        currRegionToVisit.Push((xx, yy));
                    (xx, yy) = (curr.x, curr.y - 1);
                    if (inBounds(xx, yy))
                        currRegionToVisit.Push((xx, yy));
                }

                if(maxRegion.Count < currRegion.Count)
                    maxRegion = currRegion;
            }

        return maxRegion;
    }

    public BitmapImage ConvertBitmapToImage(Bitmap src)
    {
        MemoryStream ms = new MemoryStream();
        src.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        ms.Seek(0, SeekOrigin.Begin);
        image.StreamSource = ms;
        image.EndInit();
        return image;
    }
}
