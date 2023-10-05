using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PX.SM.AUStepField;

namespace LUMTomofunCustomization.DAC
{
    [Serializable]
    [PXCacheName("SOOrderKvExt")]
    public class SOOrderKvExt : IBqlTable
    {
        #region RecordID
        [PXDBGuid(IsKey = true)]
        [PXUIField(DisplayName = "Record ID")]
        public virtual Guid? RecordID { get; set; }
        public abstract class recordID : PX.Data.BQL.BqlGuid.Field<recordID> { }
        #endregion

        #region FieldName
        [PXDBString(50, IsKey = true, InputMask = "")]
        [PXUIField(DisplayName = "Field Name")]
        public virtual string FieldName { get; set; }
        public abstract class fieldName : PX.Data.BQL.BqlString.Field<fieldName> { }
        #endregion

        #region ValueNumeric
        [PXDBDecimal()]
        [PXUIField(DisplayName = "Value Numeric")]
        public virtual Decimal? ValueNumeric { get; set; }
        public abstract class valueNumeric : PX.Data.BQL.BqlDecimal.Field<valueNumeric> { }
        #endregion

        #region ValueDate
        [PXDBDate()]
        [PXUIField(DisplayName = "Value Date")]
        public virtual DateTime? ValueDate { get; set; }
        public abstract class valueDate : PX.Data.BQL.BqlDateTime.Field<valueDate> { }
        #endregion

        #region ValueString
        [PXDBString(256, IsUnicode = true, InputMask = "")]
        [PXUIField(DisplayName = "Value String")]
        public virtual string ValueString { get; set; }
        public abstract class valueString : PX.Data.BQL.BqlString.Field<valueString> { }
        #endregion

        #region ValueText
        [PXDBString(IsUnicode = true, InputMask = "")]
        [PXUIField(DisplayName = "Value Text")]
        public virtual string ValueText { get; set; }
        public abstract class valueText : PX.Data.BQL.BqlString.Field<valueText> { }
        #endregion
    }
}
