using System;

namespace Xesin.AddressablesExtensions
{
    public interface IReleaseEvent
    {
        event Action OnDispatch;
    }
}
