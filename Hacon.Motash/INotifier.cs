using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Hacon.Motash
{
    public interface INotifier
    {
        void Send(List<Failure> failures);
    }
}