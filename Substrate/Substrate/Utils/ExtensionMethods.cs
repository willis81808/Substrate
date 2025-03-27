using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Substrate.Utils
{
    internal static class ExtensionMethods
    {
        public static bool IsAir(this Block block) => block.Id == 0;
    }
}
