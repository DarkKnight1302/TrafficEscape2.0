namespace TrafficEscape2.Models;

public class DistanceApiResponse
{
    public string[] destination_addresses
    {
        get; set;
    }

    public string[] origin_addresses
    {
        get; set;
    }

    public ElementsObj[] rows
    {
        get; set; 
    }

    public string status
    {
        get; set;
    }

}

public class ElementsObj
{
    public ElementResp[] elements
    {
        get; set;
    }
}

public class ElementResp
{
    public DataValue distance
    {
        get; set;
    }

    public DataValue duration
    {
        get; set; 
    }

    public DataValue duration_in_traffic
    {
        get; set;
    }
}

public class DataValue
{
    public string text
    {
        get; set;
    }

    public int value
    {
        get; set; 
    }
}
