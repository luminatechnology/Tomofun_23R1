using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.SO
{
    public class SOOrderExtension : PXCacheExtension<SOOrder>
    {
        [PXMergeAttributes(Method = MergeMethod.Replace)]
        [PXDBString(60, IsUnicode = true)]
        [PXUIField(DisplayName = "Customer Order Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
        public virtual String CustomerOrderNbr { get; set; }
    }
}
