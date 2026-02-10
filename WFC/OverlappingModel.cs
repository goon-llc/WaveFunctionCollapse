// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace WFC;

public class OverlappingModel : Model
{
  readonly List<byte[]> _patterns;
  readonly List<int> _colors;

  public OverlappingModel( string name, int n, int outWidth, int outHeight, bool periodicInput, bool periodicOutput, int symmetry, bool ground, Heuristic heuristic )
    : base( outWidth, outHeight, n, periodicOutput, heuristic )
  {
    var (bitmap, sx, sy) = BitmapHelper.LoadBitmap( $"samples/{name}.png" );
    byte[] sample = new byte[ bitmap.Length ];
    _colors = new List<int>( );
    for ( int i = 0; i < sample.Length; i++ )
    {
      int color = bitmap[ i ];
      int k = 0;
      for ( ; k < _colors.Count; k++ )
        if ( _colors[ k ] == color )
          break;
      if ( k == _colors.Count ) _colors.Add( color );
      sample[ i ] = ( byte )k;
    }

    static byte[] Pattern( Func<int, int, byte> f, int N )
    {
      byte[] result = new byte[ N * N ];
      for ( int y = 0; y < N; y++ )
      for ( int x = 0; x < N; x++ )
        result[ x + y * N ] = f( x, y );
      return result;
    }

    static byte[] Rotate( byte[] p, int N ) => Pattern( ( x, y ) => p[ N - 1 - y + x * N ], N );
    static byte[] Reflect( byte[] p, int N ) => Pattern( ( x, y ) => p[ N - 1 - x + y * N ], N );

    static long Hash( byte[] p, int C )
    {
      long result = 0, power = 1;
      for ( int i = 0; i < p.Length; i++ )
      {
        result += p[ p.Length - 1 - i ] * power;
        power *= C;
      }
      return result;
    }

    _patterns = new( );
    Dictionary<long, int> patternIndices = new( );
    List<double> weightList = new( );

    int C = _colors.Count;
    int xmax = periodicInput ? sx : sx - n + 1;
    int ymax = periodicInput ? sy : sy - n + 1;
    for ( int y = 0; y < ymax; y++ )
    {
      for ( int x = 0; x < xmax; x++ )
      {
        byte[][] ps = new byte[ 8 ][];

        ps[ 0 ] = Pattern( ( dx, dy ) => sample[ ( x + dx ) % sx + ( y + dy ) % sy * sx ], n );
        ps[ 1 ] = Reflect( ps[ 0 ], n );
        ps[ 2 ] = Rotate( ps[ 0 ], n );
        ps[ 3 ] = Reflect( ps[ 2 ], n );
        ps[ 4 ] = Rotate( ps[ 2 ], n );
        ps[ 5 ] = Reflect( ps[ 4 ], n );
        ps[ 6 ] = Rotate( ps[ 4 ], n );
        ps[ 7 ] = Reflect( ps[ 6 ], n );

        for ( int k = 0; k < symmetry; k++ )
        {
          byte[] p = ps[ k ];
          long h = Hash( p, C );
          if ( patternIndices.TryGetValue( h, out int index ) ) weightList[ index ] = weightList[ index ] + 1;
          else
          {
            patternIndices.Add( h, weightList.Count );
            weightList.Add( 1.0 );
            _patterns.Add( p );
          }
        }
      }
    }

    weights = weightList.ToArray( );
    T = weights.Length;
    this.ground = ground;

    static bool Agrees( byte[] p1, byte[] p2, int dx, int dy, int n )
    {
      int xmin = dx < 0 ? 0 : dx, xmax = dx < 0 ? dx + n : n, ymin = dy < 0 ? 0 : dy, ymax = dy < 0 ? dy + n : n;
      for ( int y = ymin; y < ymax; y++ )
      for ( int x = xmin; x < xmax; x++ )
        if ( p1[ x + n * y ] != p2[ x - dx + n * ( y - dy ) ] )
          return false;
      return true;
    }

    propagator = new int[ 4 ][][];
    for ( int d = 0; d < 4; d++ )
    {
      propagator[ d ] = new int[ T ][];
      for ( int t = 0; t < T; t++ )
      {
        List<int> list = new( );
        for ( int t2 = 0; t2 < T; t2++ )
          if ( Agrees( _patterns[ t ], _patterns[ t2 ], Dx[ d ], Dy[ d ], n ) )
            list.Add( t2 );
        propagator[ d ][ t ] = new int[ list.Count ];
        for ( int c = 0; c < list.Count; c++ ) propagator[ d ][ t ][ c ] = list[ c ];
      }
    }
  }

  [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
  public override (int[] bitmap, int width, int height) GetBitmap( )
  {
    int[] bitmap = new int[ mx * my ];
    if ( observed[ 0 ] >= 0 )
    {
      for ( int y = 0; y < my; y++ )
      {
        int dy = y < my - n + 1 ? 0 : n - 1;
        for ( int x = 0; x < mx; x++ )
        {
          int dx = x < mx - n + 1 ? 0 : n - 1;
          bitmap[ x + y * mx ] = _colors[ _patterns[ observed[ x - dx + ( y - dy ) * mx ] ][ dx + dy * n ] ];
        }
      }
    }
    else
    {
      for ( int i = 0; i < wave.Length; i++ )
      {
        int contributors = 0, r = 0, g = 0, b = 0;
        int x = i % mx, y = i / mx;
        for ( int dy = 0; dy < n; dy++ )
        for ( int dx = 0; dx < n; dx++ )
        {
          int sx = x - dx;
          if ( sx < 0 ) sx += mx;

          int sy = y - dy;
          if ( sy < 0 ) sy += my;

          int s = sx + sy * mx;
          if ( !periodicOutput && ( sx + n > mx || sy + n > my || sx < 0 || sy < 0 ) ) continue;
          for ( int t = 0; t < T; t++ )
            if ( wave[ s ][ t ] )
            {
              contributors++;
              int argb = _colors[ _patterns[ t ][ dx + dy * n ] ];
              r += ( argb & 0xff0000 ) >> 16;
              g += ( argb & 0xff00 ) >> 8;
              b += argb & 0xff;
            }
        }
        bitmap[ i ] = unchecked(( int )0xff000000 | ( ( r / contributors ) << 16 ) | ( ( g / contributors ) << 8 ) | b / contributors);
      }
    }
    return (bitmap, mx, my);
  }
  
  public override void SerializeBitmap( string filename )
  {
    var image = GetBitmap( );
    BitmapHelper.SaveBitmap( image.bitmap, image.width, image.height, filename );
  }
}