using Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Word_Sever.Interface
{
    public interface INet
    {
        void Update();
        void Close();
    }
}
