using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RapidBus;

public enum DeliveryMode : byte
{
    NonPersistent = 1,
    Persistent = 2
}