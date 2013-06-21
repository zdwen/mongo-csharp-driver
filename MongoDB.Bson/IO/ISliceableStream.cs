using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Bson.IO
{
    internal interface ISliceableStream
    {
        IByteBuffer GetSlice(int position, int length);
    }
}
