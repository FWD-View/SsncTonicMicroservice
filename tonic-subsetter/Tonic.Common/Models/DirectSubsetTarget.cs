using System;
using System.Collections.Generic;

namespace Tonic.Common.Models;

public class DirectSubsetTarget
{
    public Table Table { get; init; } = new("", "");
    public string? Clause { get; init;  }
    public decimal? Percent { get; init;  }
    public string? IdColumn { get; init;  }
    public ISet<long>? IdKeys { get; init;  }
    public bool Grouped { get; init;  }

    public void VerifyCorrect()
    {
        if (IdColumn == null ^ IdKeys == null) throw new ArgumentException("IdKey and IdColumn must be present or absent together");
        if (Clause == null && Percent == null && IdColumn == null && IdKeys == null) throw new ArgumentException("No subset specification provided");
    }

    public override string ToString() => $"Direct Target: {Table.HostAndTable()}.{IdColumn}";
}