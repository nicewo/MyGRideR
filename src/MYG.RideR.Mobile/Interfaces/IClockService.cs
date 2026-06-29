using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MYG.RideR.Mobile.Interfaces;
public interface IClockService
{
    event Action<DateTime>? TimeChanged;

    void Start();

    void Stop();
}
