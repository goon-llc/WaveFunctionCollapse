// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;

namespace WFC;

abstract class Model
{
  protected bool[][] wave;

  protected int[][][] propagator;
  private int[][][] _compatible;
  protected int[] observed;

  private (int, int)[] _stack;
  private int _stackSize, _observedSoFar;

  readonly protected int mx;
  readonly protected int my;
  readonly protected int n;
  protected int T; // todo sort out better name
  readonly protected bool periodic;
  protected bool ground;

  protected double[] weights;
  private double[] _weightLogWeights, _distribution;

  protected int[] sumsOfOnes;
  private double _sumOfWeights, _sumOfWeightLogWeights, _startingEntropy;
  protected double[] sumsOfWeights;
  private double[] _sumsOfWeightLogWeights;
  private double[] _entropies;

  public enum Heuristic { Entropy, MRV, Scanline };
  private readonly Heuristic _heuristic;

  protected Model( int width, int height, int N, bool periodic, Heuristic heuristic )
  {
    mx = width;
    my = height;
    this.n = N;
    this.periodic = periodic;
    this._heuristic = heuristic;
  }

  void Init( )
  {
    wave = new bool[ mx * my ][];
    _compatible = new int[ wave.Length ][][];
    for ( int i = 0; i < wave.Length; i++ )
    {
      wave[ i ] = new bool[ T ];
      _compatible[ i ] = new int[ T ][];
      for ( int t = 0; t < T; t++ ) _compatible[ i ][ t ] = new int[ 4 ];
    }
    _distribution = new double[ T ];
    observed = new int[ mx * my ];

    _weightLogWeights = new double[ T ];
    _sumOfWeights = 0;
    _sumOfWeightLogWeights = 0;

    for ( int t = 0; t < T; t++ )
    {
      _weightLogWeights[ t ] = weights[ t ] * Math.Log( weights[ t ] );
      _sumOfWeights += weights[ t ];
      _sumOfWeightLogWeights += _weightLogWeights[ t ];
    }

    _startingEntropy = Math.Log( _sumOfWeights ) - _sumOfWeightLogWeights / _sumOfWeights;

    sumsOfOnes = new int[ mx * my ];
    sumsOfWeights = new double[ mx * my ];
    _sumsOfWeightLogWeights = new double[ mx * my ];
    _entropies = new double[ mx * my ];

    _stack = new (int, int)[ wave.Length * T ];
    _stackSize = 0;
  }

  public bool Run( int seed, int limit )
  {
    if ( wave == null ) Init( );

    Clear( );
    Random random = new(seed);

    for ( int l = 0; l < limit || limit < 0; l++ )
    {
      int node = NextUnobservedNode( random );
      if ( node >= 0 )
      {
        Observe( node, random );
        bool success = Propagate( );
        if ( !success ) return false;
      }
      else
      {
        for ( int i = 0; i < wave.Length; i++ )
        for ( int t = 0; t < T; t++ )
          if ( wave[ i ][ t ] )
          {
            observed[ i ] = t;
            break;
          }
        return true;
      }
    }

    return true;
  }

  int NextUnobservedNode( Random random )
  {
    if ( _heuristic == Heuristic.Scanline )
    {
      for ( int i = _observedSoFar; i < wave.Length; i++ )
      {
        if ( !periodic && ( i % mx + n > mx || i / mx + n > my ) ) continue;
        if ( sumsOfOnes[ i ] > 1 )
        {
          _observedSoFar = i + 1;
          return i;
        }
      }
      return -1;
    }

    double min = 1E+4;
    int argmin = -1;
    for ( int i = 0; i < wave.Length; i++ )
    {
      if ( !periodic && ( i % mx + n > mx || i / mx + n > my ) ) continue;
      int remainingValues = sumsOfOnes[ i ];
      double entropy = _heuristic == Heuristic.Entropy ? _entropies[ i ] : remainingValues;
      if ( remainingValues > 1 && entropy <= min )
      {
        double noise = 1E-6 * random.NextDouble( );
        if ( entropy + noise < min )
        {
          min = entropy + noise;
          argmin = i;
        }
      }
    }
    return argmin;
  }

  void Observe( int node, Random random )
  {
    bool[] w = wave[ node ];
    for ( int t = 0; t < T; t++ ) _distribution[ t ] = w[ t ] ? weights[ t ] : 0.0;
    int r = _distribution.Random( random.NextDouble( ) );
    for ( int t = 0; t < T; t++ )
      if ( w[ t ] != ( t == r ) )
        Ban( node, t );
  }

  bool Propagate( )
  {
    while ( _stackSize > 0 )
    {
      ( int i1, int t1 ) = _stack[ _stackSize - 1 ];
      _stackSize--;

      int x1 = i1 % mx;
      int y1 = i1 / mx;

      for ( int d = 0; d < 4; d++ )
      {
        int x2 = x1 + Dx[ d ];
        int y2 = y1 + Dy[ d ];
        if ( !periodic && ( x2 < 0 || y2 < 0 || x2 + n > mx || y2 + n > my ) ) continue;

        if ( x2 < 0 ) x2 += mx;
        else if ( x2 >= mx ) x2 -= mx;
        if ( y2 < 0 ) y2 += my;
        else if ( y2 >= my ) y2 -= my;

        int i2 = x2 + y2 * mx;
        int[] p = propagator[ d ][ t1 ];
        int[][] compat = _compatible[ i2 ];

        for ( int l = 0; l < p.Length; l++ )
        {
          int t2 = p[ l ];
          int[] comp = compat[ t2 ];

          comp[ d ]--;
          if ( comp[ d ] == 0 ) Ban( i2, t2 );
        }
      }
    }

    return sumsOfOnes[ 0 ] > 0;
  }

  void Ban( int i, int t )
  {
    wave[ i ][ t ] = false;

    int[] comp = _compatible[ i ][ t ];
    for ( int d = 0; d < 4; d++ ) comp[ d ] = 0;
    _stack[ _stackSize ] = ( i, t );
    _stackSize++;

    sumsOfOnes[ i ] -= 1;
    sumsOfWeights[ i ] -= weights[ t ];
    _sumsOfWeightLogWeights[ i ] -= _weightLogWeights[ t ];

    double sum = sumsOfWeights[ i ];
    _entropies[ i ] = Math.Log( sum ) - _sumsOfWeightLogWeights[ i ] / sum;
  }

  void Clear( )
  {
    for ( int i = 0; i < wave.Length; i++ )
    {
      for ( int t = 0; t < T; t++ )
      {
        wave[ i ][ t ] = true;
        for ( int d = 0; d < 4; d++ ) _compatible[ i ][ t ][ d ] = propagator[ Opposite[ d ] ][ t ].Length;
      }

      sumsOfOnes[ i ] = weights.Length;
      sumsOfWeights[ i ] = _sumOfWeights;
      _sumsOfWeightLogWeights[ i ] = _sumOfWeightLogWeights;
      _entropies[ i ] = _startingEntropy;
      observed[ i ] = -1;
    }
    _observedSoFar = 0;

    if ( ground )
    {
      for ( int x = 0; x < mx; x++ )
      {
        for ( int t = 0; t < T - 1; t++ ) Ban( x + ( my - 1 ) * mx, t );
        for ( int y = 0; y < my - 1; y++ ) Ban( x + y * mx, T - 1 );
      }
      Propagate( );
    }
  }

  public abstract void Save( string filename );

  protected static int[] Dx = [ -1, 0, 1, 0 ];
  protected static int[] Dy = [ 0, 1, 0, -1 ];
  private readonly static int[] Opposite = [ 2, 3, 0, 1 ];
}