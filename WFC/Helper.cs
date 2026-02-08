// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WaveFunctionCollapse;

static class Helper
{
  public static int Random( this double[] weights, double r )
  {
    double sum = 0;
    foreach ( double t in weights ) sum += t;
    double threshold = r * sum;

    double partialSum = 0;
    for ( int i = 0; i < weights.Length; i++ )
    {
      partialSum += weights[ i ];
      if ( partialSum >= threshold ) return i;
    }
    return 0;
  }

  public static long ToPower( this int a, int n )
  {
    long product = 1;
    for ( int i = 0; i < n; i++ ) product *= a;
    return product;
  }

  public static T Get<T>( this XElement xElement, string attribute, T defaultT = default )
  {
    XAttribute a = xElement.Attribute( attribute );
    return a == null ? defaultT : ( T )TypeDescriptor.GetConverter( typeof(T) ).ConvertFromInvariantString( a.Value );
  }

  public static IEnumerable<XElement> Elements( this XElement xElement, params string[] names ) => xElement.Elements( ).Where( e => names.Any( n => n == e.Name ) );
}

static class BitmapHelper
{
  public static (int[], int, int) LoadBitmap( string filename )
  {
    using var image = Image.Load<Bgra32>( filename );
    int width = image.Width, height = image.Height;
    var result = new int[ width * height ];
    image.CopyPixelDataTo( MemoryMarshal.Cast<int, Bgra32>( result ) );
    return ( result, width, height );
  }

  unsafe public static void SaveBitmap( int[] data, int width, int height, string filename )
  {
    fixed ( int* pData = data )
    {
      using var image = Image.WrapMemory<Bgra32>( pData, width, height );
      image.SaveAsPng( filename );
    }
  }
}