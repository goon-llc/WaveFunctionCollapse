// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace WaveFunctionCollapse;

class SimpleTiledModel : Model
{
  private readonly List<int[]> _tiles;
  private readonly List<string> _tileNames;
  private readonly int _tilesize;
  private readonly bool _blackBackground;

  public SimpleTiledModel( string name, string subsetName, int width, int height, bool periodic, bool blackBackground, Heuristic heuristic ) : base( width, height, 1, periodic,
    heuristic )
  {
    _blackBackground = blackBackground;
    var xDoc = XDocument.Load( $"tilesets/{name}.xml" );
    var xRoot = xDoc.Root;

    bool unique = xRoot.Get( "unique", false );

    List<string> subset = null;
    if ( subsetName != null )
    {
      XElement xSubset = xRoot.Element( "subsets" )?.Elements( "subset" ).FirstOrDefault( x => x.Get<string>( "name" ) == subsetName );
      if ( xSubset == null ) Console.WriteLine( $"ERROR: subset {subsetName} is not found" );
      else subset = xSubset.Elements( "tile" ).Select( x => x.Get<string>( "name" ) ).ToList( );
    }

    static int[] Tile( Func<int, int, int> f, int size )
    {
      int[] result = new int[ size * size ];
      for ( int y = 0; y < size; y++ )
      for ( int x = 0; x < size; x++ )
        result[ x + y * size ] = f( x, y );
      return result;
    }

    static int[] Rotate( int[] array, int size ) => Tile( ( x, y ) => array[ size - 1 - y + x * size ], size );
    static int[] Reflect( int[] array, int size ) => Tile( ( x, y ) => array[ size - 1 - x + y * size ], size );

    _tiles = new List<int[]>( );
    _tileNames = new List<string>( );
    var weightList = new List<double>( );

    var action = new List<int[]>( );
    var firstOccurrence = new Dictionary<string, int>( );

    foreach ( XElement xTile in xRoot.Element( "tiles" ).Elements( "tile" ) )
    {
      string tileName = xTile.Get<string>( "name" );
      if ( subset != null && !subset.Contains( tileName ) ) continue;

      Func<int, int> a, b; //a is 90 degrees rotation, b is reflection
      int cardinality;

      char sym = xTile.Get( "symmetry", 'X' );
      if ( sym == 'L' )
      {
        cardinality = 4;
        a = i => ( i + 1 ) % 4;
        b = i => i % 2 == 0 ? i + 1 : i - 1;
      }
      else if ( sym == 'T' )
      {
        cardinality = 4;
        a = i => ( i + 1 ) % 4;
        b = i => i % 2 == 0 ? i : 4 - i;
      }
      else if ( sym == 'I' )
      {
        cardinality = 2;
        a = i => 1 - i;
        b = i => i;
      }
      else if ( sym == '\\' )
      {
        cardinality = 2;
        a = i => 1 - i;
        b = i => 1 - i;
      }
      else if ( sym == 'F' )
      {
        cardinality = 8;
        a = i => i < 4 ? ( i + 1 ) % 4 : 4 + ( i - 1 ) % 4;
        b = i => i < 4 ? i + 4 : i - 4;
      }
      else
      {
        cardinality = 1;
        a = i => i;
        b = i => i;
      }

      T = action.Count;
      firstOccurrence.Add( tileName, T );

      int[][] map = new int[ cardinality ][];
      for ( int t = 0; t < cardinality; t++ )
      {
        map[ t ] = new int[ 8 ];

        map[ t ][ 0 ] = t;
        map[ t ][ 1 ] = a( t );
        map[ t ][ 2 ] = a( a( t ) );
        map[ t ][ 3 ] = a( a( a( t ) ) );
        map[ t ][ 4 ] = b( t );
        map[ t ][ 5 ] = b( a( t ) );
        map[ t ][ 6 ] = b( a( a( t ) ) );
        map[ t ][ 7 ] = b( a( a( a( t ) ) ) );

        for ( int s = 0; s < 8; s++ ) map[ t ][ s ] += T;

        action.Add( map[ t ] );
      }

      if ( unique )
      {
        for ( int t = 0; t < cardinality; t++ )
        {
          ( var bitmap, _tilesize, _tilesize ) = BitmapHelper.LoadBitmap( $"tilesets/{name}/{tileName} {t}.png" );
          _tiles.Add( bitmap );
          _tileNames.Add( $"{tileName} {t}" );
        }
      }
      else
      {
        ( var bitmap, _tilesize, _tilesize ) = BitmapHelper.LoadBitmap( $"tilesets/{name}/{tileName}.png" );
        _tiles.Add( bitmap );
        _tileNames.Add( $"{tileName} 0" );

        for ( int t = 1; t < cardinality; t++ )
        {
          if ( t <= 3 ) _tiles.Add( Rotate( _tiles[ T + t - 1 ], _tilesize ) );
          if ( t >= 4 ) _tiles.Add( Reflect( _tiles[ T + t - 4 ], _tilesize ) );
          _tileNames.Add( $"{tileName} {t}" );
        }
      }

      for ( int t = 0; t < cardinality; t++ ) weightList.Add( xTile.Get( "weight", 1.0 ) );
    }

    T = action.Count;
    weights = weightList.ToArray( );

    propagator = new int[ 4 ][][];
    var densePropagator = new bool[ 4 ][][];
    for ( int d = 0; d < 4; d++ )
    {
      densePropagator[ d ] = new bool[ T ][];
      propagator[ d ] = new int[ T ][];
      for ( int t = 0; t < T; t++ ) densePropagator[ d ][ t ] = new bool[ T ];
    }

    foreach ( XElement xNeighbor in xRoot.Element( "neighbors" ).Elements( "neighbor" ) )
    {
      string[] left = xNeighbor.Get<string>( "left" ).Split( [ ' ' ], StringSplitOptions.RemoveEmptyEntries );
      string[] right = xNeighbor.Get<string>( "right" ).Split( [ ' ' ], StringSplitOptions.RemoveEmptyEntries );

      if ( subset != null && ( !subset.Contains( left[ 0 ] ) || !subset.Contains( right[ 0 ] ) ) ) continue;

      int l = action[ firstOccurrence[ left[ 0 ] ] ][ left.Length == 1 ? 0 : int.Parse( left[ 1 ] ) ], d = action[ l ][ 1 ];
      int r = action[ firstOccurrence[ right[ 0 ] ] ][ right.Length == 1 ? 0 : int.Parse( right[ 1 ] ) ], u = action[ r ][ 1 ];

      densePropagator[ 0 ][ r ][ l ] = true;
      densePropagator[ 0 ][ action[ r ][ 6 ] ][ action[ l ][ 6 ] ] = true;
      densePropagator[ 0 ][ action[ l ][ 4 ] ][ action[ r ][ 4 ] ] = true;
      densePropagator[ 0 ][ action[ l ][ 2 ] ][ action[ r ][ 2 ] ] = true;

      densePropagator[ 1 ][ u ][ d ] = true;
      densePropagator[ 1 ][ action[ d ][ 6 ] ][ action[ u ][ 6 ] ] = true;
      densePropagator[ 1 ][ action[ u ][ 4 ] ][ action[ d ][ 4 ] ] = true;
      densePropagator[ 1 ][ action[ d ][ 2 ] ][ action[ u ][ 2 ] ] = true;
    }

    for ( int t2 = 0; t2 < T; t2++ )
    for ( int t1 = 0; t1 < T; t1++ )
    {
      densePropagator[ 2 ][ t2 ][ t1 ] = densePropagator[ 0 ][ t1 ][ t2 ];
      densePropagator[ 3 ][ t2 ][ t1 ] = densePropagator[ 1 ][ t1 ][ t2 ];
    }

    List<int>[][] sparsePropagator = new List<int>[ 4 ][];
    for ( int d = 0; d < 4; d++ )
    {
      sparsePropagator[ d ] = new List<int>[ T ];
      for ( int t = 0; t < T; t++ ) sparsePropagator[ d ][ t ] = new List<int>( );
    }

    for ( int d = 0; d < 4; d++ )
    for ( int t1 = 0; t1 < T; t1++ )
    {
      List<int> sp = sparsePropagator[ d ][ t1 ];
      bool[] tp = densePropagator[ d ][ t1 ];

      for ( int t2 = 0; t2 < T; t2++ )
        if ( tp[ t2 ] )
          sp.Add( t2 );

      int maxSt = sp.Count;
      if ( maxSt == 0 ) Console.WriteLine( $"ERROR: tile {_tileNames[ t1 ]} has no neighbors in direction {d}" );
      propagator[ d ][ t1 ] = new int[ maxSt ];
      for ( int st = 0; st < maxSt; st++ ) propagator[ d ][ t1 ][ st ] = sp[ st ];
    }
  }

  public override void Save( string filename )
  {
    int[] bitmapData = new int[ mx * my * _tilesize * _tilesize ];
    if ( observed[ 0 ] >= 0 )
    {
      for ( int x = 0; x < mx; x++ )
      {
        for ( int y = 0; y < my; y++ )
        {
          int[] tile = _tiles[ observed[ x + y * mx ] ];
          for ( int dy = 0; dy < _tilesize; dy++ )
          {
            for ( int dx = 0; dx < _tilesize; dx++ )
            {
              bitmapData[ x * _tilesize + dx + ( y * _tilesize + dy ) * mx * _tilesize ] = tile[ dx + dy * _tilesize ];
            }
          }
        }
      }
    }
    else
    {
      for ( int i = 0; i < wave.Length; i++ )
      {
        int x = i % mx, y = i / mx;
        if ( _blackBackground && sumsOfOnes[ i ] == T )
          for ( int yt = 0; yt < _tilesize; yt++ )
          {
            for ( int xt = 0; xt < _tilesize; xt++ )
            {
              bitmapData[ x * _tilesize + xt + ( y * _tilesize + yt ) * mx * _tilesize ] = 255 << 24;
            }
          }
        else
        {
          bool[] w = wave[ i ];
          double normalization = 1.0 / sumsOfWeights[ i ];
          for ( int yt = 0; yt < _tilesize; yt++ )
          {
            for ( int xt = 0; xt < _tilesize; xt++ )
            {
              int idi = x * _tilesize + xt + ( y * _tilesize + yt ) * mx * _tilesize;
              double r = 0, g = 0, b = 0;
              for ( int t = 0; t < T; t++ )
                if ( w[ t ] )
                {
                  int argb = _tiles[ t ][ xt + yt * _tilesize ];
                  r += ( ( argb & 0xff0000 ) >> 16 ) * weights[ t ] * normalization;
                  g += ( ( argb & 0xff00 ) >> 8 ) * weights[ t ] * normalization;
                  b += ( argb & 0xff ) * weights[ t ] * normalization;
                }
              bitmapData[ idi ] = unchecked(( int )0xff000000 | ( ( int )r << 16 ) | ( ( int )g << 8 ) | ( int )b);
            }
          }
        }
      }
    }
    BitmapHelper.SaveBitmap( bitmapData, mx * _tilesize, my * _tilesize, filename );
  }

  public string TextOutput( )
  {
    var result = new System.Text.StringBuilder( );
    for ( int y = 0; y < my; y++ )
    {
      for ( int x = 0; x < mx; x++ ) result.Append( $"{_tileNames[ observed[ x + y * mx ] ]}, " );
      result.Append( Environment.NewLine );
    }
    return result.ToString( );
  }
}