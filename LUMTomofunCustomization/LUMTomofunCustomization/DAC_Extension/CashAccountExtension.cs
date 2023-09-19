using PX.Data;

namespace PX.Objects.CA
{
    public class CashAccountExtension : PXCacheExtension<CashAccount>
    {
        #region UsrCheckPaymentUnique
        [PXDBBool]
        [PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
        [PXUIField(DisplayName = "Check Payment Unique", Visible = true)]
        public virtual bool? UsrCheckPaymentUnique { get;set;}
        public abstract class usrCheckPaymentUnique : PX.Data.BQL.BqlBool.Field<usrCheckPaymentUnique> { }
        #endregion
    }
}
