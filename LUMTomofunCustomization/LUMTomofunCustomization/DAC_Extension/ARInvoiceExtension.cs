using PX.Data;
using PX.Objects.AR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.IN
{
    public class ARInvoiceExtension : PXCacheExtension<ARInvoice>
    {
        [PXMergeAttributes(Method = MergeMethod.Replace)]
        [PXDBString(60, IsUnicode = true)]
        [PXUIField(DisplayName = "Customer Order Nbr.", Visibility = PXUIVisibility.SelectorVisible, Required = false)]
        public virtual String InvoiceNbr { get; set; }
    }
}
