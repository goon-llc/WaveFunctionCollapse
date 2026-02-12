using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WFC;

internal static class StopWatchExtensions
{
  public static readonly Dictionary<string, long> Tallies = new( );

  public static void Mark( this Stopwatch stopwatch, string region )
  {
    Console.WriteLine( $"{region}:  {stopwatch.ElapsedTicks} ticks" );
    stopwatch.Restart( );
  }

  public static void Accrue( this Stopwatch stopwatch, string region )
  {
    Tallies.TryAdd( region, 0 );
    Tallies[ region ] += stopwatch.ElapsedTicks;
    stopwatch.Restart( );
  }

  public static void GetTally( this Stopwatch stopwatch, string region )
  {
    Console.WriteLine( $"{region}: {Tallies[ region ]} ticks" );
  }
}