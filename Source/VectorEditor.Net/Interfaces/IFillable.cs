using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace VeNET.Interfaces
{
    public interface IFillable
    {
        Brush Fill { get; set; }
    }
}
