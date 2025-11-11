namespace Tufo.API.Models;

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int AreasImported { get; set; }
    public int ClimbsImported { get; set; }
    public int ClimbsSkipped { get; set; }
    public List<string> Errors { get; set; } = new();
}