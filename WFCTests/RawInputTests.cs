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


  private bool Retry( Model model, int seed = 42 )
  {
    int tries = 0;
    int maxTries = 15;
    bool success = false;
    var random = new Random( seed );
    while ( !success && tries < maxTries )
    {
      tries++;
      success = model.Run( random.Next() + 1_000_000, -1 );
    }
    return success;
  }

  [ Fact ]
  private void BasicMineLayoutScanline( )
  {
    var folder = MakeTestOutputFolder( nameof(BasicMineLayoutScanline) );
    var layoutName = "mine01";
    var scanline = new OverlappingModel(
      name: layoutName,
      n: 3,
      outWidth: 128,
      outHeight: 128,
      periodicInput: false,
      periodicOutput: false,
      symmetry: 1,
      ground: false,
      Model.Heuristic.Scanline );
    
    var outputPath = Path.Combine( folder.FullName, $"{layoutName}_scanline.png" );
    if ( Retry( scanline ) ) scanline.SerializeBitmap( outputPath );
    else throw new Exception( $"Failed to generate coherent layout with {layoutName}.png" );
  }

  [ Fact ]
  private void BasicMineLayoutEntropy( )
  {
    var folder = MakeTestOutputFolder( nameof(BasicMineLayoutEntropy) );
    var layoutName = "mine01";
    var scanline = new OverlappingModel(
      name: layoutName,
      n: 3,
      outWidth: 128,
      outHeight: 128,
      periodicInput: false,
      periodicOutput: false,
      symmetry: 1,
      ground: false,
      Model.Heuristic.Entropy );
    
    var outputPath = Path.Combine( folder.FullName, $"{layoutName}_entropy.png" );
    if ( Retry( scanline ) ) scanline.SerializeBitmap( outputPath );
    else throw new Exception( $"Failed to generate coherent layout with {layoutName}.png" );
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
      ground: false,
      Model.Heuristic.Scanline
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
        model.SerializeBitmap( Path.Combine( outDir.FullName, $"{layoutName}_{tries}.png" ) );
      } );
    }
    else
    {
      throw new Exception( $"Failed to generate coherent layout with {layoutName}.png after {maxTries} tries." );
    }
  }
}