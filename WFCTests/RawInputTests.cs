using System;
using System.IO;
using WFC;

namespace WFCTests;

public class RawInputTests
{
  private DirectoryInfo MakeTestOutputFolder( string testName )
  {
    var folder = System.IO.Directory.CreateDirectory( $"output/{testName}" );
    foreach ( var file in folder.GetFiles( ) )
    {
      file.Delete( );
    }

    return folder;
  }

  [ Fact ]
  public void ArgOutOfRangeCase( )
  {
    var outDir = MakeTestOutputFolder( nameof(ArgOutOfRangeCase) );

    var layoutName = "mine02";
    Model model = new OverlappingModel(
      name: layoutName,
      n: 3,
      outWidth: 256,
      outHeight: 256,
      periodicInput: false,
      periodicOutput: false,
      symmetry: 1,
      ground: true,
      Model.Heuristic.MRV
    );

    int tries = 0;
    int maxTries = 10;
    bool success = false;

    while ( !success && tries < maxTries )
    {
      tries++;
      success = model.Run( tries + 8, -1 );
    }

    if ( success )
    {
      Assert.Throws<ArgumentOutOfRangeException>( ( ) =>
      {
        model.Save( Path.Combine( outDir.FullName, $"{layoutName}_{tries}.png" ) );
      } );
    }
    else
    {
      throw new Exception( $"Failed to generate coherent layout with {layoutName}.png after {maxTries} tries." );
    }
  }
}