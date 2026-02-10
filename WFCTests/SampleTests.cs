using System;
using System.Diagnostics;
using System.Xml.Linq;
using WFC;

namespace WFCTests;

public class SampleTests
{
  [ Fact ]
  public void Run( )
  {
    Stopwatch sw = Stopwatch.StartNew( );
    var folder = System.IO.Directory.CreateDirectory( "output" );
    foreach ( var file in folder.GetFiles( ) ) file.Delete( );

    Random random = new( );
    XDocument xDocument = XDocument.Load( "params.xml" );

    foreach ( XElement xElement in xDocument.Root.Elements( "overlapping" ) )
    {
      Model model;
      string name = xElement.Get<string>( "name" );
      Console.WriteLine( $"< {name}" );

      int size = xElement.Get( "size", 48 );
      int oWidth = xElement.Get( "width", size );
      int oHeight = xElement.Get( "height", size );
      bool periodic = xElement.Get( "periodic", false );
      string heuristicString = xElement.Get<string>( "heuristic" );
      var heuristic = heuristicString == "Scanline" ? Model.Heuristic.Scanline : ( heuristicString == "MRV" ? Model.Heuristic.MRV : Model.Heuristic.Entropy );

      int n = xElement.Get( "N", 3 );
      bool periodicInput = xElement.Get( "periodicInput", true );
      int symmetry = xElement.Get( "symmetry", 8 );
      bool ground = xElement.Get( "ground", false );

      ( var data, int iWidth, int iHeight ) = BitmapHelper.LoadBitmap( $"samples/{name}.png" );
      
      model = new OverlappingModel( 
        data, iWidth, iHeight, 
        n, oWidth, oHeight, 
        periodicInput, periodic, 
        symmetry, ground, 
        heuristic );

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
            (var map, int w, int h) = model.GetBitmap();
            BitmapHelper.SaveBitmap( map, w, h, $"samples/{name}" );
          }
          Console.WriteLine( "CONTRADICTION" );
        }
      }
    }

    Console.WriteLine( $"time = {sw.ElapsedMilliseconds}" );
  }
}