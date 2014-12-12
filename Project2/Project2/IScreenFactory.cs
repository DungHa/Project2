using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Project2
{
    public interface IScreenFactory
    {
        GameScreen CreateScreen(Type screenType);
    }
}
