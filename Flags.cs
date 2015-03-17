using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreCX
{
    static class Flags
    {
        internal static volatile bool market_closed = new bool();

        internal static StatusCodes CloseMarket()
        {
            if (!market_closed)
            {
                market_closed = true;
                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorMarketAlreadyClosed;
        }

        internal static StatusCodes OpenMarket()
        {
            if (market_closed)
            {
                market_closed = false;
                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorMarketAlreadyOpened;
        }
    }
}
