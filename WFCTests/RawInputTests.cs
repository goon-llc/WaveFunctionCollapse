using System;
using WFC;

namespace WFCTests;

public class RawInputTests
{
  [ Fact ]
  public void MineLayoutTest( )
  {
    var folder = System.IO.Directory.CreateDirectory( "output" );
    foreach ( var file in folder.GetFiles( ) )
    {
      file.Delete( );
    }
    

    var layoutName = "mine02";

    Model model = new OverlappingModel(
      name: layoutName,
      n: 3,
      outWidth: 48,
      outHeight: 48,
      periodicInput: false,
      periodicOutput: true,
      symmetry: 8,
      ground: false,
      Model.Heuristic.Entropy
    );

    int seed = 42;
    var success = model.Run( seed, -1 );

    if ( success )
    {
      model.Save( $"output/{layoutName}_{seed}.png" );
    }
    else
    {
      Console.WriteLine( $"Failed to generate coherent layout with {layoutName}.png." );
    }
  }
}