using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Word_Server.Item
{
    public class Document : NetWorkTool
    {
        public Document(int networkID) : base(networkID, 0.1f,true)
        {
        }
    }
}
