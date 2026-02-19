namespace WFC
{
  public record WFCConfig(
    int[] brush, int inputWidth, int inputHeight, int n, int outputWidth, int outputHeight,
    bool periodicInput, bool periodicOutput, int symmetry, bool ground, Model.Heuristic heuristic );
}