﻿using System;
using System.Collections.Generic;

using System.Text;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;

namespace VeNET.Objects.Entities
{
    public class LineEntity : Entity
    {
        public LineEntity()
            : base("Line")
        {
            this.setGeometry(new LineGeometry(new Point(0, 0), new Point(1, 1)));
        }
    }
}
