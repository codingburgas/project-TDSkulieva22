namespace Recipe_Hub.Models;

public class ErrorViewModel
{
    //Unique ID for tracking the error request
    public string? RequestId { get; set; }

    //Indicates whether the RequestId should be displayed
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}