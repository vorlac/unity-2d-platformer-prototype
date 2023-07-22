using System;

namespace Core
{
    [Flags]
    public enum Direction
    {
        None        = 0x00,
        Up          = 0x01,
        Down        = 0x02,
        Left        = 0x04, 
        Right       = 0x08,
        Horizontal  = Left | Right,
        Vertical    = Up | Down,
        All         = Horizontal | Vertical
    }

    [Flags]
    public enum FlowDirection
    {
        None          = 0x00,
        StartToEnd    = 0x01,
        EndToStart    = 0x02,
        Bidirectional = StartToEnd | EndToStart
    }
}
