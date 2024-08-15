using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RapidBus.Abstractions;

public interface IRapidBusFactory
{
    IRapidBus Create(IServiceProvider serviceProvider);
}
