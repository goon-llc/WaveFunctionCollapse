// Copyright (C) 2016 Maxim Gumin, The MIT License (MIT)

using System;
using System.Xml.Linq;
using System.Diagnostics;

namespace WaveFunctionCollapse;

static class InvocationExample
{
  static void Run( )
  {
    Stopwatch sw = Stopwatch.StartNew( );
    var folder = System.IO.Directory.CreateDirectory( "output" );
    foreach ( var file in folder.GetFiles( ) ) file.Delete( );

    Random random = new( );
    XDocument xdoc = XDocument.Load( "samples.xml" );

    foreach ( XElement xElement in xdoc.Root.Elements( "overlapping" ) )
    {
      Model model;
      string name = xElement.Get<string>( "name" );
      Console.WriteLine( $"< {name}" );

      int size = xElement.Get( "size", 48 );
      int width = xElement.Get( "width", size );
      int height = xElement.Get( "height", size );
      bool periodic = xElement.Get( "periodic", false );
      string heuristicString = xElement.Get<string>( "heuristic" );
      var heuristic = heuristicString == "Scanline" ? Model.Heuristic.Scanline : ( heuristicString == "MRV" ? Model.Heuristic.MRV : Model.Heuristic.Entropy );

      int n = xElement.Get( "N", 3 );
      bool periodicInput = xElement.Get( "periodicInput", true );
      int symmetry = xElement.Get( "symmetry", 8 );
      bool ground = xElement.Get( "ground", false );

      model = new OverlappingModel( name, n, width, height, periodicInput, periodic, symmetry, ground, heuristic );

      for ( int i = 0; i < xElement.Get( "screenshots", 2 ); i++ )
      {
        for ( int k = 0; k < 10; k++ )
        {
          Console.Write( "> " );
          int seed = random.Next( );
          bool success = model.Run( seed, xElement.Get( "limit", -1 ) );
          if ( success )
          {
            Console.WriteLine( "DONE" );
            model.Save( $"output/{name} {seed}.png" );
            break;
          }
          Console.WriteLine( "CONTRADICTION" );
        }
      }
    }

    Console.WriteLine( $"time = {sw.ElapsedMilliseconds}" );
  }
}