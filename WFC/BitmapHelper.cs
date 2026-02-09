// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;

namespace WFC;

static class BitmapHelper
{
  public static (int[] bits, int width, int height) LoadBitmap( string filename )
  {
    using var image = Image.Load<Bgra32>( filename );
    int width = image.Width;
    int height = image.Height;
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