

using System.ComponentModel;

namespace API_2.Enums
{
    public enum Status
    {
        [Description("idle")]
        Idle,

        [Description("moving up")]
        MovingUp,

        [Description("moving down")]
        MovingDown,

        [Description("doors are open")]
        DoorsOpen
    }
}
