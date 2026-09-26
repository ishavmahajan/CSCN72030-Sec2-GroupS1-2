namespace kitchenOS_Scada.Api.Enums;

public enum PointNames
{
    Temperature,
    Setpoint,
    Humidity,
    Door,
    Valve,
    PowerState,
    Compressor,
    Heating,
    Motor,
    Power,
    Energy,
    Speed,
    FanSpeed,
    Airflow,
    Timer,
    Mode,
    Cycle,
    Filter,
    WaterLevel,
    Detergent,
    Flow,
    Consumption,
    Leak
}

public static class PointNamesExtensions
{
    public static string GetPointName(this PointNames pointName)
    {
        return pointName switch
        {
            PointNames.Temperature => "temperature",
            PointNames.Setpoint => "setpoint",
            PointNames.Humidity => "humidity",
            PointNames.Door => "door",
            PointNames.Valve => "valve",
            PointNames.PowerState => "powerState",
            PointNames.Compressor => "compressor",
            PointNames.Heating => "heating",
            PointNames.Motor => "motor",
            PointNames.Power => "power",
            PointNames.Energy => "energy",
            PointNames.Speed => "speed",
            PointNames.FanSpeed => "fanSpeed",
            PointNames.Airflow => "airflow",
            PointNames.Timer => "timer",
            PointNames.Mode => "mode",
            PointNames.Cycle => "cycle",
            PointNames.Filter => "filter",
            PointNames.WaterLevel => "waterLevel",
            PointNames.Detergent => "detergent",
            PointNames.Flow => "flow",
            PointNames.Consumption => "consumption",
            PointNames.Leak => "leak",
            _ => throw new ArgumentOutOfRangeException(nameof(pointName), pointName, null)
        };
    }

    public static PointNames GetPointNameEnum(string pointName)
    {
        return pointName switch
        {
            "temperature" => PointNames.Temperature,
            "setpoint" => PointNames.Setpoint,
            "humidity" => PointNames.Humidity,
            "door" => PointNames.Door,
            "valve" => PointNames.Valve,
            "powerState" => PointNames.PowerState,
            "compressor" => PointNames.Compressor,
            "heating" => PointNames.Heating,
            "motor" => PointNames.Motor,
            "power" => PointNames.Power,
            "energy" => PointNames.Energy,
            "speed" => PointNames.Speed,
            "fanSpeed" => PointNames.FanSpeed,
            "airflow" => PointNames.Airflow,
            "timer" => PointNames.Timer,
            "mode" => PointNames.Mode,
            "cycle" => PointNames.Cycle,
            "filter" => PointNames.Filter,
            "waterLevel" => PointNames.WaterLevel,
            "detergent" => PointNames.Detergent,
            "flow" => PointNames.Flow,
            "consumption" => PointNames.Consumption,
            "leak" => PointNames.Leak,
            _ => throw new ArgumentOutOfRangeException(nameof(pointName), pointName, null)
        };
    }
}
