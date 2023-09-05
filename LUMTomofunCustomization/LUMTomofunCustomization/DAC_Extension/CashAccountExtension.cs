using PX.Data;
using PX.Objects.CA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PX.Objects.CA.CashAccountExtension;

namespace PX.Objects.CA
{
    public class CashAccountExtension : PXCacheExtension<CashAccount>
    {
        #region UsrCheckPaymentUnique
        [PXDBBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Check Payment Unique")]
        public virtual bool? UsrCheckPaymentUnique { get;set;}
        public abstract class usrCheckPaymentUnique : PX.Data.BQL.BqlBool.Field<usrCheckPaymentUnique> { }
        #endregion
    }
}
