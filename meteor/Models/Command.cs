using System;

namespace meteor.Models;

public class Command
{
    public string Name { get; set; }
    public Action Action { get; set; }
}