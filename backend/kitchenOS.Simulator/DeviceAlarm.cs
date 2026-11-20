namespace kitchenOS.Simulator;

// A configured alarm definition that can be raised for a simulated device.
public sealed record DeviceAlarm(string Code, string Severity, string Text);
